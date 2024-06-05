using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using BlockGame.Components.Entity;
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

        //An old variable used to determine how much perlin noise affected the landscape. Derpecated now but I keep it so you can still use the old worldgen if you wanted.
        int perlinModifer = 50;

        private Random rnd = new Random();

        //X,Y,Z
        //Block stores Objects, when the chunk is loaded
        /*        private Block[, ,] blocks = new Block[chunkLength, chunkHeight, chunkWidth];
        */        //BlockIDS stores the ids, when the chunk is unloaded
                  //index 0 [0] == block ID
                  //index 1 [1] == lighting value
        private ushort[,,][] blockIDs = new ushort[chunkLength, chunkHeight, chunkWidth][];

        //Bunch of bufferrs and graphic vars
        private GraphicsDevice graphics;
        private VertexBuffer vertexBuffer;
        private VertexBuffer lineBuffer;
        private VertexBuffer debugBuffer;
        private WorldManager world;

        //Chunk loading 
        public bool chunkLoaded = false;

        /// <summary>
        /// Chunks that need to be rebuilt are rebuilt in grouped time based batches to avoid lag
        /// These variables are used to code that in.
        /// </summary>
        private bool rebuildNextFrame = false;
        private int framesSinceLastRebuild = 10000;

        private DataManager dataManager;

        //Hitbox used for Frustum Calcs
        private BoundingBox chunkBox;

        //Position, remember this is relative to CHUNKS not WORLD!
        public Vector3 chunkPos;

        public BoundingBox ChunkBox
        {
            get { return chunkBox; }
        }

        //Each chunk stores its block's colliders (hitboxes) and normals. This is only iterared for visible blocks.
        public List<Face> visibleFaces;
        public bool drawHitboxes = false;

        public Chunk(WorldManager world, Vector3 chunkPos, GraphicsDevice graphics, DataManager dataManager)
        {
            this.world = world;
            this.chunkPos = chunkPos;
            this.graphics = graphics;
            this.dataManager = dataManager;

            //Creating the chunks hitbox , used in frustum calcs.
            this.chunkBox = new BoundingBox(new Vector3(chunkPos.X * Chunk.chunkLength * Block.blockSize, 0, chunkPos.Z * Chunk.chunkWidth * Block.blockSize), new Vector3(chunkPos.X * Chunk.chunkLength * Block.blockSize + chunkLength * Block.blockSize, chunkHeight * Block.blockSize, chunkPos.Z * Chunk.chunkWidth * Block.blockSize + chunkWidth * Block.blockSize));
            Game1.ChunkCount++;
            SetupBlockIDArray();
        }

        //Setup array to be all air.
        public void SetupBlockIDArray()
        {
            for (int x = 0; x < chunkLength; x++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        blockIDs[x, y, z] = new ushort[2];
                    }
                }
            }
        }

        //Generates a completley full chunk of stone
        public void GenerateFullChunk()
        {
            for (int x = 0; x < chunkLength; x++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {

                    //build column up to the perlin noise
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
                basicEffect.VertexColorEnabled = true;
                basicEffect.View = camera.View;
                basicEffect.Projection = camera.Projection;
                basicEffect.World = Matrix.Identity;
                basicEffect.TextureEnabled = true;
                basicEffect.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;

                //Loop through and draw each vertex
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    graphics.SetVertexBuffer(vertexBuffer);

                    //Setting the texture to be the block atlas.
                    basicEffect.Texture = DataManager.blockAtlas; /*dataManager.blockData[key].texture;*/
                    pass.Apply();

                    graphics.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3); //count is vertex / 3 since each triangle has 3 vertices

                }

                //Debug rendering of face hitboxes
                if (Game1.debug && debugBuffer != null && drawHitboxes)
                {
                    //Loop through and draw each line
                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphics.SetVertexBuffer(debugBuffer);
                        graphics.DrawPrimitives(PrimitiveType.LineList, 0, debugBuffer.VertexCount / 2);
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
            //Generate a List of all the blocks that are empty
            List<Vector3> emptyBlocks = new List<Vector3>();

            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        if (blockIDs[x, y, z][0] == 0)
                        {
                            emptyBlocks.Add(new Vector3(chunkPos.X * 16 + x, y, chunkPos.Z * 16 + z)); //Converting to world coords
                        }
                    }
                }
            }

            visibleFaces = new List<Face>();

            //Add all the faces around the empty blocks, as these are all visible blocks.
            foreach (Vector3 block in emptyBlocks)
            {
                //Z+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z + 1)) != 0)
                {
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y, block.Z + 1),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z + 1 - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + 1 + Block.blockSize / 2)),
                        new Vector3(0, 0, -1)));
                }
                //Z-1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z - 1)) != 0)
                {
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y, block.Z - 1),
                         new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - 1 - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z - 1 + Block.blockSize / 2)),
                         new Vector3(0, 0, +1)));

                }
                //x+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X + 1, block.Y, block.Z)) != 0)
                {
                    visibleFaces.Add(new Face(new Vector3(block.X + 1, block.Y, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2 + 1, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + 1 + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(-1, 0, 0)));
                }
                //x-1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X - 1, block.Y, block.Z)) != 0)
                {
                    visibleFaces.Add(new Face(new Vector3(block.X - 1, block.Y, block.Z),
                        new BoundingBox(new Vector3(block.X - 1 - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X - 1 + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(1, 0, 0)));
                }
                //y+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y + 1, block.Z)) != 0)
                {
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y + 1, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2 + 1, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2 + 1, block.Z + Block.blockSize / 2)),
                        new Vector3(0, -1, 0)));

                }
                //y-1

                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y - 1, block.Z)) != 0)
                {
                    visibleFaces.Add(new Face(new Vector3(block.X, block.Y - 1, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - 1 - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y - 1 + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(0, 1, 0)));
                }
            }
        }

        //Builds the vertex buffer for the chunk using all visible blocks.
        public void BuildVertexBuffer()
        {
            Game1.RebuildCalls += 1;

            //List of verticies before vertex buffer is built.
            List<VertexPositionColorTexture> vertexList = new List<VertexPositionColorTexture>();
            List<VertexPositionColor> lineList = new List<VertexPositionColor>();

            //Check sides of the empty block, render those faces if there is a block
            foreach (Face face in visibleFaces)
            {
                //Z+1
                if (face.blockNormal.Equals(new Vector3(0, 0, -1)))
                {
                    //Gen color
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition);

                    Vector3 adjacentBlock = face.blockPosition - Vector3.UnitZ;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 20;
                    Color color = new Color(colorValue, colorValue, colorValue);
                    Block.AddNegZVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);
                }
                //Z-1
                if (face.blockNormal.Equals(new Vector3(0, 0, 1)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition);

                    Vector3 adjacentBlock = face.blockPosition + Vector3.UnitZ;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 20;
                    Color color = new Color(colorValue, colorValue, colorValue);
                    Block.AddPosZVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);


                }
                //x+1
                if (face.blockNormal.Equals(new Vector3(-1, 0, 0)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition);

                    Vector3 adjacentBlock = face.blockPosition - Vector3.UnitX;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 20;
                    Color color = new Color(colorValue, colorValue, colorValue);
                    Block.AddNegXVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos); Game1.BlockCount++;


                }
                //x-1
                if (face.blockNormal.Equals(new Vector3(1, 0, 0)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition);

                    Vector3 adjacentBlock = face.blockPosition + Vector3.UnitX;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 20;
                    Color color = new Color(colorValue, colorValue, colorValue);
                    Block.AddPosXVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
                //y+1
                if (face.blockNormal.Equals(new Vector3(0, -1, 0)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition);

                    Vector3 adjacentBlock = face.blockPosition - Vector3.UnitY;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 20;
                    Color color = new Color(colorValue, colorValue, colorValue);
                    Block.AddNegYVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);


                }
                //y-1

                if (face.blockNormal.Equals(new Vector3(0, 1, 0)))
                {
                    ushort blockID = world.GetBlockAtWorldIndex(face.blockPosition);

                    Vector3 adjacentBlock = face.blockPosition + Vector3.UnitY;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 20;
                    Color color = new Color(colorValue, colorValue, colorValue);
                    Block.AddPosYVerticiesPos(face.blockPosition * Block.blockSize, vertexList, lineList, color, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
            }

            //Create main vertex buffer
            if (vertexList.Count > 0)
            {
                vertexBuffer = new VertexBuffer(graphics, typeof(VertexPositionColorTexture), vertexList.Count, BufferUsage.WriteOnly);
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
                debugBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), debugList.Count, BufferUsage.WriteOnly);
                debugBuffer.SetData(debugList.ToArray());
            }
        }

        /// <summary>
        /// Gets the current block ID given the block position relative to the current chunk.
        /// </summary>
        /// <param name="posRelativeToChunk"></param>
        /// <returns></returns>
        public ushort GetBlock(Vector3 posRelativeToChunk)
        {
            return blockIDs[(int)posRelativeToChunk.X, (int)posRelativeToChunk.Y, (int)posRelativeToChunk.Z][0];
        }

        public ushort GetBlockLightLevel(Vector3 posRelativeToChunk)
        {
            return blockIDs[(int)posRelativeToChunk.X, (int)posRelativeToChunk.Y, (int)posRelativeToChunk.Z][1];
        }
        public void SetBlockLightLevel(Vector3 posRelativeToChunk, ushort newLight)
        {
            blockIDs[(int)posRelativeToChunk.X, (int)posRelativeToChunk.Y, (int)posRelativeToChunk.Z][1] = newLight;
            rebuildNextFrame = true;
        }

        //Rebuild the chunk (used for when the chunk is updated)
        public void BuildChunk()
        {
            BuildVisibleFaces();
            BuildVertexBuffer();
            CreateDebugVBOList();
        }

        public void SetBlock(Vector3 posRelativeToChunk, ushort block)
        {
            if ((int)posRelativeToChunk.X >= chunkLength || (int)posRelativeToChunk.Z >= chunkWidth || posRelativeToChunk.Y >= chunkHeight || posRelativeToChunk.X < 0 || posRelativeToChunk.Y < 0 || posRelativeToChunk.Z < 0)
            {
                return;
            }

            blockIDs[(int)posRelativeToChunk.X, (int)posRelativeToChunk.Y, (int)posRelativeToChunk.Z][0] = block;

            //When placing a block, the chunk will reload within the next few frames. It also reloads the chunk NEXT to it, should the block be broken on the edge of the chunk.
            rebuildNextFrame = true;

            if (posRelativeToChunk.X == 0)
            {
                Chunk chunk = world.GetChunk(new Vector2(this.chunkPos.X - 1, this.chunkPos.Z) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));
                if (chunk != null)
                {
                    chunk.rebuildNextFrame = true;
                }
            }
            else if (posRelativeToChunk.X == 15)
            {
                Chunk chunk = world.GetChunk(new Vector2(this.chunkPos.X + 1, this.chunkPos.Z) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));
                if (chunk != null)
                {
                    chunk.rebuildNextFrame = true;
                }
            }
            if (posRelativeToChunk.Z == 0)
            {
                Chunk chunk = world.GetChunk(new Vector2(this.chunkPos.X, this.chunkPos.Z - 1) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));
                if (chunk != null)
                {
                    chunk.rebuildNextFrame = true;
                }
            }
            else if (posRelativeToChunk.Z == 15)
            {
                Chunk chunk = world.GetChunk(new Vector2(this.chunkPos.X, this.chunkPos.Z + 1) + new Vector2(WorldManager.chunksGenerated / 2, WorldManager.chunksGenerated / 2));
                if (chunk != null)
                {
                    chunk.rebuildNextFrame = true;
                }
            }
            
            SetBlockLightLevel(posRelativeToChunk, dataManager.blockData[block].lightEmittingFactor);
        }

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
            //if player is greater than render distance, then unload the chunk
            int[] playerPos = WorldManager.WorldPositionToChunkIndex(player.Position);
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

        private void UnloadChunk()
        {
            if (vertexBuffer != null && lineBuffer != null)
            {
                vertexBuffer.Dispose();
                lineBuffer.Dispose();
                vertexBuffer = null;
                lineBuffer = null;

                chunkLoaded = false;
            }
        }
    }
}
