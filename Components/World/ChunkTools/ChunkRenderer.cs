using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockGame.Components.World.WorldTools;

namespace BlockGame.Components.World.ChunkTools
{
    /// <summary>
    /// This class is in charge of the rendering of each chunk specifically.
    /// </summary>
    internal class ChunkRenderer
    {
        Chunk chunk; //Refernece to chunk this is for
        public VertexBuffer vertexBuffer; //Main vertexbuffer
        public VertexBuffer debugBuffer; //for rendering hitbox debug
        public static int defaultLightHue = 50; //default lighting value for blocks
        private List<Face> facesToRender = new List<Face>(); //Faces which need to be rendered by the chunk

        public ChunkRenderer(Chunk chunk)
        {
            this.chunk = chunk;
        }

        /// <summary>
        /// Draws everything within the current chunk (faces)
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="basicEffect"></param>
        public void Draw(Camera camera, BasicEffect basicEffect)
        {
            if (vertexBuffer != null)
            {
                //Setting up basic effect
                basicEffect.VertexColorEnabled = true; //Turn on colors
                basicEffect.View = camera.View; //Set view matrix
                basicEffect.Projection = camera.Projection; //Set projection matrix
                basicEffect.World = Matrix.Identity; //No world matrix (no transformations needed)
                basicEffect.TextureEnabled = true; //Turn on texturing
                basicEffect.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap; //Pixel perfect rendering

                //Loop through and draw each vertex
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    Game1._graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer); //Set vertex buffer to the chunk data

                    //Setting the texture to be the block atlas.
                    basicEffect.Texture = DataManager.blockAtlas;
                    pass.Apply();

                    Game1._graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3); //draw solid blocks

                }
            }
            //Debug rendering of face hitboxes
            if (Game1.debug && debugBuffer != null)
            {
                //Loop through and draw each line
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Game1._graphics.GraphicsDevice.SetVertexBuffer(debugBuffer);
                    Game1._graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, debugBuffer.VertexCount / 2);
                }
            }
        }

        //Builds the vertex buffer for the chunk, which displays all the blocks in the chunk.
        public void BuildVertexBuffer(ushort[,,][] blockIDs, WorldManager world)
        {

            List<VertexPositionColorTexture> vertexList = new List<VertexPositionColorTexture>(); //List of verticies which will be loaded into the vertexbuffer

            //Generate a list of all the blocks that are empty. Visible blocks are the blocks around "empty" blocks
            List<Vector3> transparentBlocks = new List<Vector3>();

            for (int y = 0; y < ChunkGenerator.chunkHeight; y++)
            {
                for (int x = 0; x < ChunkGenerator.chunkLength; x++)
                {
                    for (int z = 0; z < ChunkGenerator.chunkWidth; z++)
                    {
                        if (DataManager.blockData[blockIDs[x, y, z][0]].transparent) //If the block is empty or is transparent, then add it to the list
                        {
                            transparentBlocks.Add(new Vector3(chunk.chunkPos.X * 16 + x, y, chunk.chunkPos.Z * 16 + z)); //x,y,z must be x16xchunkPos to convert to world coordinates
                        }
                    }
                }
            }

            facesToRender = new List<Face>(); //Reset visible face list

            //Add all the faces around the empty blocks, as these are all visible blocks.
            foreach (Vector3 block in transparentBlocks)
            {
                Vector3 targetBlock; //Variable for target block which is changed as faces are checked
                ushort blockID; //Variable for blockID whic his changed as faces are checked

                //Z+1
                targetBlock = new Vector3(block.X, block.Y, block.Z + 1); //Reference to direction
                blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                if (blockID != 0) //If this block is not air...
                {
                    //Get the air block, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = block;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);


                    //Add the verticies of this face to the vertexBuffer
                    DataManager.blockData[blockID].AddNegZVerticiesPos(targetBlock, vertexList, color);
                }
                //Z-1
                targetBlock = new Vector3(block.X, block.Y, block.Z - 1); //Reference to direction
                blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                if (blockID != 0) //If this block is not air...
                {
                    //Get the air block, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = block;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    DataManager.blockData[blockID].AddPosZVerticiesPos(targetBlock, vertexList, color);

                }

                //x+1
                targetBlock = new Vector3(block.X+1, block.Y, block.Z); //Reference to direction
                blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                if (blockID != 0) //If this block is not air...
                {
                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = block;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    DataManager.blockData[blockID].AddNegXVerticiesPos(targetBlock, vertexList, color);
                }

                //x-1
                targetBlock = new Vector3(block.X - 1, block.Y, block.Z); //Reference to direction
                blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                if (blockID != 0) //If this block is not air...
                {
                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = block;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    DataManager.blockData[blockID].AddPosXVerticiesPos(targetBlock, vertexList, color);
                }
                //y+1
                targetBlock = new Vector3(block.X, block.Y+1, block.Z); //Reference to direction
                blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                if (blockID != 0) //If this block is not air...
                {
                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = block;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    DataManager.blockData[blockID].AddNegYVerticiesPos(targetBlock, vertexList, color);

                }
                //y-1
                targetBlock = new Vector3(block.X, block.Y - 1, block.Z); //Reference to direction
                blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                if (blockID != 0) //If this block is not air...
                {
                    //Get the air block in front of it, and set the color value to be dependant on that.
                    Vector3 adjacentBlock = block;
                    int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                    Color color = new Color(colorValue, colorValue, colorValue);

                    //Add the verticies of this face to the vertexBuffer
                    DataManager.blockData[blockID].AddPosYVerticiesPos(targetBlock, vertexList, color);
                }
            }

            //If the vertexList has any verticies, build it.
            if (vertexList.Count > 0)
            {
                vertexBuffer = new VertexBuffer(Game1._graphics.GraphicsDevice, typeof(VertexPositionColorTexture), vertexList.Count, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertexList.ToArray());
            }

            facesToRender.Clear(); //Clear list to free up memory

        }

        /// <summary>
        /// Free up data when the chunk is unloaded
        /// </summary>
        public void Unload()
        {
            if (vertexBuffer != null)
            {
                vertexBuffer.Dispose(); //Free up VB
                vertexBuffer = null; //reset reference
            }
        }

        public void BuildHitboxes()
        {
            List<VertexPositionColor> debugList = new List<VertexPositionColor>();
            foreach (Face face in chunk.GetColliders())
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
                debugBuffer = new VertexBuffer(Game1._graphics.GraphicsDevice, typeof(VertexPositionColor), debugList.Count, BufferUsage.WriteOnly);
                debugBuffer.SetData(debugList.ToArray());
            }
        }

    }
}
