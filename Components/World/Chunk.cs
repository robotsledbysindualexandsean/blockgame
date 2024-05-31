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
        private string[,,] blockIDs = new string[chunkLength, chunkHeight, chunkWidth];

        //Bunch of bufferrs and graphic vars
        private GraphicsDevice graphics;
        private VertexBuffer vertexBuffer;
        private VertexBuffer lineBuffer;
        private VertexBuffer debugBuffer;
        private WorldManager world;

        //Chunk loading 
        public bool chunkLoaded = false;
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
        public List<BoundingBox> blockColliders;
        public bool drawHitboxes = false;
        public List<Vector3> blockNormals;

        public Chunk(WorldManager world, Vector3 chunkPos, GraphicsDevice graphics, DataManager dataManager)
        {
            this.world = world;
            this.chunkPos = chunkPos;
            this.graphics = graphics;
            this.dataManager = dataManager;
            this.chunkBox = new BoundingBox(new Vector3(chunkPos.X * Chunk.chunkLength * Block.blockSize, 0, chunkPos.Z * Chunk.chunkWidth * Block.blockSize), new Vector3(chunkPos.X * Chunk.chunkLength * Block.blockSize + chunkLength * Block.blockSize, chunkHeight * Block.blockSize, chunkPos.Z * Chunk.chunkWidth * Block.blockSize + chunkWidth * Block.blockSize));
            Game1.ChunkCount++;
            SetupBlockIDArray();

        }

        //Setup array to be all air.
        public void SetupBlockIDArray()
        {
            for(int x = 0; x < Chunk.chunkLength; x++)
            {
                for(int y = 0; y < chunkHeight; y++)
                {
                    for(int z = 0 ; z < chunkWidth; z++)
                    {
                        blockIDs[x, y, z] = "0000";
                    }
                }
            }
        }

        //perlin noise terrain chunk (old)
        public void GenerateChunk()
        {
            for(int x = 0; x < chunkLength; x++)
            {
                for(int z = 0; z < chunkWidth; z++)
                {
                    //Perlin Noise
                    //Getting array index for perlin noise
                    //Remember, the position does NOT equal array pos, which is why this calculation happens
                    //So this is getting block pos converted to a LengthXWidth array (which is what perlin noise uses)
                    Vector3 pos = new Vector3((chunkPos.X) * 16 + x, 0, (chunkPos.Z) * 16 + z) + new Vector3(WorldManager.chunksGenerated / 2*16, 0, WorldManager.chunksGenerated / 2*16);

                    //build column up to the perlin noise
                    for (int y = 1; y < (int)Math.Clamp((chunkHeight/2 - world.PerlinNoise[(int)pos.X,(int)pos.Z]*perlinModifer), 0, chunkHeight); y++)
                    {
                        /*                        Color colorDueToNoise = new Color(Convert.ToByte(Math.Clamp(127.5f - world.PerlinNoise[(int)pos.X, (int)pos.Z] * 127.5, 0, Convert.ToByte(255))), Convert.ToByte(127.5f), Convert.ToByte(127.5f));*/
/*                        blocks[x, y, z] = new Block(new Vector3((chunkPos.X) * 16 + x, y, (chunkPos.Z) * 16 + z), graphics);
*/                        blockIDs[x, y, z] = "0001";
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
                    for (int y = 1; y < chunkHeight-3; y++)
                    {
                        blockIDs[x, y, z] = "0002";
                    }
                }
            }
        }

        public void Draw(Camera camera, BasicEffect basicEffect)
        {
            if(vertexBuffer != null && chunkLoaded)
            {
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
                    basicEffect.Texture = DataManager.blockAtlas; /*dataManager.blockData[key].texture;*/
                    pass.Apply();

                    graphics.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3); //count is vertex / 3 since each triangle has 3 vertices

                }
/*                if(lineBuffer != null)
                {
                    //Loop through and draw each linea
                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphics.SetVertexBuffer(lineBuffer);
                        graphics.DrawPrimitives(PrimitiveType.LineList, 0, lineBuffer.VertexCount / 2); 
                    }
                }*/

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


        //Builds the vertex buffer for the chunk using all visible blocks.
        public void BuildVertexBuffer()
        {
            Game1.RebuildCalls += 1;
            //Add all verticeis
            List<VertexPositionColorTexture> vertexList = new List<VertexPositionColorTexture>();
            List<VertexPositionColor> lineList = new List<VertexPositionColor>();

            //Generate a List of all the blocks that are empty
            List<Vector3> emptyBlocks = new List<Vector3>();

            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    { 
                        if (blockIDs[x, y, z].Substring(0,4).Equals("0000"))
                        {
                            emptyBlocks.Add(new Vector3(chunkPos.X * 16 + x, y, chunkPos.Z * 16 + z)); //Converting to world coords
                        }
                    }
                }
            }

            //Check sides of the empty block, render those faces if there is a block
            foreach (Vector3 block in emptyBlocks)
            {
                //Z+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z + 1)).Equals("0000"))
                {
                    //Gen color
                    string blockID = world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z + 1));
                    Color color = new Color(Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)), Convert.ToByte(127.5f), Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)));

                    Block.AddNegZVerticiesPos(new Vector3(block.X * Block.blockSize, block.Y * Block.blockSize, (block.Z + 1) * Block.blockSize), vertexList, lineList, Color.White, Color.Black, dataManager.blockData[blockID].atlasPos);
                }
                //Z-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z - 1)).Equals("0000"))
                {
                    string blockID = world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z - 1));
                    Color color = new Color(Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)), Convert.ToByte(127.5f), Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)));
                    Block.AddPosZVerticiesPos(new Vector3(block.X * Block.blockSize, block.Y * Block.blockSize, (block.Z - 1) * Block.blockSize), vertexList, lineList, Color.White, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
                //x+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X + 1, block.Y, block.Z)).Equals("0000"))
                {
                    string blockID = world.GetBlockAtWorldIndex(new Vector3(block.X+1, block.Y, block.Z));
                    Color color = new Color(Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)), Convert.ToByte(127.5f), Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)));
                    Block.AddNegXVerticiesPos(new Vector3((block.X + 1) * Block.blockSize, block.Y * Block.blockSize, block.Z * Block.blockSize), vertexList, lineList, Color.White, Color.Black, dataManager.blockData[blockID].atlasPos);                    Game1.BlockCount++;

                }
                //x-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X - 1, block.Y, block.Z)).Equals("0000"))
                {
                    string blockID = world.GetBlockAtWorldIndex(new Vector3(block.X-1, block.Y, block.Z));
                    Color color = new Color(Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)), Convert.ToByte(127.5f), Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)));
                    Block.AddPosXVerticiesPos(new Vector3((block.X - 1) * Block.blockSize, block.Y * Block.blockSize, block.Z * Block.blockSize), vertexList, lineList, Color.White, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
                //y+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y + 1, block.Z)).Equals("0000"))
                {
                    string blockID = world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y+1, block.Z));
                    Color color = new Color(Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)), Convert.ToByte(127.5f), Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)));
                    Block.AddNegYVerticiesPos(new Vector3((block.X) * Block.blockSize, (block.Y + 1) * Block.blockSize, block.Z * Block.blockSize), vertexList, lineList, Color.White, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
                //y-1

                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y - 1, block.Z)).Equals("0000"))
                {
                    string blockID = world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y-1, block.Z));
                    Color color = new Color(Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)), Convert.ToByte(127.5f), Convert.ToByte(Math.Clamp(block.Y * 2, 0, 255)));
                    Block.AddPosYVerticiesPos(new Vector3(block.X * Block.blockSize, (block.Y - 1) * Block.blockSize, block.Z * Block.blockSize), vertexList, lineList, Color.White, Color.Black, dataManager.blockData[blockID].atlasPos);

                }
            }

            //Create buffer
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

            CreateDebugVBOList();
        }

        //Used for drawing hitboxes but this isn't used anymore.
        private void CreateDebugVBOList()
        {
            List<VertexPositionColor> debugList = new List<VertexPositionColor>();

            foreach (BoundingBox hitbox in blockColliders)
            {
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

                //
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));

                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
                debugList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));
            }

            if (debugList.Count > 0)
            {
                debugBuffer = new VertexBuffer(graphics, typeof(VertexPositionColor), debugList.Count, BufferUsage.WriteOnly);
                debugBuffer.SetData(debugList.ToArray());
            }
        }

        public string GetBlock(int[] posRelativeToChunk)
        {
            return blockIDs[posRelativeToChunk[0], posRelativeToChunk[1], posRelativeToChunk[2]].Substring(0,4);
        }

        //Rebuild the chunk (used for when the chunk is updated)
        public void RebuildChunk()
        {
            BuildVertexBuffer();
            blockColliders = GetVisibleFacesColliders();
            blockNormals = GetVisibleFaceNormals();
            CreateDebugVBOList();
        }

        public void SetBlock(Vector3 posRelativeToChunk, string block)
        {
            if((int)posRelativeToChunk.X >= chunkLength || (int)posRelativeToChunk.Z >= chunkWidth || posRelativeToChunk.Y >= chunkHeight || posRelativeToChunk.X < 0 || posRelativeToChunk.Y < 0 || posRelativeToChunk.Z < 0)
            {
                return;
            }
            
            blockIDs[(int)posRelativeToChunk.X, (int)posRelativeToChunk.Y, (int)posRelativeToChunk.Z] = block + blockIDs[(int)posRelativeToChunk.X, (int)posRelativeToChunk.Y, (int)posRelativeToChunk.Z].Substring(4);
            
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
        }

        public void GenerateEmptyChunk()
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        blockIDs[x, y, z] = "0000";
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
                RebuildChunk();
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

        public void LoadChunk()
        {
            BuildVertexBuffer();
        }


        //Updates the chunks list of hitboxes for all blocks. uses same metholog of only displaying visible blocks.
        public List<BoundingBox> GetVisibleFacesColliders()
        {
            List<BoundingBox>boxes = new List<BoundingBox>();

            List<Vector3> emptyBlocks = new List<Vector3>();

            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        if (blockIDs[x, y, z].Substring(0,4).Equals("0000"))
                        {
                            emptyBlocks.Add(new Vector3(chunkPos.X * 16 + x, y, chunkPos.Z * 16 + z));
                        }
                    }
                }
            }

            //Check sides of the empty block, add its bounding box if ithere isa  blcok
            foreach (Vector3 block in emptyBlocks)
            {

                //Z+1      
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z + 1)).Equals("0000"))
                {
                    boxes.Add(new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z + 1 - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + 1 + Block.blockSize / 2)));
                }
                //Z-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z - 1)).Equals("0000"))
                {
                    boxes.Add(new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2 - 1), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z - 1 + Block.blockSize / 2)));
                }
                //x+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X + 1, block.Y, block.Z)).Equals("0000"))
                {
                    boxes.Add(new BoundingBox(new Vector3(block.X + 1 - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + 1 + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)));

                }
                //x-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X - 1, block.Y, block.Z)).Equals("0000"))
                {
                    boxes.Add(new BoundingBox(new Vector3(block.X - 1 - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X - 1 + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)));
                }
                //y+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y + 1, block.Z)).Equals("0000"))
                {
                    boxes.Add(new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y + 1 - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + 1 + Block.blockSize / 2, block.Z + Block.blockSize / 2)));
                }
                //y-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y - 1, block.Z)).Equals("0000"))
                {
                    boxes.Add(new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - 1 - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y - 1 + Block.blockSize / 2, block.Z + Block.blockSize / 2)));
                }
            }

            return boxes;
        }

        // gets visible blocks normals
        public List<Vector3> GetVisibleFaceNormals()
        {
            List<Vector3> normals = new List<Vector3>();

            List<Vector3> emptyBlocks = new List<Vector3>();

            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        if (blockIDs[x, y, z].Substring(0,4).Equals("0000"))
                        {
                            emptyBlocks.Add(new Vector3(chunkPos.X * 16 + x, y, chunkPos.Z * 16 + z)); //Converting to world coords
                        }
                    }
                }
            }

            //Check sides of the empty block, add its bounding box if ithere isa  blcok
            foreach (Vector3 block in emptyBlocks)
            {
                //Z+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z + 1)).Equals("0000"))
                {
                    normals.Add((new Vector3(0, 0, -1)));
                }
                //Z-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z - 1)).Equals("0000"))
                {
                    normals.Add((new Vector3(0, 0, 1)));
                }
                //x+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X + 1, block.Y, block.Z)).Equals("0000"))
                {
                    normals.Add((new Vector3(-1, 0, 0)));

                }
                //x-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X - 1, block.Y, block.Z)).Equals("0000"))
                {
                    normals.Add((new Vector3(1, 0, 0)));
                }
                //y+1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y + 1, block.Z)).Equals("0000"))
                {
                    normals.Add((new Vector3(0, -1, 0)));
                }
                //y-1
                if (!world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y - 1, block.Z)).Equals("0000"))
                {
                    normals.Add((new Vector3(0, 1, 0)));
                }
            }



            return normals;
        }

    }
}
