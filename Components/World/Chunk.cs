using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using BlockGame.Components.Entities;
using System.Xml.Schema;
using Microsoft.Xna.Framework.Input;

namespace BlockGame.Components.World
{
    internal class Chunk
    {
        //Chunk size
        public static int chunkLength = 16;
        public static int chunkWidth = 16;
        public static int chunkHeight = 50;

        int perlinModifer = 50; //An old variable used to determine how much perlin noise affected the landscape. Derpecated now but I keep it so you can still use the old worldgen if you wanted.

        private Random rnd = new Random();

        //X,Y,Z
        //Main array that stores the block data at each position in the chunk.
        //[0] == BlockID
        //[1] == block light leel
        private ushort[,,][] blockIDs = new ushort[chunkLength, chunkHeight, chunkWidth][];

        private GraphicsDeviceManager graphics; //GraphicsDevice
        private VertexBuffer vertexBuffer; //Main vertexbuffer
        private VertexBuffer lineBuffer; //Line buffer for block outlines
        private VertexBuffer debugBuffer; //Debug buffer
        private WorldManager world; //WorldManager

        public bool chunkLoaded = false; //Is this chunk loaded?

        /// <summary>
        /// Chunks that need to be rebuilt are rebuilt in grouped time based batches to avoid lag
        /// These variables are used to code that in.
        /// </summary>
        private bool rebuildNextFrame = false;
        private int framesSinceLastRebuild = 10000; //Counter for how long since last rebuild

        private DataManager dataManager; //DataManager reference

        private BoundingBox chunkBox; //Hitbox used for Frustum Calcs

        public Vector3 chunkPos; //Chunk position, relative to chunks (not world position)

        public BoundingBox ChunkBox
        {
            get { return chunkBox; }
        }

        public List<Face> visibleFaces; //List of all the chunks visible faces, so that logic does not need to be run on not seen blocks
        public bool drawHitboxes = false; //Should this chunk draw debug hitboxes?

        public Chunk(WorldManager world, Vector3 chunkPos, GraphicsDeviceManager graphics, DataManager dataManager)
        {
            //Set variables
            this.world = world;
            this.chunkPos = chunkPos;
            this.graphics = graphics;
            this.dataManager = dataManager;

            //Creating the chunks hitbox , used in frustum calcs.
            this.chunkBox = new BoundingBox(new Vector3(chunkPos.X * Chunk.chunkLength * Block.blockSize, 0, chunkPos.Z * Chunk.chunkWidth * Block.blockSize), new Vector3(chunkPos.X * Chunk.chunkLength * Block.blockSize + chunkLength * Block.blockSize, chunkHeight * Block.blockSize, chunkPos.Z * Chunk.chunkWidth * Block.blockSize + chunkWidth * Block.blockSize));
            
            SetupBlockIDArray(); //Initize array
        }

