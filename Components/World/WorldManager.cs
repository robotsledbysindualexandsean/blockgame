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
        static public int chunksGenerated = 20; //How many chunks (square diameter) will be generated. Must be even, and greater than 2 :)
        static public int roomHeight = 10; //Height for all rooms

        private Chunk[,] chunks = new Chunk[chunksGenerated, chunksGenerated]; //Array which stores all chunks in the world. 0 -> chunksGenerated

        private GraphicsDeviceManager graphics; //Refernece to graphics device

        /// <summary>
        /// List of all chunks that need to be loaded.
        /// Chunks are loaded on an interval (one at a time, one per frame) to prevent lag.
        /// </summary>
        private List<Chunk> chunksToLoad = new List<Chunk>();

        private DungeonManager dungeonManager = new DungeonManager(); //Dungeon Manager
        public DataManager dataManager; //DataManager reference
        public int[,] dungeonMap; //Array storing the 2d dungoen map

        public List<Entity> entities = new List<Entity>(); //List of all entities in this world
        public List<Vector3> toPropagate = new List<Vector3>(); //List of blocks that need to be propogated

        public Player player; //Reference to the player

        private Random rnd = new Random();

        /// <summary>
        /// Debug getters and setters.
        /// used to display how many chunks are generated/ blocks generated.
        /// </summary>
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
            //Setting variables
            this.graphics = graphics;
            this.dataManager = dataManager;

            GenerateChunks(); //Generate chunks with all blocks filled

            GenerateDungeon(); //"Cut" the dungeon into the chunks

            player = new Player(graphics, new Vector3(0f, 25, 0f), Vector3.Zero, this, dataManager); //Create player entity
            entities.Add(player); //Add it to the list of entities
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
                chunks[0, i] = new Chunk(this, new Vector3(0, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager); //Create chunk object
                chunks[0, i].GenerateEmptyChunk(); //Make it empty
            }

            //Generate Middle chunks (not empty)
            for (int x = 1; x < chunksGenerated-1; x++)
            {
                //Generate Side empty chunks (PosZ and NegZ)
                chunks[x,0] = new Chunk(this, new Vector3(x, 0, 0) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager); //Create chunk object
                chunks[x,0].GenerateEmptyChunk(); //make above object empty

                chunks[x, chunksGenerated - 1] = new Chunk(this, new Vector3(x, 0, chunksGenerated - 1) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager); //Create chunk object
                chunks[x, chunksGenerated - 1].GenerateEmptyChunk(); //Make the above object empty

                //Generate Middle chunks (full)
                for (int z = 1; z < chunksGenerated-1; z++)
                {
                    chunks[x, z] = new Chunk(this, new Vector3(x, 0, z) - new Vector3(chunksGenerated/2, 0, chunksGenerated / 2), graphics, dataManager); //Create chunk object
                    chunks[x, z].GenerateFullChunk(); //Fill all blocks
                }
            }

            //Generate Neg Z Empty Chunks
            for (int i = 0; i < chunksGenerated; i++)
            {
                chunks[chunksGenerated-1, i] = new Chunk(this, new Vector3(chunksGenerated - 1, 0, i) - new Vector3(chunksGenerated / 2, 0, chunksGenerated / 2), graphics, dataManager); //Create chunk object
                chunks[chunksGenerated - 1, i].GenerateEmptyChunk(); //make it empty
            }
        }

        public void Update(GameTime gameTime)
        {
            //Load the first chunk in the chunkstoload frame (this loads one chunk per frame)
            if(chunksToLoad.Count > 0)
            {
                chunksToLoad.ElementAt(0).BuildChunk(); //Build the 0th element chunk
                chunksToLoad.RemoveAt(0); //Remove that chunk from the list of chunks needed to be loaded (it has been loaded this frame)
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
            int counter = 0; //Counter for counting how many chunks get rendered

            //For each chunk in the world, call its draw method
            foreach (Chunk chunk in chunks)
            {
                if(chunk != null && player.Camera.InFrustum(chunk.ChunkBox))
                {
                    chunk.Draw(player.Camera, basicEffect); //Draw chunk
                    counter++;
                }
            }
            Game1.ChunksRendered = counter;

            //For each entity in the world, call its draw method
            foreach (Entity entity in entities)
            {
                entity.Draw(graphics, basicEffect, player.Camera, spriteBatch, skinEffect);
            }
        }

        //First 2 are chunk X,Z, then block X,Y,Z in chunk Index
        //Converts a world position (XYZ) into a chunk position (XZ,XYZ)
        //This is useful since blocks have a WORLD position, however chunks are storing them in chunk positon
        //This way, WorldManager and Chunks can access other chunks blocks using ChunkIndex
        public static int[] posInWorlditionToChunkIndex(Vector3 posInWorld)
        {
            posInWorld += new Vector3( 16 * chunksGenerated / 2,  0,  16 * chunksGenerated / 2); //since the chunks actual pos is cnetered at 0,0 readd the old centering to reset it to not be.
            int[] chunkIndex = new int[5]; //Creating arraay
            chunkIndex[0] = (int)(posInWorld.X / Chunk.chunkLength); //Chunk X coordinate
            chunkIndex[1] = (int)(posInWorld.Z / Chunk.chunkWidth); //Chunk Y coordinate
            chunkIndex[2] = (int)Math.Abs(posInWorld.X % Chunk.chunkLength); //Block X coordinate
            chunkIndex[4] = (int)(posInWorld.Z % Chunk.chunkWidth); //Block Z coordinate
            chunkIndex[3] = (int)posInWorld.Y; //Block Y coordinate
            return chunkIndex;

        }

        /// <summary>
        /// Gets the current chunk when given a 2D position
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Chunk GetChunk(Vector2 index)
        {
            //If out of bounds, then just return null
            if(index.X < 0 || index.Y < 0)
            {
                return null;
            }
            if(index.X >= chunksGenerated || index.Y >= chunksGenerated)
            {
                return null;
            }

            return chunks[(int)index.X, (int)index.Y]; //Return the chunk at this index
        }

        /// <summary>
        /// Gets the block id when given a 3D world position
        /// </summary>
        /// <param name="posInWorld">3D coordinate in the world</param>
        /// <returns>Block ID</returns>
        public ushort GetBlockAtWorldIndex(Vector3 posInWorld)
        {
            int[] chunkIndex = posInWorlditionToChunkIndex(posInWorld); //Gets the block data

            //If greater than array size, return air
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return 0;
            }

            //IF less than array size, return air
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return 0;
            }

            Vector3 posInChunk = new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]); //Turn array data into vector so it can be passed into method

            return chunks[chunkIndex[0], chunkIndex[1]].GetBlock(posInChunk); //Return the block in the chunk, using GetBlock.
        }

        //Gets a blocks light level at a world index
        public ushort GetBlockLightLevelAtWorldIndex(Vector3 posInWorld)
        {
            int[] chunkIndex = posInWorlditionToChunkIndex(posInWorld); //gets block data

            //If greater than array size, return no light
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return 0;
            }

            //IF less than array size, return no light
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return 0;
            }

            Vector3 posInChunk = new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]); //Turn array data into vector so it can be passed into method

            return chunks[chunkIndex[0], chunkIndex[1]].GetBlockLightLevel(posInChunk); //Return the block in the chunk, using GetBlockLightLevel.
        }

        /// <summary>
        /// Sets the block to the given ID when given a world position
        /// </summary>
        /// <param name="posInWorld">World position to change block to</param>
        /// <param name="blockId">Block to be changed to</param>
        public void SetBlockAtWorldIndex(Vector3 posInWorld, ushort blockId)
        {
            int[] chunkIndex = posInWorlditionToChunkIndex(posInWorld); //Get block data

            //If greater than array size, stop method
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return;
            }

            //IF less than array size, stop method
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return;
            }

            Vector3 posInChunk = new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]); //Turn array data into vector so it can be passed into method

            chunks[chunkIndex[0], chunkIndex[1]].SetBlock(posInChunk, blockId); //Setting block
        }

        /// <summary>
        /// Sets a blocks light level, when given a world index
        /// </summary>
        /// <param name="posInWorld">Wolrd index to change block light level</param>
        /// <param name="newLight">Light level to change to</param>
        public void SetBlockLightLevelAtWorldIndex(Vector3 posInWorld, ushort newLight)
        {
            int[] chunkIndex = posInWorlditionToChunkIndex(posInWorld); //Get block data

            //If greater than array size, stop method
            if (chunkIndex[0] >= chunksGenerated || Math.Abs(chunkIndex[1]) >= chunksGenerated || Math.Abs(chunkIndex[2]) >= Chunk.chunkLength || Math.Abs(chunkIndex[4]) >= Chunk.chunkWidth || Math.Abs(chunkIndex[3]) >= Chunk.chunkHeight)
            {
                return;
            }

            //IF less than array size, stop method
            if (chunkIndex[0] < 0 || chunkIndex[1] < 0 || chunkIndex[2] < 0 || chunkIndex[3] < 0 || chunkIndex[4] < 0)
            {
                return;
            }

            chunks[chunkIndex[0], chunkIndex[1]].SetBlockLightLevel(new Vector3(chunkIndex[2], chunkIndex[3], chunkIndex[4]), newLight); //Setting block light level
        }


        /// <summary>
        /// Returns what chunk a specific 3D world position is in. Giving in array terms (not chunk world position)
        /// </summary>
        /// <param name="posInWorld"></param>
        /// <returns></returns>
        public Vector2 posInWorlditionToChunk(Vector3 posInWorld)
        {
            posInWorld += new Vector3(16 * chunksGenerated / 2, 0, 16 * chunksGenerated / 2); //Offsetting posinWorld so that its in array index terms
            return new Vector2((int)(posInWorld.X / Chunk.chunkLength), (int)(posInWorld.Z / Chunk.chunkWidth)); //Return that chunk
            
        }

        //Determines if the given chunkPos is a loaded chunk or not
        public bool IsChunkLoaded(Vector2 chunkPos)
        {
            //If out of bounds, return false
            if(chunkPos.X > chunksGenerated || chunkPos.Y > chunksGenerated)
            {
                return false;
            }

            if(chunkPos.X < 0 || chunkPos.Y < 0)
            {
                return false;
            }
            
            if (chunks[(int)chunkPos.X, (int)chunkPos.Y].chunkLoaded) //If loaded, return true
            {
                return true;
            }
            return false; //Else return false
        }

        /// <summary>
        /// Sets the chunks around a given positon in a given radius to be ready to load, and will begin loading next frame.
        /// </summary>
        /// <param name="position">INital chunk which will have the chunks aronud it loaded</param>
        /// <param name="radius">Radius of chunks that will be loaded</param>
        public void LoadChunks(Vector2 position, int radius)
        {
            //Foreach chunk within the radius... Load if valid
            for(int x = -radius/2; x <= radius/2; x++)
            {
                for(int z = -radius/2; z <= radius/2; z++)
                {
                    //Checking to make sure not out of bounds.
                    if (position.X + x >= chunksGenerated || position.Y + z >= chunksGenerated)
                    {
                        continue;
                    }

                    //Checking that chunk is not less than 0 in array index
                    if (position.X + x < 0 || position.Y + z < 0)
                    {
                        continue;
                    }

                    if (chunks[(int)position.X + x, (int)position.Y+z] != null && chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded == false) //If a valid chunk, and is not loaded already, set it to be ready to load
                    {
                        chunksToLoad.Add(chunks[(int)position.X + x, (int)position.Y + z]); //Add to chunks to load list
                        chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded = true; //Tell chunk that iti s loaded
                    }
                }
            }
        }

        /// <summary>
        /// Builds/loads the chunks in a radius around a given position instantly.
        /// Meaning that these chunks are not queued to load next frame, but are instead loaded in the current frame.
        /// </summary>
        /// <param name="position">INital chunk which will have the chunks aronud it loaded</param>
        /// <param name="radius">Radius of chunks that will be loaded</param>
        public void LoadChunksInstantly(Vector2 position, int radius)
        {
            //For each chunk in the radius.. load if avalid
            for (int x = -radius / 2; x <= radius / 2; x++)
            {
                for (int z = -radius / 2; z <= radius / 2; z++)
                {
                    //Checking to make sure not out of bounds.
                    if (position.X + x >= chunksGenerated || position.Y + z >= chunksGenerated)
                    {
                        continue;
                    }

                    //Checking if the chunk is not less than 0 in the array index.
                    if (position.X + x < 0 || position.Y + z < 0)
                    {
                        continue;
                    }

                    if (chunks[(int)position.X + x, (int)position.Y + z] != null && chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded == false) //If a valid chunk that isnt loaded already, then load it
                    {
                        chunks[(int)position.X + x, (int)position.Y + z].BuildChunk(); //Build the chunk
                        chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded = true; //Turn it to loaded

                    }
                }
            }
        }

        /// <summary>
        /// Gets an array of chunks nearby in a position around a radius. Gives a square array, not a circle as radius may imply.
        /// </summary>
        /// <param name="position">Position which the "chunks nearby" are located for</param>
        /// <param name="radius"> Square radius around position to get chunks </param>
        /// <returns></returns>
        public Chunk[,] GetChunksNearby(Vector3 position, int radius)
        {
            Chunk[,] array = new Chunk[1 + radius * 2, 1 + radius * 2]; //Create 2D array of chunks

            Vector2 chunk = posInWorlditionToChunk(position); //Get the current chunk the world position is in

            //For each chunk around that position... Add it to the array if valid
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

                    array[x, z] = chunks[(int)chunk.X + x - radius, (int)chunk.Y + z - radius]; //Add to 2D array
                }
            }

            return array; //Return 2D array of chunks nearby
        }

        /// <summary>
        /// Gets an array of all blocks adjacent to a given block, taking into account the top and bottom of the world.
        /// </summary>
        /// <param name="posInWorld">The world position of the block to check the adjacent blocks of.</param>
        /// <returns>An array of all blocks adjacent to posInWorld.</returns>
        public Vector3[] GetAdjacentBlocks(Vector3 posInWorld)
        {
            if (posInWorld.Y > 0 && posInWorld.Y < Chunk.chunkHeight - 1) // If this block is above the bottom of the world and below the top of the world...
            {
                Vector3[] adjacentBlocks = { posInWorld + Vector3.UnitX, posInWorld - Vector3.UnitX, posInWorld + Vector3.UnitY, posInWorld - Vector3.UnitY, posInWorld + Vector3.UnitZ, posInWorld - Vector3.UnitZ };
                return adjacentBlocks; // All 6 adjacent blocks.
            }
            else if (posInWorld.Y <= 0) // If this block is at the bottom of the world...
            {
                Vector3[] adjacentBlocks = { posInWorld + Vector3.UnitX, posInWorld - Vector3.UnitX, posInWorld + Vector3.UnitY, posInWorld + Vector3.UnitZ, posInWorld - Vector3.UnitZ };
                return adjacentBlocks; // Excludes the block below posInWorld (posInWorld - Vector3.UnitY).
            }
            else // If this block is at the top of the world...
            {
                Vector3[] adjacentBlocks = { posInWorld + Vector3.UnitX, posInWorld - Vector3.UnitX, posInWorld - Vector3.UnitY, posInWorld + Vector3.UnitZ, posInWorld - Vector3.UnitZ };
                return adjacentBlocks; // Excludes the block above posInWorld (posInWorld + Vector3.UnitY).
            }
        }

        /// <summary>
        /// Calculates the correct light value for a block based on the adjacent blocks' light values.
        /// </summary>
        /// <param name="posInWorld">The world position of the block to propagate light to.</param>
        public void PropagateLight(Vector3 posInWorld)
        {
            Game1.LightingPasses++; // Updates the number of global light passes.

            Vector3[] targets = GetAdjacentBlocks(posInWorld);
            ushort oldLight = GetBlockLightLevelAtWorldIndex(posInWorld); // The light level of the block before being recalculated.
            ushort newLight = 0; // The light level to potentially change to (if it becomes higher than oldLight).

            if (GetBlockAtWorldIndex(posInWorld) != 0) // If this block is not air...
            {
                return; // End.
            }

            // Sets newLight to be equal to the highest light level in the adjacent blocks - 1.
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
                SetBlockLightLevelAtWorldIndex(posInWorld, newLight); // Set the light level to be equal to newLight.

                if (newLight > 1)
                {
                    // Propagate light to each eligible adjacent block.
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
        /// <param name="posInWorld">The world position of the block to remove light from.</param>
        public void DepopulateLight(Vector3 posInWorld)
        {
            Game1.LightingPasses++; // Updates the number of global light passes.

            Vector3[] targets = GetAdjacentBlocks(posInWorld);
            ushort oldLight = GetBlockLightLevelAtWorldIndex(posInWorld); // The light level of the block before being removed.
            ushort newLight = (ushort)(oldLight - 1); // The exact threshold of light de-propagation (i.e., the light level that this light source would propagate to other blocks).

            SetBlockLightLevelAtWorldIndex(posInWorld, 0); // Set the light level at posInWorld to 0.

            if (newLight == 0) // If the threshold is 0...
            {
                return; // End.
            }

            // Depopulates light from each eligible adjacent block and queues light propagation to lit edge blocks.
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
                        toPropagate.Add(posInWorld); // Add posInWorld to the list of blocks to propagate light to after depopulation is complete.
                    }
                }
            }
        }

        //Add an entity to the list of entities
        public void CreateEntity(Entity entity)
        {
            entities.Add(entity);
        }

        //Remove an entity from the list of entities
        public void DestroyEntity(Entity entity)
        {
            entities.Remove(entity);
        }
    }
}
