using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockGame.Components.World.WorldTools;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;

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
                Game1._graphics.GraphicsDevice.BlendState = BlendState.Opaque; //Dont do anything to alpha pixels (this is donw in shader)
                basicEffect.Texture = DataManager.blockAtlas; //Setting the texture to be the block atlas.

               //Use the shader to render in the blocks (the shader is used to remove alpha values on textures)

               Game1._transparentShader.Parameters["WorldViewProjection"].SetValue(basicEffect.World * basicEffect.View * basicEffect.Projection); //give shader matrix
               Game1._transparentShader.Parameters["Texture"].SetValue(basicEffect.Texture); //give shader texture

                //Loop through and draw each vertex
                foreach (EffectPass pass in Game1._transparentShader.CurrentTechnique.Passes)
                {
                    Game1._graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer); //Set vertex buffer to the chunk data

                    //Setting the texture to be the block atlas.
                    pass.Apply();

                    Game1._graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3); //draw solid blocks up to transparent index

                }

                //OLD REGULAR BASIC EFFECT RENDERING (deprecated) (keeping since idk abotu shaders so backup)
                /*                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    Game1._graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer); //Set vertex buffer to the chunk data


                    pass.Apply();

                    Game1._graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3); //draw all blocks
                }*/

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

            List<Vector3> transparentBlocks = GetTransparentBlocks();

            facesToRender = new List<Face>(); //Reset visible face list

            Vector3[] directions = { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ }; //Array of all directions to check

            //Add all the faces around the empty blocks, as these are all visible blocks.
            foreach (Vector3 block in transparentBlocks)
            {
                foreach(Vector3 direction in directions) //Check all directions and add faces if needed
                {
                    Vector3 targetBlock = block + direction; //Reference to direction
                    ushort blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                    if(blockID != 0) //If the block is not air
                    {
                        //Get the air block, and set the color value to be dependant on that.
                        Vector3 adjacentBlock = block;
                        int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                        Color color = new Color(colorValue, colorValue, colorValue);

                        DataManager.blockData[blockID].AddFaceToVertexList(targetBlock, -direction, vertexList, color); //Add to vertex list.
                    }
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
        /// Returns a list of transparent blocks in the chunk. Not to be confused with transparent faces, which are to be rendered.
        /// </summary>
        /// <returns>List of transparent blocks</returns>
        private List<Vector3> GetTransparentBlocks()
        {
            //Generate a list of all the blocks that are empty. Visible blocks are the blocks around "empty" blocks
            List<Vector3> transparentBlocks = new List<Vector3>();

            for (int y = 0; y < ChunkGenerator.chunkHeight; y++)
            {
                for (int x = 0; x < ChunkGenerator.chunkLength; x++)
                {
                    for (int z = 0; z < ChunkGenerator.chunkWidth; z++)
                    {
                        if (DataManager.blockData[chunk.blockIDs[x, y, z][0]].transparent) //If the block is empty or is transparent, then add it to the list
                        {
                            transparentBlocks.Add(new Vector3(chunk.chunkPos.X * 16 + x, y, chunk.chunkPos.Z * 16 + z)); //x,y,z must be x16xchunkPos to convert to world coordinates
                        }
                    }
                }
            }
            return transparentBlocks;
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