        //Initalize array
        public void SetupBlockIDArray()
        {
            for (int x = 0; x < chunkLength; x++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        blockIDs[x, y, z] = new ushort[2]; //Set each index in the 3D array to be an array of size 2
                    }
                }
            }
        }

        //Geneates a chunk completley filled with a block
        public void GenerateFullChunk()
        {
            for (int x = 0; x < chunkLength; x++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {
                    //Starts 1 and ends - 3 from top to avoid rendering bug
                    for (int y = 1; y < chunkHeight - 3; y++)
                    {
                        blockIDs[x, y, z][0] = 3;
                    }
                }
            }
        }

        /// <summary>
        /// Draws everything within the current chunk (faces)
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="basicEffect"></param>
        public void Draw(Camera camera, BasicEffect basicEffect)
        {
            if (vertexBuffer != null && chunkLoaded)
            {
                //Setting up basic effect
                basicEffect.VertexColorEnabled = true; //Turn on colors
                basicEffect.View = camera.View; //Set view matrix
                basicEffect.Projection = camera.Projection; //Set projection matrix
                basicEffect.World = Matrix.Identity; //No world matrix (no transformations needed)
                basicEffect.TextureEnabled = true; //Turn on texturing
                basicEffect.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap; //Pixel perfect rendering

                //Loop through and draw each vertex
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer); //Set vertex buffer to the chunk data

                    //Setting the texture to be the block atlas.
                    basicEffect.Texture = DataManager.blockAtlas;
                    pass.Apply();

                    //Draw triangles
                    graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3); //count is vertex / 3 since each triangle has 3 vertices

                }

                //Debug rendering of face hitboxes
                if (Game1.debug && debugBuffer != null && drawHitboxes)
                {
                    //Loop through and draw each line
                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphics.GraphicsDevice.SetVertexBuffer(debugBuffer);
                        graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, debugBuffer.VertexCount / 2);
                    }
                }
            }
        }

        /// <summary>
        /// Method which builds all the data for the visible faces, such as hitboxes, normals, positions
        /// This is all stored in the VisibleFaces list for reference.
        /// </summary>
        public void BuildVisibleFaces()
        {
            //Generate a list of all the blocks that are empty. Visible blocks are the blocks around "empty" blocks
            List<Vector3> emptyBlocks = new List<Vector3>();

            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        if (blockIDs[x, y, z][0] == 0) //If the block is empty, then add it to the list
                        {
                            emptyBlocks.Add(new Vector3(chunkPos.X * 16 + x, y, chunkPos.Z * 16 + z)); //x,y,z must be x16xchunkPos to convert to world coordinates
                        }
                    }
                }
            }

            visibleFaces = new List<Face>(); //Reset visible face list

            //Add all the faces around the empty blocks, as these are all visible blocks.
            foreach (Vector3 block in emptyBlocks)
            {
                //Z+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z + 1)) != 0) //If this block is not air...
                {
                    //Add a new Face, which stores the hitbox of this face, and the normal value of where the face is facing.
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y, block.Z + 1),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z + 1 - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + 1 - Block.blockSize / 2)),
                        new Vector3(0, 0, -1)));
                }
                //Z-1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z - 1)) != 0) //If this block is not air...
                {
                    //Add a new Face, which stores the hitbox of this face, and the normal value of where the face is facing.
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y, block.Z - 1),
                         new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - 1 + Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z - 1 + Block.blockSize / 2)),
                         new Vector3(0, 0, +1)));

                }
                //x+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X + 1, block.Y, block.Z)) != 0) //If this block is not air...
                {
                    //Add a new Face, which stores the hitbox of this face, and the normal value of where the face is facing.
                    visibleFaces.Add(new Face(new Vector3(block.X + 1, block.Y, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2 + 1, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + 1 - Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(-1, 0, 0)));
                }
                //x-1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X - 1, block.Y, block.Z)) != 0) //If this block is not air...
                {
                    //Add a new Face, which stores the hitbox of this face, and the normal value of where the face is facing.
                    visibleFaces.Add(new Face(new Vector3(block.X - 1, block.Y, block.Z),
                        new BoundingBox(new Vector3(block.X - 1 + Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X - 1 + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(1, 0, 0)));
                }
                //y+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y + 1, block.Z)) != 0) //If this block is not air...
                {
                    //Add a new Face, which stores the hitbox of this face, and the normal value of where the face is facing.
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y + 1, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2 + 1, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y - Block.blockSize / 2 + 1, block.Z + Block.blockSize / 2)),
                        new Vector3(0, -1, 0)));

                }
                //y-1

                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y - 1, block.Z)) != 0) //If this block is not air...
                {
                    //Add a new Face, which stores the hitbox of this face, and the normal value of where the face is facing.
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y - 1, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - 1 + Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y - 1 + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(0, 1, 0)));
                }
            }
        }

        //Builds the vertex buffer for the chunk, which displays all the blocks in the chunk.
        public void BuildVertexBuffer()
        {
            Game1.RebuildCalls += 1;

            List<VertexPositionColorTexture> vertexList = new List<VertexPositionColorTexture>(); //List of verticies which will be loaded into the vertexbuffer
            List<VertexPositionColor> lineList = new List<VertexPositionColor>(); //Same for lines, which is used for debug hitbox viewing

            int defaultLightHue = 0;

            //For each visible face stored, add it to the vertexlist
            foreach (Face face in visibleFaces)
            {
                ///Check which direction this face is "facing"

                //Z+1
                if (face.blockNormal.Equals(new Vector3(0, 0, -1)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition); //Get block ID

                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = face.blockPosition - Vector3.UnitZ;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    Block.AddNegZVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);
                }
                //Z-1
                if (face.blockNormal.Equals(new Vector3(0, 0, 1)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition); //Get block ID

                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = face.blockPosition + Vector3.UnitZ;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    Block.AddPosZVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);


                }
                //x+1
                if (face.blockNormal.Equals(new Vector3(-1, 0, 0)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition); //Get block ID

                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = face.blockPosition - Vector3.UnitX;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    Block.AddNegXVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);


                }
                //x-1
                if (face.blockNormal.Equals(new Vector3(1, 0, 0)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition); //Get block ID

                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = face.blockPosition + Vector3.UnitX;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    Block.AddPosXVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
                //y+1
                if (face.blockNormal.Equals(new Vector3(0, -1, 0)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition); //Get block ID

                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = face.blockPosition - Vector3.UnitY;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    Block.AddNegYVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);


                }
                //y-1

                if (face.blockNormal.Equals(new Vector3(0, 1, 0))) //Get block ID
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition);

                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = face.blockPosition + Vector3.UnitY;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    Block.AddPosYVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
            }

            //If the vertexList has any verticies, build it.
            if (vertexList.Count > 0)
            {
                vertexBuffer = new VertexBuffer(graphics.GraphicsDevice, typeof(VertexPositionColorTexture), vertexList.Count, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertexList.ToArray());
            }

            //Lines
            /*            if (lineList.Count > 0)
                        {
                            lineBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), lineList.Count, BufferUsage.WriteOnly);
                            lineBuffer.SetData(lineList.ToArray());
                        }*/

            //Create the debug vertex buffer
            CreateDebugVBOList();
        }

        //Building vertex buffer for debug rendering
        private void CreateDebugVBOList()
        {
            List<VertexPositionColor> debugList = new List<VertexPositionColor>();

            foreach (Face face in visibleFaces)
            {
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Min.Y, face.hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Min.Y, face.hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Min.Y, face.hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Max.Y, face.hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Min.Y, face.hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Min.Y, face.hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Max.Y, face.hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Max.Y, face.hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Max.Y, face.hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Min.Y, face.hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Max.Y, face.hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Max.Y, face.hitbox.Min.Z), Color.Red));

                //
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Min.Y, face.hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Min.Y, face.hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Min.Y, face.hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Min.Y, face.hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Max.Y, face.hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Max.Y, face.hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Max.Y, face.hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Max.Y, face.hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Min.Y, face.hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Min.X, face.hitbox.Max.Y, face.hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Min.Y, face.hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(face.hitbox.Max.X, face.hitbox.Max.Y, face.hitbox.Min.Z), Color.Red));
            }

            if (debugList.Count > 0)
            {
                debugBuffer = new VertexBuffer(graphics.GraphicsDevice, typeof(VertexPositionColor), debugList.Count, BufferUsage.WriteOnly);
                debugBuffer.SetData(debugList.ToArray());
            }
        }

        //Turns the position within this chunk into a position in the world.
        public Vector3 PosInChunkToPosInWorld(Vector3 posInChunk)
        {
            return new Vector3(posInChunk.X + chunkPos.X * chunkWidth, posInChunk.Y, posInChunk.Z + chunkPos.Z * chunkLength);
        }

        /// <summary>
        /// Gets the current block ID given the block position relative to the current chunk.
        /// </summary>
        /// <param name="posInChunk">The position of the block in relation to the chunk.</param>
        /// <returns>The block ID of the given block.</returns>
        public ushort GetBlock(Vector3 posInChunk)
        {
            return blockIDs[(int)posInChunk.X, (int)posInChunk.Y, (int)posInChunk.Z][0];
        }

        /// <summary>
        /// Gets the light level of a given block.
        /// </summary>
        /// <param name="posInChunk">The position of the block in relation to the chunk.</param>
        /// <returns>The light level of the given block.</returns>
        public ushort GetBlockLightLevel(Vector3 posInChunk)
        {
            return blockIDs[(int)posInChunk.X, (int)posInChunk.Y, (int)posInChunk.Z][1];
        }

        /// <summary>
        /// Sets the light level of a given block.
        /// </summary>
        /// <param name="posInChunk">The position of the block in relation to the chunk.</param>
        /// <param name="newLight">The light level to set the block to.</param>
        public void SetBlockLightLevel(Vector3 posInChunk, ushort newLight)
        {
            blockIDs[(int)posInChunk.X, (int)posInChunk.Y, (int)posInChunk.Z][1] = newLight;
            rebuildNextFrame = true;
        }

        //Rebuild the chunk (used for when the chunk is updated)
        public void BuildChunk()
        {
            BuildVisibleFaces(); //First, rebuild all the visible faces in the chunk
            BuildVertexBuffer(); //Using the data from above, build the vertex buffer
            CreateDebugVBOList(); //Debug
        }

        /// <summary>
        /// Places a block in the chunk at a given position with a given block ID.
        /// </summary>
        /// <param name="posInChunk">The position of the block in relation to the chunk.</param>
        /// <param name="blockID">The block ID to set to.</param>
        public void SetBlock(Vector3 posInChunk, ushort blockID)
        {
            Vector3 posInWorld = PosInChunkToPosInWorld(posInChunk); //Gets this blocks position in the world

            ushort oldBlockID = world.GetBlockAtWorldIndex(posInWorld); // The block ID at the location before being changed.

            // Determines if the old block was a light source, and if so, uses DepopulateLight to remove the light source and PropagateLight to fill in the removed light if necessary.
            if (dataManager.blockData[oldBlockID].IsLightSource()) // If the old block was a light source...
            {
                world.DepopulateLight(posInWorld); // Depopulate light at the position.

                foreach (Vector3 target in world.toPropagate) // For each queued block from DepopulateLight...
                {
                    world.PropagateLight(target);
                }

                world.toPropagate.Clear(); // Clear the propagation queue.
            }

            //If out of bounds, return
            if ((int)posInChunk.X >= chunkLength || (int)posInChunk.Z >= chunkWidth || posInChunk.Y >= chunkHeight || posInChunk.X < 0 || posInChunk.Y < 0 || posInChunk.Z < 0)
            {
                return;
            }

            blockIDs[(int)posInChunk.X, (int)posInChunk.Y, (int)posInChunk.Z][0] = blockID; // Sets the block at posInChunk to blockID.

            rebuildNextFrame = true; //Sets this chunk to be rebuilt next frame, so that its vertexbuffer is rebuilt.

            //Get adjacent chunks in the positive and negative X and Z directions.
            Chunk chunkNegX = world.GetChunk(new Vector2(this.chunkPos.X - 1, this.chunkPos.Z) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));
            Chunk chunkPosX = world.GetChunk(new Vector2(this.chunkPos.X + 1, this.chunkPos.Z) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));
            Chunk chunkNegZ = world.GetChunk(new Vector2(this.chunkPos.X, this.chunkPos.Z - 1) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));
            Chunk chunkPosZ = world.GetChunk(new Vector2(this.chunkPos.X, this.chunkPos.Z + 1) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));

            // If the block is at either X end of the chunk, set the adjacent chunk to rebuild as well.
            if (posInChunk.X == 0 && chunkNegX != null) // If the block is at the negative X end of the chunk and an adjacent chunk exists...
            {
                chunkNegX.rebuildNextFrame = true;
            }
            else if (posInChunk.X == 15 && chunkPosX != null) // If the block is at the positive X end of the chunk and an adjacent chunk exists...
            {
                chunkPosX.rebuildNextFrame = true;
            }

            // If the block is at either Z end of the chunk, set the adjacent chunk to rebuild as well.
            if (posInChunk.Z == 0 && chunkNegZ != null) // If the block is at the negative Z end of the chunk and an adjacent chunk exists...
            {
                chunkNegZ.rebuildNextFrame = true;
            }
            else if (posInChunk.Z == 15 && chunkPosZ != null) // If the block is at the positive Z end of the chunk and an adjacent chunk exists...
            {
                chunkPosZ.rebuildNextFrame = true;
            }

            // If the new set block is a light source, set its light level accordingly and propagate light to the adjacent blocks. If the new set block is air, propagate light to it.
            if (dataManager.blockData[blockID].IsLightSource()) // If the new block is a light source...
            {
                SetBlockLightLevel(posInChunk, dataManager.blockData[blockID].lightEmittingFactor); // Set the block's light level to the light source's LEF.

                Vector3[] targets = world.GetAdjacentBlocks(posInWorld);

                // Propagate light to each adjacent block.
                foreach (Vector3 target in targets)
                {
                    world.PropagateLight(target);
                }
            }
            else if (blockID == 0) // If the new block is air...
            {
                world.PropagateLight(posInWorld);
            }
        }

        /// <summary>
        /// Generates a chunk of air
        /// </summary>
        public void GenerateEmptyChunk()
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        blockIDs[x, y, z][0] = 0;
                    }
                }
            }
        }

        public void Update(Player player)
        {
            int[] playerPos = WorldManager.posInWorlditionToChunkIndex(player.Position); //if player is greater than render distance, then unload the chunk

            //Still gotta offset array index to chunk index ;-;
            if (chunkLoaded && Vector2.Distance(new Vector2(playerPos[0], playerPos[1]) - new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2), new Vector2(this.chunkPos.X, this.chunkPos.Z)) > Player.renderDistance)
            {
                UnloadChunk();
            }

            //Rebuild the chunk if need be
            if (rebuildNextFrame && framesSinceLastRebuild > 5)
            {
                BuildChunk();
                rebuildNextFrame = false;
                framesSinceLastRebuild = 0;
            }

            framesSinceLastRebuild += 1;
        }

        /// <summary>
        /// Method for unloading a chunk. Frees up memory to be used later in the program.
        /// </summary>
        private void UnloadChunk()
        {
            if (vertexBuffer != null && lineBuffer != null)
            {
                vertexBuffer.Dispose(); //Free up VB
                lineBuffer.Dispose(); //Free up VB (line debug)
                vertexBuffer = null; //reset reference
                lineBuffer = null; //reset reference

                chunkLoaded = false; //Tell this chunk it is no longer loaded
            }
        }
    }
}
