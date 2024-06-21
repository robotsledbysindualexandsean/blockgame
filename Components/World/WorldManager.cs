using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BlockGame.Components.Entities;
using BlockGame.Components.World.Dungeon;
using BlockGame.Components.World.PerlinNoise;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace BlockGame.Components.World
{
    internal class WorldManager
    {

        //World Generation Properties
        static public int chunksGenerated = 20; //Must be even, and greater than 2 :)
        static public int roomHeight = 10; //Height for all rooms

        //Array which stores all chunks in the world. 0 -> chunksGenerated
        private Chunk[,] chunks = new Chunk[chunksGenerated, chunksGenerated];

        //Refernece to graphics device
        private GraphicsDeviceManager graphics;

        //An old and deprecated array which stored perlin noise output.
        private float[,] perlinNoise;

        /// <summary>
        /// List of all chunks that need to be loaded.
        /// Chunks are loaded on an interval (one at a time) to prevent lag.
        /// </summary>
        private List<Chunk> chunksToLoad = new List<Chunk>();

        //Dungeon Manager and array storing it's output
        private DungeonManager dungeonManager = new DungeonManager();
        public DataManager dataManager;
        public int[,] dungeonMap;

        /// <summary>
        /// List of entities in the world
        /// </summary>
        public List<Entity> entities = new List<Entity>();
        public List<Vector3> toPropagate = new List<Vector3>();

        //Player reference as well
        public Player player;

        private Random rnd = new Random();

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

        public WorldManager(GraphicsDeviceManager graphics, DataManager dataManager)
        {
            this.graphics = graphics;
            this.dataManager = dataManager;

            //Generating perlin noise for terrain generation. Currenly unused (for dungeon gen)
            //perlinNoise = Perlin.GeneratePerlinNoise(chunksGenerated*Chunk.chunkLength, chunksGenerated*Chunk.chunkWidth, 8);

            //Generate chunks with all blocks filled
            GenerateChunks();

            //Add Dungeon, this cuts into the blocks
            GenerateDungeon();

            //Set up player
            player = new Player(graphics, new Vector3(0f, 25, 0f), Vector3.Zero, this, dataManager);
            entities.Add(player);
        }


        /// <summary>
        /// Generates a 2D map, and then applies to the 3D world
        /// </summary>
        private void GenerateDungeon()
        {
            //Getting 2D map
            dungeonMap = dungeonManager.GenerateDungeon((chunksGenerated - 2) * 16, (chunksGenerated - 2) * 16);

            //Offset for dungeon map array and the actual world block pos
            Vector3 arrayOffset = new Vector3(chunksGenerated * Chunk.chunkLength / 2, 0, chunksGenerated * Chunk.chunkWidth / 2);

            //Looping through each index in the 2D map, then depending on what it is, changing that column in the world
            for (int x = 0; x < (chunksGenerated - 2) * Chunk.chunkLength; x++)
            {
                for (int z = 0; z < (chunksGenerated - 2) * Chunk.chunkWidth; z++)
                {
                    //If this column has a 1, then cut a floor.
                    if (dungeonMap[x, z] == 1)
                    {
                        //Starting from the middle of the chunk, set roomHeight/2 upwards and downwards to air
                        for (int y = Chunk.chunkHeight / 2 - roomHeight / 2; y < Chunk.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to empty
                            SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 0);
                        }

                        //Generate random stone texture for floor
                        SetBlockAtWorldIndex(new Vector3(x, Chunk.chunkHeight / 2 - roomHeight / 2 - 1, z) - arrayOffset, (ushort)rnd.Next(3, 5));
                    }

                    //If the index is a wall, then set the blocks in the room to be the wall block (wood)
                    if (dungeonMap[x, z] == 2)
                    {
                        for (int y = Chunk.chunkHeight / 2 - roomHeight / 2; y < Chunk.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to be wall (wood)
                            SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, (ushort)rnd.Next(1, 3));
                        }
                    }
                    //If index is a door, then cut 3 blocks upwards from the floor.
                    else if (dungeonMap[x, z] == 3)
                    {
                        for (int y = Chunk.chunkHeight / 2 - roomHeight / 2; y < Chunk.chunkHeight / 2 - roomHeight / 2 + 5; y++)
                        {
                            //Set block to empty
                            SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 0);
                        }

                        //Set the blocks ABOVE the door to be the wall block (wood)
                        for (int y = Chunk.chunkHeight / 2 - roomHeight / 2 + 5; y < Chunk.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to wall block (wood)
                            SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 2);
                        }
                    }
                    //If the index is VOID, then from the rooms floor to the bottom of the chunk, make empty.
                    else if (dungeonMap[x, z] == 4)
                    {
                        for (int y = 0; y < Chunk.chunkHeight / 2 + roomHeight / 2; y++)
                        {
                            //Set block to empty
                            SetBlockAtWorldIndex(new Vector3(x, y, z) - arrayOffset, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates all the chunks in the world, and stores it in the 2D array. 
        /// Currently generates full chunks in the middle, and empty ones in the side (to combat a rendering issue)
        /// </summary>
        private void GenerateChunks()
        {

            //Generate PosX empty chunks
            for (int i = 0; i < chunksGenerated; i++)
            {
                //This (and all chunks below) need to have their positions offset by the chunksGenerated/2, so that they can have negative positions.
                chunks[0, i] = new Chunk(this, new Vector3(0, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[0, i].GenerateEmptyChunk();
            }

            //Generate Middle chunks (not empty)
            for (int x = 1; x < chunksGenerated-1; x++)
            {
                //Generate Side empty chunks (PosZ and NegZ)
                chunks[x,0] = new Chunk(this, new Vector3(x, 0, 0) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[x,0].GenerateEmptyChunk();
                chunks[x, chunksGenerated - 1] = new Chunk(this, new Vector3(x, 0, chunksGenerated - 1) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[x, chunksGenerated - 1].GenerateEmptyChunk();

                //Generate Middle chunks (full)
                for (int z = 1; z < chunksGenerated-1; z++)
                {
                    chunks[x, z] = new Chunk(this, new Vector3(x, 0, z) - new Vector3(chunksGenerated/2, 0, chunksGenerated / 2), graphics, dataManager);
                    chunks[x, z].GenerateFullChunk();
                }
            }

            //Generate Neg Z Empty Chunks
            for (int i = 0; i < chunksGenerated; i++)
            {
                chunks[chunksGenerated-1, i] = new Chunk(this, new Vector3(chunksGenerated - 1, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager);
                chunks[chunksGenerated - 1, i].GenerateEmptyChunk();
            }
        }

        public void Update(GameTime gameTime)
        {
            //Load all the chunks which are queued to load.
            if(chunksToLoad.Count > 0)
            {
                chunksToLoad.ElementAt(0).BuildChunk();
                chunksToLoad.RemoveAt(0);
            }

            //Update the chunks.
            foreach (Chunk chunk in chunks)
            {
                chunk.Update(player);
            }

            //Update all entities
            foreach(Entity entity in entities.ToList())
            {
                entity.Update(gameTime);
            }
        }

        public void Draw(BasicEffect basicEffect, SpriteBatch spriteBatch, SkinnedEffect skinEffect)
        {
            int counter = 0;
            foreach (Chunk chunk in chunks)
            {
                if(chunk != null && player.Camera.InFrustum(chunk.ChunkBox))
                {
                    chunk.Draw(player.Camera, basicEffect);
                    counter++;
                }
            }
            Game1.ChunksRendered = counter;

            //Draw entities
            foreach (Entity entity in entities)
            {
                entity.Draw(graphics, basicEffect, player.Camera, spriteBatch, skinEffect);
            }
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
            chunkIndex[4] = (int)(worldPos.Z % Chunk.chunkWidth);
            chunkIndex[3] = (int)worldPos.Y;
            return chunkIndex;

        }

        /// <summary>
        /// Gets the current chunk when given a 2D position
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Chunk GetChunk(Vector2 index)
        {
            if(index.X < 0 || index.Y < 0)
            {
                return null;
            }
            if(index.X >= chunksGenerated || index.Y >= chunksGenerated)
            {
                return null;
            }

            return chunks[(int)index.X, (int)index.Y];
        }

        /// <summary>
        /// Gets the block id when given a 3D world position
        /// </summary>
        /// <param name="worldpos"></param>
        /// <returns></returns>
        public ushort GetBlockAtWorldIndex(Vector3 worldpos)
        {
            int[] chunkIndex = WorldPositionToChunkIndex(worldpos);

            //greater than size of array
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return 0;
            }

            //less than 0
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return 0;
            }

            Vector3 posRelativeToChunk = new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]);

            return chunks[chunkIndex[0], chunkIndex[1]].GetBlock(posRelativeToChunk);
        }
        public ushort GetBlockLightLevelAtWorldIndex(Vector3 worldpos)
        {
            int[] chunkIndex = WorldPositionToChunkIndex(worldpos);

            //greater than size of array
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return 0;
            }

            //les than 0
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return 0;
            }

            Vector3 posRelativeToChunk = new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]);

            return chunks[chunkIndex[0], chunkIndex[1]].GetBlockLightLevel(posRelativeToChunk);
        }

        /// <summary>
        /// Sets the block to the given ID when given a world position
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="blockId"></param>
        public void SetBlockAtWorldIndex(Vector3 worldPos, ushort blockId)
        {
            int[] chunkIndex = WorldPositionToChunkIndex(worldPos);

            //greater than size of array
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return;
            }

            //les than 0
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return;
            }

            Vector3 posRelativeToChunk = new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]);

            chunks[chunkIndex[0], chunkIndex[1]].SetBlock(posRelativeToChunk, blockId);
        }

        public void SetBlockLightLevelAtWorldIndex(Vector3 worldPos, ushort newLight)
        {
            int[] chunkIndex = WorldPositionToChunkIndex(worldPos);

            //greater than size of array
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return;
            }

            //les than 0
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return;
            }

            chunks[chunkIndex[0], chunkIndex[1]].SetBlockLightLevel(new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]), newLight);
        }


        /// <summary>
        /// Returns what chunk a specific 3D world position is in. Giving in array terms (not chunk world position)
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public Vector2 WorldPositionToChunk(Vector3 worldPos)
        {
            worldPos += new Vector3(16 * chunksGenerated / 2, 0, 16 * chunksGenerated / 2);
            return new Vector2((int)(worldPos.X / Chunk.chunkLength), (int)(worldPos.Z / Chunk.chunkWidth));
            
        }

        //Determines if the given chunkPos is a loaded chunk or not
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

        /// <summary>
        /// Sets the chunks around a given posiiton in a given radius to be ready to load.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
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

        /// <summary>
        /// Builds/loads the chunks in a radius around a given position instantly.
        /// Meaning that these chunks are not queued to load next frame, but are instead loaded in the current frame.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
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

        /// <summary>
        /// Gets an array of chunks nearby in a position around a radius. Gives a square array, not a circle as radius may imply.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets an array of all blocks adjacent to a given block, taking into account the top and bottom of the world.
        /// </summary>
        /// <param name="worldPos">The world position of the block to check the adjacent blocks of.</param>
        /// <returns>An array of all blocks adjacent to worldPos.</returns>
        public Vector3[] GetAdjacentBlocks(Vector3 worldPos)
        {
            if (worldPos.Y > 0 && worldPos.Y < Chunk.chunkHeight - 1) // If this block is above the bottom of the world and below the top of the world...
            {
                Vector3[] adjacentBlocks = { worldPos + Vector3.UnitX, worldPos - Vector3.UnitX, worldPos + Vector3.UnitY, worldPos - Vector3.UnitY, worldPos + Vector3.UnitZ, worldPos - Vector3.UnitZ };
                return adjacentBlocks; // All 6 adjacent blocks.
            }
            else if (worldPos.Y <= 0) // If this block is at the bottom of the world...
            {
                Vector3[] adjacentBlocks = { worldPos + Vector3.UnitX, worldPos - Vector3.UnitX, worldPos + Vector3.UnitY, worldPos + Vector3.UnitZ, worldPos - Vector3.UnitZ };
                return adjacentBlocks; // Excludes the block below worldPos (worldPos - Vector3.UnitY).
            }
            else // If this block is at the top of the world...
            {
                Vector3[] adjacentBlocks = { worldPos + Vector3.UnitX, worldPos - Vector3.UnitX, worldPos - Vector3.UnitY, worldPos + Vector3.UnitZ, worldPos - Vector3.UnitZ };
                return adjacentBlocks; // Excludes the block above worldPos (worldPos + Vector3.UnitY).
            }
        }

        /// <summary>
        /// Calculates the correct light value for a block based on the adjacent blocks' light values.
        /// </summary>
        /// <param name="worldPos">The world position of the block to propagate light to.</param>
        public void PropagateLight(Vector3 worldPos)
        {
            Game1.LightingPasses++; // Updates the number of global light passes.

            Vector3[] targets = GetAdjacentBlocks(worldPos);
            ushort oldLight = GetBlockLightLevelAtWorldIndex(worldPos); // The light level of the block before being recalculated.
            ushort newLight = 0; // The light level to potentially change to (if it becomes higher than oldLight).

            if (GetBlockAtWorldIndex(worldPos) != 0) // If this block is not air...
            {
                return; // End.
            }

            foreach (Vector3 target in targets) // For each adjacent block...
            {
                ushort blockID = GetBlockAtWorldIndex(target); // The block ID of the current target.

                if (blockID == 0 || dataManager.blockData[blockID].IsLightSource()) // If the target block is either air or a light source...
                {
                    ushort targetLight = GetBlockLightLevelAtWorldIndex(target); // Get the light level at the target.

                    if (targetLight > newLight) // If the target's light level is greater than the currently stored newLight light level...
                    {
                        newLight = (ushort)(targetLight - 1); // Set the new light level to be 1 less than the target's light level.
                    }
                }
            }

            if (newLight > oldLight) // If the recalculated light level is greater than the original light level on the block...
            {
                SetBlockLightLevelAtWorldIndex(worldPos, newLight); // Set the light level to be equal to newLight.

                if (newLight > 1)
                {
                    foreach (Vector3 target in targets) // For each adjacent block...
                    {
                        ushort targetLight = GetBlockLightLevelAtWorldIndex(target); // Get the light level at the target.

                        if (GetBlockAtWorldIndex(target) == 0 && targetLight < newLight - 1) // If the target block is air and has a light level less than newLight - 1 (i.e., is eligible to be propagated to)...
                        {
                            PropagateLight(target); // Propagate light to the target block.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes light from a light source, de-propagating outwards to any affected blocks.
        /// </summary>
        /// <param name="worldPos">The world position of the block to remove light from.</param>
        public void DepopulateLight(Vector3 worldPos)
        {
            Game1.LightingPasses++; // Updates the number of global light passes.

            Vector3[] targets = GetAdjacentBlocks(worldPos);
            ushort oldLight = GetBlockLightLevelAtWorldIndex(worldPos); // The light level of the block before being removed.
            ushort newLight = (ushort)(oldLight - 1); // The exact threshold of light de-propagation (i.e., the light level that this light source would propagate to other blocks).

            SetBlockLightLevelAtWorldIndex(worldPos, 0); // Set the light level at worldPos to 0.

            if (newLight == 0) // If the threshold is 0...
            {
                return; // End.
            }

            foreach (Vector3 target in targets) // For each adjacent block...
            {
                ushort blockID = GetBlockAtWorldIndex(target); // The block ID of the current target.

                if (blockID == 0) // If the target block is air...
                {
                    ushort targetLight = GetBlockLightLevelAtWorldIndex(target); // Get the light level at the target.

                    if (targetLight == newLight) // If the target's light level is equal to the threshold...
                    {
                        DepopulateLight(target); // Depopulate light at the target block.
                    }
                    else if (targetLight != 0) // If the target's light level is NOT equal to the threshold and is also not zero...
                    {
                        toPropagate.Add(worldPos); // Add worldPos to the list of blocks to propagate light to after depopulation is complete.
                    }
                }
            }
        }

        public void CreateEntity(Entity entity)
        {
            entities.Add(entity);
        }

        public void DestroyEntity(Entity entity)
        {
            entities.Remove(entity);
        }
    }
}
