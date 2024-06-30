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
using BlockGame.Components.World.WorldTools;

namespace BlockGame.Components.World.ChunkTools
{
    internal class Chunk
    {
        public ChunkRenderer renderer; //Renderer for the chunk, deals with building and drawing
        public ChunkGenerator generator; //Generator for the chunk, deals with hitboxes and generating blocks

        //X,Y,Z
        //Main array that stores the block data at each position in the chunk.
        //[0] == BlockID
        //[1] == block light leel
        public ushort[,,][] blockIDs = new ushort[ChunkGenerator.chunkLength, ChunkGenerator.chunkHeight, ChunkGenerator.chunkWidth][];

        private WorldManager world; //WorldManager

        public bool chunkLoaded = false; //Is this chunk loaded?

        /// Chunks that need to be rebuilt are rebuilt in grouped time based batches to avoid lag
        /// These variables are used to code that in.
        public bool rebuildNextFrame = false;
        private int framesSinceLastRebuild = 10000; //Counter for how long since last rebuild

        public Vector3 chunkPos; //Chunk position, relative to chunks (not world position)

        public Chunk(WorldManager world, Vector3 chunkPos)
        {
            //Set variables
            this.world = world;
            this.chunkPos = chunkPos;

            this.renderer = new ChunkRenderer(this);
            this.generator = new ChunkGenerator(this, world);

            blockIDs = ChunkGenerator.SetupBlockIDArray(); //Initalize 3D array
        }

        /// <summary>
        /// Draws everything within the current chunk (faces)
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="basicEffect"></param>
        public void Draw(Camera camera, BasicEffect basicEffect)
        {
            renderer.Draw(camera, basicEffect);
        }

     
        //Turns the position within this chunk into a position in the world.
        public Vector3 PosInChunkToPosInWorld(Vector3 posInChunk)
        {
            return new Vector3(posInChunk.X + chunkPos.X * ChunkGenerator.chunkWidth, posInChunk.Y, posInChunk.Z + chunkPos.Z * ChunkGenerator.chunkLength);
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
        public void RebuildChunk()
        {
            generator.UpdateHitboxes(); //First, rebuild all the visible faces in the chunk
            renderer.BuildVertexBuffer(blockIDs, world); //Build vertex buffer using renderer
        }

        //Build the chunk (hitbox and rendering) immediately
        public void BuildChunk()
        {
            generator.BuildFacesWithColliders(blockIDs); //First, rebuild all the visible faces in the chunk
            renderer.BuildVertexBuffer(blockIDs, world); //Build vertex buffer using renderer
        }

        /// <summary>
        /// Places a block in the chunk at a given position with a given block ID. Can also be given blockNameID. Default null block if none is specified.
        /// </summary>
        /// <param name="posInChunk"></param>
        /// <param name="blockID"></param>
        /// <param name="blockNameID"></param>
        public void SetBlock(Vector3 posInChunk, ushort blockID = 0, string blockNameID = "null")
        {
            Vector3 posInWorld = PosInChunkToPosInWorld(posInChunk); //Gets this blocks position in the world

            ushort oldBlockID = world.GetBlockAtWorldIndex(posInWorld); // The block ID at the location before being changed.

            // Determines if the old block was a light source, and if so, uses DepopulateLight to remove the light source and PropagateLight to fill in the removed light if necessary.
            if (DataManager.blockData[oldBlockID].IsLightSource()) // If the old block was a light source...
            {
                world.DepopulateLight(posInWorld); // Depopulate light at the position.

                foreach (Vector3 target in world.toPropagate) // For each queued block from DepopulateLight...
                {
                    world.PropagateLight(target);
                }

                world.toPropagate.Clear(); // Clear the propagation queue.
            }

            //If out of bounds, return
            if ((int)posInChunk.X >= ChunkGenerator.chunkLength || (int)posInChunk.Z >= ChunkGenerator.chunkWidth || posInChunk.Y >= ChunkGenerator.chunkHeight || posInChunk.X < 0 || posInChunk.Y < 0 || posInChunk.Z < 0)
            {
                return;
            }

            if (!blockNameID.Equals("null")) //If a name was provided, then set that to the blockID
            {
                blockID = DataManager.blockNameID[blockNameID];
            }

            blockIDs[(int)posInChunk.X, (int)posInChunk.Y, (int)posInChunk.Z][0] = blockID; // Sets the block at posInChunk to blockID.

            if(blockID == 0)
            {
                generator.blocksBroken.Add(posInWorld);
            }

            rebuildNextFrame = true; //Sets this chunk to be rebuilt next frame, so that its vertexbuffer is rebuilt.

            //Get adjacent chunks in the positive and negative X and Z directions.
            Chunk chunkNegX = world.GetChunk(new Vector2(chunkPos.X - 1, chunkPos.Z) + new Vector2(WorldGenerator.chunksGenerated / 2, WorldGenerator.chunksGenerated / 2));
            Chunk chunkPosX = world.GetChunk(new Vector2(chunkPos.X + 1, chunkPos.Z) + new Vector2(WorldGenerator.chunksGenerated / 2, WorldGenerator.chunksGenerated / 2));
            Chunk chunkNegZ = world.GetChunk(new Vector2(chunkPos.X, chunkPos.Z - 1) + new Vector2(WorldGenerator.chunksGenerated / 2, WorldGenerator.chunksGenerated / 2));
            Chunk chunkPosZ = world.GetChunk(new Vector2(chunkPos.X, chunkPos.Z + 1) + new Vector2(WorldGenerator.chunksGenerated / 2, WorldGenerator.chunksGenerated / 2));

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
            if (DataManager.blockData[blockID].IsLightSource()) // If the new block is a light source...
            {
                SetBlockLightLevel(posInChunk, DataManager.blockData[blockID].lightEmittingFactor); // Set the block's light level to the light source's LEF.

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

        public void Update(Player player)
        {
            int[] playerPos = WorldManager.posInWorlditionToChunkIndex(player.Position); //if player is greater than render distance, then unload the chunk

            //Still gotta offset array index to chunk index ;-;
            if (chunkLoaded && Vector2.Distance(new Vector2(playerPos[0], playerPos[1]) - new Vector2(WorldGenerator.chunksGenerated / 2, WorldGenerator.chunksGenerated / 2), new Vector2(chunkPos.X, chunkPos.Z)) > Player.renderDistance)
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
            renderer.Unload();
            chunkLoaded = false; //Tell this chunk it is no longer loaded
        }

        public List<Face> GetColliders()
        {
            return generator.facesWithColliders;
        }
    }
}
