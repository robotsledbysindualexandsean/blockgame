using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BlockGame.Components.Entity;
using BlockGame.Components.World.Dungeon;
using BlockGame.Components.World.PerlinNoise;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World
{
    internal class WorldManager
    {

        //World Generation Properties
        static public int chunksGenerated = 20; //Must be even, and greater than 2 :)
        static public int roomHeight = 10; //Height for all rooms

        //Array which stores all chunks in the world. 0 -> chunksGenerated
        private Chunk[,] chunks = new Chunk[chunksGenerated, chunksGenerated];

        private GraphicsDevice graphics;
        private float[,] perlinNoise;

        //List of the chunks that need to be loaded. This is used to load one chunk per frame rather than all at once
        private List<Chunk> chunksToLoad = new List<Chunk>();

        //Dungeon Manager and array storing it's output
        private DungeonManager dungeonManager = new DungeonManager();
        private DataManager dataManager;
        public int[,] dungeonMap;

        public float[,] PerlinNoise
        {
            get { return perlinNoise; }
        }

        public int BlockCount
        {
            get { return (Chunk.chunkLength-1) * (Chunk.chunkHeight-1) * (Chunk.chunkWidth - 1) * chunksGenerated * chunksGenerated; }
        }

        public int ChunkCount
        {
            get { return chunksGenerated * chunksGenerated; }
        }

        public WorldManager(GraphicsDevice graphics, DataManager dataManager)
        {
            this.graphics = graphics;
            this.dataManager = dataManager;

            //Generating perlin noise for terrain generation. Currenly unused (for dungeon gen)
            perlinNoise = Perlin.GeneratePerlinNoise(chunksGenerated*Chunk.chunkLength, chunksGenerated*Chunk.chunkWidth, 8);

            //Generate chunks with all blocks filled
            GenerateChunks();

            //Add Dungeon, this cuts into the blocks
            GenerateDungeon();

        }


        /// <summary>
        /// Generates a 2D map, and then applies to the 3D world
        /// </summary>
        private void GenerateDungeon()
        {
            //Getting 2D map
            dungeonMap = dungeonManager.GenerateDungeon((chunksGenerated - 2) * 16, (chunksGenerated - 2) * 16);
            
            //Looping through each index in the 2D map, then depending on what it is, changing that column in the world
            for(int x = 0; x < (chunksGenerated-2)*Chunk.chunkLength; x++)
            {
                for(int z = 0; z < (chunksGenerated-2)*Chunk.chunkWidth; z++)
                {
                    //If a floor or a door, make empty
                    if (dungeonMap[x,z] == 1)
                    {
                        for(int y = Chunk.chunkHeight/2-roomHeight/2; y < Chunk.chunkHeight/2+roomHeight/2; y++)
                        {
                            //Getting the chunk index / block info using the world position. Remember our 2D array map is from 0->length however, and blocks positions are from -length->length, therefore shift
                            int[] chunk = WorldPositionToChunkIndex(new Vector3(x, y, z) - new Vector3(WorldManager.chunksGenerated / 2 * 16, 0, WorldManager.chunksGenerated / 2 * 16));

                            if (chunk[0] >= chunksGenerated || chunk[1] >= chunksGenerated)
                            {
                                continue;
                            }

                            //Set block to empty
                            chunks[chunk[0], chunk[1]].SetBlock(new Vector3(chunk[2], chunk[4], chunk[3]), 0);
                        }
                        //Floor
                        int[] chunk1 = WorldPositionToChunkIndex(new Vector3(x, Chunk.chunkHeight / 2 - roomHeight / 2 - 1, z) - new Vector3(WorldManager.chunksGenerated / 2 * 16, 0, WorldManager.chunksGenerated / 2 * 16));

                        if (chunk1[0] >= chunksGenerated || chunk1[1] >= chunksGenerated)
                        {
                            continue;
                        }

                        //Set block to empty
                        chunks[chunk1[0], chunk1[1]].SetBlock(new Vector3(chunk1[2], chunk1[4], chunk1[3]), 1);
                    }
                    else if (dungeonMap[x, z] == 3)
                    {
                        for (int y = Chunk.chunkHeight / 2 - roomHeight / 2; y < Chunk.chunkHeight / 2 - roomHeight / 2 + 3; y++)
                        {
                            //Getting the chunk index / block info using the world position. Remember our 2D array map is from 0->length however, and blocks positions are from -length->length, therefore shift
                            int[] chunk = WorldPositionToChunkIndex(new Vector3(x, y, z) - new Vector3(WorldManager.chunksGenerated / 2 * 16, 0, WorldManager.chunksGenerated / 2 * 16));

                            if (chunk[0] >= chunksGenerated || chunk[1] >= chunksGenerated)
                            {
                                continue;
                            }

                            //Set block to empty
                            chunks[chunk[0], chunk[1]].SetBlock(new Vector3(chunk[2], chunk[4], chunk[3]), 0);
                        }
                    }
                    else if (dungeonMap[x, z] == 4)
                    {
                        for (int y = 0; y < Chunk.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Getting the chunk index / block info using the world position. Remember our 2D array map is from 0->length however, and blocks positions are from -length->length, therefore shift
                            int[] chunk = WorldPositionToChunkIndex(new Vector3(x, y, z) - new Vector3(WorldManager.chunksGenerated / 2 * 16, 0, WorldManager.chunksGenerated / 2 * 16));

                            if (chunk[0] >= chunksGenerated || chunk[1] >= chunksGenerated)
                            {
                                continue;
                            }

                            //Set block to empty
                            chunks[chunk[0], chunk[1]].SetBlock(new Vector3(chunk[2], chunk[4], chunk[3]), 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates all the chunks in the world. Currently generates full chunks in the middle, and empty ones in the side (to combat a rendering issue)
        /// </summary>
        private void GenerateChunks()
        {

            //PosX empty chunks
            for (int i = 0; i < chunksGenerated; i++)
            {
                chunks[0, i] = new Chunk(this, new Vector3(0, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[0, i].GenerateEmptyChunk();
            }

            //Middle chunks (not empty)
            for (int x = 1; x < chunksGenerated-1; x++)
            {
                //Side empty chunks (PosZ and NegZ)
                chunks[x,0] = new Chunk(this, new Vector3(x, 0, 0) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[x,0].GenerateEmptyChunk();
                chunks[x, chunksGenerated - 1] = new Chunk(this, new Vector3(x, 0, chunksGenerated - 1) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[x, chunksGenerated - 1].GenerateEmptyChunk();

                //Middle chunks
                for (int z = 1; z < chunksGenerated-1; z++)
                {
                    chunks[x, z] = new Chunk(this, new Vector3(x, 0, z) - new Vector3(chunksGenerated/2, 0, chunksGenerated / 2), graphics, dataManager);
                    chunks[x, z].GenerateFullChunk();
                    //^^^ subtaracting chunksgenerated/2 centers the chunks around 0,0
                }
            }

            //Neg Z Empty Chunks
            for (int i = 0; i < chunksGenerated; i++)
            {
                chunks[chunksGenerated-1, i] = new Chunk(this, new Vector3(chunksGenerated - 1, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[chunksGenerated - 1, i].GenerateEmptyChunk();
            }
        }

        public void Update(Player player)
        {
            //load queued chunks
            //this is to buffer loading!
            if(chunksToLoad.Count > 0)
            {
                chunksToLoad.ElementAt(0).BuildChunk();
                chunksToLoad.RemoveAt(0);
            }

            foreach (Chunk chunk in chunks)
            {
                chunk.Update(player);
            }

        }

        public void Draw(Camera camera, BasicEffect basicEffect)
        {
            int counter = 0;
            foreach (Chunk chunk in chunks)
            {
                if(chunk != null && camera.InFrustum(chunk.ChunkBox))
                {
                    chunk.Draw(camera, basicEffect);
                    counter++;
                }
            }
            Game1.ChunksRendered = counter;
        }

        //First 2 are chunk X,Z, then block X,Y,Z in chunk Index
        //Converts a world position (XYZ) into a chunk position (XZ,XYZ)
        //This is useful since blocks have a WORLD position, however chunks are storing them in chunk positon
        //This way, WorldManager and Chunks can access other chunks blocks using ChunkIndex
        public static int[] WorldPositionToChunkIndex(Vector3 worldPos)
        {
            worldPos += new Vector3( 16 * chunksGenerated / 2,  0,  16 * chunksGenerated / 2); //since the chunks actual pos is cnetered at 0,0 readd the old centering to reset it to not be.
            int[] chunkIndex = new int[5];
            chunkIndex[0] = (int)(worldPos.X / Chunk.chunkLength);
            chunkIndex[1] = (int)(worldPos.Z / Chunk.chunkWidth);
            chunkIndex[2] = (int)Math.Abs(worldPos.X % Chunk.chunkLength);
            chunkIndex[3] = (int)(worldPos.Z % Chunk.chunkWidth);
            chunkIndex[4] = (int)worldPos.Y;
            return chunkIndex;

        }

        public Chunk GetChunk(Vector2 index)
        {
            if(index.X < 0 || index.Y < 0)
            {
                return null;
            }
            if(index.Y >= chunksGenerated || index.Y >= chunksGenerated)
            {
                return null;
            }
            
            return chunks[(int)index.X, (int)index.Y];
        }

        public ushort GetBlockAtWorldIndex(Vector3 worldpos)
        {
            int[] chunkIndex = WorldPositionToChunkIndex(worldpos);

            //greater than size of array
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[3]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[4]) >= Chunk.chunkHeight)
            {
                return 0;
            }

            //les than 0
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return 0;
            }

            return chunks[chunkIndex[0], chunkIndex[1]].GetBlock(new int[] { chunkIndex[2], chunkIndex[4], chunkIndex[3] });
        }

        public Vector2 WorldPositionToChunk(Vector3 worldPos)
        {
            worldPos += new Vector3(16 * chunksGenerated / 2, 0, 16 * chunksGenerated / 2);
            return new Vector2((int)(worldPos.X / Chunk.chunkLength), (int)(worldPos.Z / Chunk.chunkWidth));
            
        }

        public bool IsChunkLoaded(Vector2 chunkPos)
        {
            if(chunkPos.X > chunksGenerated || chunkPos.Y > chunksGenerated)
            {
                return false;
            }

            if(chunkPos.X < 0 || chunkPos.Y < 0)
            {
                return false;
            }
            
            if (chunks[(int)chunkPos.X, (int)chunkPos.Y].chunkLoaded)
            {
                return true;
            }
            return false;
        }

        public void LoadChunks(Vector2 position, int radius)
        {
            for(int x = -radius/2; x <= radius/2; x++)
            {
                for(int z = -radius/2; z <= radius/2; z++)
                {
                    //Checking to make sure not out of bounds
                    if (position.X + x >= chunksGenerated || position.Y + z >= chunksGenerated)
                    {
                        continue;
                    }

                    //les than 0
                    if (position.X + x < 0 || position.Y + z < 0)
                    {
                        continue;
                    }

                    if (chunks[(int)position.X + x, (int)position.Y+z] != null && chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded == false)
                    {
                        chunksToLoad.Add(chunks[(int)position.X + x, (int)position.Y + z]);
                        chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded = true;
                    }
                }
            }
        }

        public void LoadChunksInstantly(Vector2 position, int radius)
        {
            for (int x = -radius / 2; x <= radius / 2; x++)
            {
                for (int z = -radius / 2; z <= radius / 2; z++)
                {
                    //Checking to make sure not out of bounds
                    if (position.X + x >= chunksGenerated || position.Y + z >= chunksGenerated)
                    {
                        continue;
                    }

                    //les than 0
                    if (position.X + x < 0 || position.Y + z < 0)
                    {
                        continue;
                    }

                    if (chunks[(int)position.X + x, (int)position.Y + z] != null && chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded == false)
                    {
                        chunks[(int)position.X + x, (int)position.Y + z].BuildChunk();
                        chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded = true;

                    }
                }
            }
        }

        public Chunk[,] GetChunksNearby(Vector3 position, int radius)
        {
            Chunk[,] array = new Chunk[1 + radius * 2, 1 + radius * 2];

            Vector2 chunk = WorldPositionToChunk(position);

            for (int x = 0; x <= radius * 2; x++)
            {
                for (int z = 0; z <= radius * 2; z++)
                {
                    if (chunk.X + x - radius >= chunksGenerated || chunk.Y + z - radius >= chunksGenerated)
                    {
                        continue;
                    }

                    //les than 0
                    if (chunk.X + x - radius < 0 || chunk.Y + z - radius < 0)
                    {
                        continue;
                    }

                    array[x, z] = chunks[(int)chunk.X + x - radius, (int)chunk.Y + z - radius];
                }
            }

            return array;
        }
    }
}
