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
    class WorldGenerator
    {
        private DungeonManager dungeonManager = new ForestDungeonManager();
        public static int chunksGenerated = 50; //How many chunks should be generated? Must be even and greater than 2.

        public void GenerateDungeon(WorldManager world)
        {
            dungeonManager.CreateDungeon(world);
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
