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
using BlockGame.Components.World.ChunkTools;

namespace BlockGame.Components.World.WorldTools
{
    internal class WorldManager
    {
        private Chunk[,] chunks = new Chunk[WorldGenerator.chunksGenerated, WorldGenerator.chunksGenerated]; //Array which stores all chunks in the world. 0 -> chunksGenerated

        //Chunks have their hitbox and vertex buffer made on frame at a time. All hitboxes are made first before rendering.
        private List<Chunk> chunksToGenHitbox = new List<Chunk>(); //Chunks that need their hitbox gneerated
        private List<Chunk> chunksToRender = new List<Chunk> (); //chunks that need to be rendered
        private List<Chunk> chunksToBuildAnim = new List<Chunk>(); //chunks that need its animation rebuilt

        public List<Entity> entities = new List<Entity>(); //List of all entities in this world
        public List<Vector3> toPropagate = new List<Vector3>(); //List of blocks that need to be propogated

        private int animationCounter = 0; //Animation counter, used to update all block animations

        public Player player; //Reference to the player

        public WorldManager()
        {
            player = new Player(new Vector3(0f, 25, 0f), Vector3.Zero, this); //Create player entity
            entities.Add(player); //Add it to the list of entities

            chunks = WorldGenerator.GenerateChunks(WorldGenerator.chunksGenerated, this); 

            WorldGenerator.GenerateDungeon(WorldGenerator.chunksGenerated, WorldGenerator.roomHeight, this); //"Cut" the dungeon into the chunks
        }

        public void Update(GameTime gameTime)
        {
            //Generate one chunks hitbox per frame
            if (chunksToGenHitbox.Count > 0)
            {
                chunksToGenHitbox.ElementAt(0).generator.BuildFacesWithColliders(chunksToGenHitbox.ElementAt(0).blockIDs, this); //Build the 0th element chunk hitbox
                chunksToGenHitbox.RemoveAt(0); //Remove that chunk from the list of chunks needed to be loaded (it has been loaded this frame)
            }
            else if (chunksToRender.Count > 0) //IF all hitboxes have been made, start rendering
            {
                chunksToRender.ElementAt(0).renderer.BuildVertexBuffer(chunksToRender.ElementAt(0).blockIDs, this); //Build the 0th element chunk hitbox
                chunksToRender.RemoveAt(0); //Remove that chunk from the list of chunks needed to be loaded (it has been loaded this frame)
            }


            //Update the chunks.
            foreach (Chunk chunk in chunks)
            {
                chunk.Update(player);
            }

            //Update all entities
            foreach (Entity entity in entities.ToList())
            {
                entity.Update(gameTime);
            }

            //update animations
            animationCounter++;
            if (animationCounter > 5)
            {
                animationCounter = 0; //reset counter

                //Update block animations
                foreach (Block block in DataManager.blockData.Values)
                {
                    block.UpdateAnimations();
                }

                Game1.AnimatedBlocks = 0;
                foreach (Chunk chunk in GetChunksNearby(player.position, 3))
                {
                    if (player.Camera.InFrustum(chunk.generator.ChunkBox) && !chunk.rebuildNextFrame)
                    {
                        chunk.renderer.BuildAnimationBuffer(chunk.blockIDs, this); //Build animation buffer again
                        Game1.AnimatedBlocks += chunk.renderer.animationFaces.Count;
                    }
                }
            }
        }


        public void Draw(BasicEffect basicEffect, SpriteBatch spriteBatch, SkinnedEffect skinEffect)
        {
            List<Chunk> chunksToRender = new List<Chunk>(); //List of chunks that need to be loaded

            //For each chunk in the world, add it to the chunk to render list
            foreach (Chunk chunk in chunks)
            {
                if (chunk != null && player.Camera.InFrustum(chunk.generator.ChunkBox)) //Check if chunk is within the players camera frustrum
                {
                    chunksToRender.Add(chunk);
                }
            }

            chunksToRender = chunksToRender.OrderBy(x => Vector3.Distance(x.chunkPos, player.position)).ToList(); //Sort by ascending distance to player. Chunks will be drawn from front to back for depth purposes (transparency)

            //Draw all chunks, once sorted
            foreach(Chunk chunk in chunksToRender)
            {
                chunk.Draw(player.Camera, basicEffect); //Draw chunk
            }

            //For each entity in the world, call its draw method
            foreach (Entity entity in entities)
            {
                entity.Draw(basicEffect, player.Camera, spriteBatch, skinEffect);
            }
        }

