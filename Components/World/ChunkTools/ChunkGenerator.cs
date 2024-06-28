using BlockGame.Components.World.WorldTools;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World.ChunkTools
{
    /// <summary>
    /// In charge of generating chunks, as well as generating their colliders.
    /// TO DO:
    /// make a hitbox class which entities have, and then just add/remove htiboxes for blocks  on setblock  
    /// </summary>
    internal class ChunkGenerator
    {
        private Chunk chunk; //Reference to the chunk this is for
        public List<Face> facesWithColliders = new List<Face>(); //List of all faces with colliders

        //Chunk size
        public static int chunkLength = 16;
        public static int chunkWidth = 16;
        public static int chunkHeight = 50;

        private BoundingBox chunkBox; //Hitbox used for Frustum Calcs

        public BoundingBox ChunkBox
        {
            get { return chunkBox; }
        }


        public ChunkGenerator(Chunk chunk)
        {
            this.chunk = chunk;

            //Creating the chunks hitbox , used in frustum calcs.
            this.chunkBox = new BoundingBox(new Vector3(chunk.chunkPos.X * ChunkGenerator.chunkLength * Block.blockSize, 0, chunk.chunkPos.Z * ChunkGenerator.chunkWidth * Block.blockSize), new Vector3(chunk.chunkPos.X * ChunkGenerator.chunkLength * Block.blockSize + chunkLength * Block.blockSize, chunkHeight * Block.blockSize, chunk.chunkPos.Z * ChunkGenerator.chunkWidth * Block.blockSize + chunkWidth * Block.blockSize));
        }

        /// <summary>
        /// Updates the list of faces that have colliders
        /// </summary>
        /// <param name="blockIDs"></param>
        /// <param name="world"></param>
        /// <summary>
        /// Method which builds all the data for the visible faces, such as hitboxes, normals, positions
        /// This is all stored in the facesWithColliders list for reference.
        /// </summary>
        public void BuildFacesWithColliders(ushort[,,][] blockIDs, WorldManager world)
        {
            //Generate a List of all the blocks that are empty
            List<Vector3> emptyBlocks = new List<Vector3>();

            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        if (!DataManager.blockData[blockIDs[x, y, z][0]].collide) //If this block has no collision
                        {
                            emptyBlocks.Add(new Vector3(chunk.chunkPos.X * 16 + x, y, chunk.chunkPos.Z * 16 + z)); //Converting to world coords
                        }
                    }
                }
            }

            facesWithColliders.Clear();

            //Add all the faces around the empty blocks, as these are all visible blocks.
            foreach (Vector3 block in emptyBlocks)
            {
                //Z+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z + 1)) != 0)
                {
                    facesWithColliders.Add(new Face(new Vector3(block.X, block.Y, block.Z + 1),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z + 1 - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + 1 + Block.blockSize / 2)),
                        new Vector3(0, 0, -1)));
                }
                //Z-1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y, block.Z - 1)) != 0)
                {
                    facesWithColliders.Add(new Face(new Vector3(block.X, block.Y, block.Z - 1),
                         new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - 1 - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z - 1 + Block.blockSize / 2)),
                         new Vector3(0, 0, +1)));

                }
                //x+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X + 1, block.Y, block.Z)) != 0)
                {
                    facesWithColliders.Add(new Face(new Vector3(block.X + 1, block.Y, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2 + 1, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + 1 + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(-1, 0, 0)));
                }
                //x-1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X - 1, block.Y, block.Z)) != 0)
                {
                    facesWithColliders.Add(new Face(new Vector3(block.X - 1, block.Y, block.Z),
                        new BoundingBox(new Vector3(block.X - 1 - Block.blockSize / 2, block.Y - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X - 1 + Block.blockSize / 2, block.Y + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(1, 0, 0)));
                }
                //y+1
                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y + 1, block.Z)) != 0)
                {
                    facesWithColliders.Add(new Face(new Vector3(block.X, block.Y + 1, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - Block.blockSize / 2 + 1, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y + Block.blockSize / 2 + 1, block.Z + Block.blockSize / 2)),
                        new Vector3(0, -1, 0)));

                }
                //y-1

                if (world.GetBlockAtWorldIndex(new Vector3(block.X, block.Y - 1, block.Z)) != 0)
                {
                    facesWithColliders.Add(new Face(new Vector3(block.X, block.Y - 1, block.Z),
                        new BoundingBox(new Vector3(block.X - Block.blockSize / 2, block.Y - 1 - Block.blockSize / 2, block.Z - Block.blockSize / 2), new Vector3(block.X + Block.blockSize / 2, block.Y - 1 + Block.blockSize / 2, block.Z + Block.blockSize / 2)),
                        new Vector3(0, 1, 0)));
                }
            }

            //chunk.renderer.BuildHitboxes(); //Build the hitboxes using the renderer for debug
        }

        //Geneates a chunk completley filled with a block
        public static void GenerateFullChunk(Chunk chunk)
        {
            for (int x = 0; x < chunkLength; x++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {
                    //Starts 1 and ends - 3 from top to avoid rendering bug
                    for (int y = 1; y < chunkHeight - 3; y++)
                    {
                        chunk.blockIDs[x, y, z][0] = 3;
                    }
                }
            }
        }

        //Returns an initalized array for a chunk
        public static ushort[,,][] SetupBlockIDArray()
        {
            ushort[,,][] blockIDs = new ushort[chunkLength, chunkHeight, chunkWidth][];

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

            return blockIDs; //Return array
        }

        /// <summary>
        /// Generates a chunk of air
        /// </summary>
        public static void GenerateEmptyChunk(Chunk chunk)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkLength; x++)
                {
                    for (int z = 0; z < chunkWidth; z++)
                    {
                        chunk.blockIDs[x, y, z][0] = 0;
                    }
                }
            }
        }

    }
}
