using BlockGame.Components.World.ChunkTools;
using BlockGame.Components.World.Dungeon;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World.WorldTools
{
    /// <summary>
    /// In charge of world geneartion. Static class, not instanced per world.
    /// </summary>
    static class WorldGenerator
    {
        public static int chunksGenerated = 20; //How many chunks should be generated? Must be even and greater than 2.
        public static int roomHeight = 10; //Height of rooms


        /// <summary>
        /// Generates a 2D map, and then applies to the 3D world which is given
        /// </summary>
        public static void GenerateDungeon(int chunksGenerated, int roomHeight, WorldManager world)
        {
            //Getting 2D map
            int[,] dungeonMap = DungeonManager.GenerateDungeon((chunksGenerated - 2) * 16, (chunksGenerated - 2) * 16);

            //Offset for dungeon map array and the actual world block pos
            Vector3 arrayOffset = new Vector3(chunksGenerated * ChunkGenerator.chunkLength / 2, 0, chunksGenerated * ChunkGenerator.chunkWidth / 2);

            //Looping through each index in the 2D map, then depending on what it is, changing that column in the world
            for (int x = 0; x < (chunksGenerated - 2) * ChunkGenerator.chunkLength; x++)
            {
                for (int z = 0; z < (chunksGenerated - 2) * ChunkGenerator.chunkWidth; z++)
                {
                    //If this column has a 1, then cut a floor.
                    if (dungeonMap[x, z] == 1)
                    {
                        //Starting from the middle of the chunk, set roomHeight/2 upwards and downwards to air
                        for (int y = ChunkGenerator.chunkHeight / 2 - roomHeight / 2; y < ChunkGenerator.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to empty
                            world.SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 0);
                        }

                        //Generate random stone texture for floor
                        world.SetBlockAtWorldIndex(new Vector3(x, ChunkGenerator.chunkHeight / 2 - roomHeight / 2 - 1, z) - arrayOffset, (ushort)Game1.rnd.Next(3, 5));
                    }

                    //If the index is a wall, then set the blocks in the room to be the wall block (wood)
                    if (dungeonMap[x, z] == 2)
                    {
                        for (int y = ChunkGenerator.chunkHeight / 2 - roomHeight / 2; y < ChunkGenerator.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to be wall (wood)
                            world.SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, (ushort)Game1.rnd.Next(1, 3));
                        }
                    }
                    //If index is a door, then cut 3 blocks upwards from the floor.
                    else if (dungeonMap[x, z] == 3)
                    {
                        for (int y = ChunkGenerator.chunkHeight / 2 - roomHeight / 2; y < ChunkGenerator.chunkHeight / 2 - roomHeight / 2 + 5; y++)
                        {
                            //Set block to empty
                            world.SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 0);
                        }

                        //Set the blocks ABOVE the door to be the wall block (wood)
                        for (int y = ChunkGenerator.chunkHeight / 2 - roomHeight / 2 + 5; y < ChunkGenerator.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to wall block (wood)
                            world.SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 2);
                        }
                    }
                    //If the index is VOID, then from the rooms floor to the bottom of the chunk, make empty.
                    else if (dungeonMap[x, z] == 4)
                    {
                        for (int y = 0; y < ChunkGenerator.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to empty
                            world.SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates all the chunks in the world, and stores it in the 2D array. 
        /// Returns an array of generated chunks.
        /// </summary>
        public static Chunk[,] GenerateChunks(int chunksGenerated, WorldManager world)
        {
            Chunk[,] chunks = new Chunk[chunksGenerated, chunksGenerated];

            //Generate PosX empty chunks
            for (int i = 0; i < chunksGenerated; i++)
            {
                //This (and all chunks below) need to have their positions offset by the chunksGenerated/2, so that they can have negative positions.
                chunks[0, i] = new Chunk(world, new Vector3(0, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2)); //Create chunk object
                ChunkGenerator.GenerateEmptyChunk(chunks[0, i]); //Make it empty
            }

            //Generate Middle chunks (not empty)
            for (int x = 1; x < chunksGenerated - 1; x++)
            {
                //Generate Side empty chunks (PosZ and NegZ)
                chunks[x, 0] = new Chunk(world, new Vector3(x, 0, 0) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2)); //Create chunk object
                ChunkGenerator.GenerateEmptyChunk(chunks[x, 0]); //make above object empty

                chunks[x, chunksGenerated - 1] = new Chunk(world, new Vector3(x, 0, chunksGenerated - 1) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2)); //Create chunk object
                ChunkGenerator.GenerateEmptyChunk(chunks[x, chunksGenerated - 1]); //Make the above object empty

                //Generate Middle chunks (full)
                for (int z = 1; z < chunksGenerated - 1; z++)
                {
                    chunks[x, z] = new Chunk(world, new Vector3(x, 0, z) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2)); //Create chunk object
                    ChunkGenerator.GenerateFullChunk(chunks[x, z]); //Fill all blocks
                }
            }

            //Generate Neg Z Empty Chunks
            for (int i = 0; i < chunksGenerated; i++)
            {
                chunks[chunksGenerated - 1, i] = new Chunk(world, new Vector3(chunksGenerated - 1, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2)); //Create chunk object
                ChunkGenerator.GenerateEmptyChunk(chunks[chunksGenerated - 1, i]); //make it empty
            }

            return chunks;
        }
    }


}