        //First 2 are chunk X,Z, then block X,Y,Z in chunk Index
        //Converts a world position (XYZ) into a chunk position (XZ,XYZ)
        //This is useful since blocks have a WORLD position, however chunks are storing them in chunk positon
        //This way, WorldManager and Chunks can access other chunks blocks using ChunkIndex
        public static int[] posInWorlditionToChunkIndex(Vector3 posInWorld)
        {
            posInWorld += new Vector3(16 * WorldGenerator.chunksGenerated / 2, 0, 16 * WorldGenerator.chunksGenerated / 2); //since the chunks actual pos is cnetered at 0,0 readd the old centering to reset it to not be.
            int[] chunkIndex = new int[5]; //Creating arraay
            chunkIndex[0] = (int)(posInWorld.X / ChunkGenerator.chunkLength); //Chunk X coordinate
            chunkIndex[1] = (int)(posInWorld.Z / ChunkGenerator.chunkWidth); //Chunk Y coordinate
            chunkIndex[2] = (int)Math.Abs(posInWorld.X % ChunkGenerator.chunkLength); //Block X coordinate
            chunkIndex[4] = (int)(posInWorld.Z % ChunkGenerator.chunkWidth); //Block Z coordinate
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
            if (index.X < 0 || index.Y < 0)
            {
                return null;
            }
            if (index.X >= WorldGenerator.chunksGenerated || index.Y >= WorldGenerator.chunksGenerated)
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
            if (chunkIndex[0] >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[1]) >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[2]) >= ChunkGenerator.chunkLength || Math.Abs(chunkIndex[4]) >= ChunkGenerator.chunkWidth || Math.Abs(chunkIndex[3]) >= ChunkGenerator.chunkHeight)
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
            if (chunkIndex[0] >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[1]) >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[2]) >= ChunkGenerator.chunkLength || Math.Abs(chunkIndex[4]) >= ChunkGenerator.chunkWidth || Math.Abs(chunkIndex[3]) >= ChunkGenerator.chunkHeight)
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
            if (chunkIndex[0] >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[1]) >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[2]) >= ChunkGenerator.chunkLength || Math.Abs(chunkIndex[4]) >= ChunkGenerator.chunkWidth || Math.Abs(chunkIndex[3]) >= ChunkGenerator.chunkHeight)
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
            if (chunkIndex[0] >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[1]) >= WorldGenerator.chunksGenerated || Math.Abs(chunkIndex[2]) >= ChunkGenerator.chunkLength || Math.Abs(chunkIndex[4]) >= ChunkGenerator.chunkWidth || Math.Abs(chunkIndex[3]) >= ChunkGenerator.chunkHeight)
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
            posInWorld += new Vector3(16 * WorldGenerator.chunksGenerated / 2, 0, 16 * WorldGenerator.chunksGenerated / 2); //Offsetting posinWorld so that its in array index terms
            return new Vector2((int)(posInWorld.X / ChunkGenerator.chunkLength), (int)(posInWorld.Z / ChunkGenerator.chunkWidth)); //Return that chunk

        }

        //Determines if the given chunkPos is a loaded chunk or not
        public bool IsChunkLoaded(Vector2 chunkPos)
        {
            //If out of bounds, return false
            if (chunkPos.X > WorldGenerator.chunksGenerated || chunkPos.Y > WorldGenerator.chunksGenerated)
            {
                return false;
            }

            if (chunkPos.X < 0 || chunkPos.Y < 0)
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
            for (int x = -radius / 2; x <= radius / 2; x++)
            {
                for (int z = -radius / 2; z <= radius / 2; z++)
                {
                    //Checking to make sure not out of bounds.
                    if (position.X + x >= WorldGenerator.chunksGenerated || position.Y + z >= WorldGenerator.chunksGenerated)
                    {
                        continue;
                    }

                    //Checking that chunk is not less than 0 in array index
                    if (position.X + x < 0 || position.Y + z < 0)
                    {
                        continue;
                    }

                    if (chunks[(int)position.X + x, (int)position.Y + z] != null && chunks[(int)position.X + x, (int)position.Y + z].chunkLoaded == false) //If a valid chunk, and is not loaded already, set it to be ready to load
                    {
                        chunksToGenHitbox.Add(chunks[(int)position.X + x, (int)position.Y + z]); //Add to chunks to hitbox load list
                        chunksToRender.Add(chunks[(int)position.X + x, (int)position.Y + z]); //Add to chunks to render load list
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
                    if (position.X + x >= WorldGenerator.chunksGenerated || position.Y + z >= WorldGenerator.chunksGenerated)
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
                    if (chunk.X + x - radius >= WorldGenerator.chunksGenerated || chunk.Y + z - radius >= WorldGenerator.chunksGenerated)
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
            if (posInWorld.Y > 0 && posInWorld.Y < ChunkGenerator.chunkHeight - 1) // If this block is above the bottom of the world and below the top of the world...
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

                if (blockID == 0 || DataManager.blockData[blockID].IsLightSource()) // If the target block is either air or a light source...
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
