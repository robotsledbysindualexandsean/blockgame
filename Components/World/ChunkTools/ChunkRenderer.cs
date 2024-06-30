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
        public DynamicVertexBuffer vertexBuffer; //Main vertexbuffer
        public DynamicVertexBuffer debugBuffer; //for rendering hitbox debug
        public static int defaultLightHue = 50; //default lighting value for blocks

        public DynamicVertexBuffer animationBuffer; //vertex buffer used for animated blocks
        public List<Vector3[]> animatedFaces = new List<Vector3[]>(); //List of animation block positions [0] and their facing direction [1]

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
            //Setting up basic effect
            basicEffect.VertexColorEnabled = true; //Turn on colors
            basicEffect.View = camera.View; //Set view matrix
            basicEffect.Projection = camera.Projection; //Set projection matrix
            basicEffect.World = Matrix.Identity; //No world matrix (no transformations needed)
            basicEffect.TextureEnabled = true; //Turn on texturing
            basicEffect.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap; //Pixel perfect rendering
            Game1._graphics.GraphicsDevice.BlendState = BlendState.Opaque; //Dont do anything to alpha pixels (this is donw in shader)
            basicEffect.Texture = DataManager.blockAtlas; //Setting the texture to be the block atlas.

            if (vertexBuffer != null)
            {
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

            }
            if (animationBuffer != null) //Build animation buffer
            {
                //Use the shader to render in the blocks (the shader is used to remove alpha values on textures)
                Game1._transparentShader.Parameters["WorldViewProjection"].SetValue(basicEffect.World * basicEffect.View * basicEffect.Projection); //give shader matrix
                Game1._transparentShader.Parameters["Texture"].SetValue(basicEffect.Texture); //give shader texture

                //Loop through and draw each vertex
                foreach (EffectPass pass in Game1._transparentShader.CurrentTechnique.Passes)
                {
                    Game1._graphics.GraphicsDevice.SetVertexBuffer(animationBuffer); //Set vertex buffer to the chunk data

                    //Setting the texture to be the block atlas.
                    pass.Apply();

                    Game1._graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, animationBuffer.VertexCount / 3); //draw solid blocks up to transparent index

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

        /// <summary>
        /// Builds only the animation vertex buffer
        /// </summary>
        /// <param name="blockIDs"></param>
        /// <param name="world"></param>
        public void BuildAnimationBuffer(ushort[,,][] blockIDs, WorldManager world)
        {
            List<VertexPositionColorTexture> animationList = new List<VertexPositionColorTexture>(); //List of verticies which will be loaded into the animationBuffer

            //animationFaces holds all the faces in the chunk that are animated. Remember [0] holds the position, and [1] holds the faces normal.
            foreach (Vector3[] face in animatedFaces) //For each fae, render it
            {

                //Get the air block, and set the color value to be dependant on that.
                Vector3 adjacentBlock = face[0] + face[1]; //Get air block in front of it
                int colorValue = world.GetBlockLightLevelAtWorldIndex(adjacentBlock) * 17 + defaultLightHue;
                Color color = new Color(colorValue, colorValue, colorValue);

                ushort blockID = world.GetBlockAtWorldIndex(face[0]); //get blocks iD

                DataManager.blockData[blockID].AddFaceToVertexList(face[0], face[1], animationList, color); //Add to vertex list.
            }

            //Build animation vertex buffer
            if (animationList.Count > 0)
            {
                animationBuffer = new DynamicVertexBuffer(Game1._graphics.GraphicsDevice, typeof(VertexPositionColorTexture), animationList.Count, BufferUsage.WriteOnly);
                animationBuffer.SetData(animationList.ToArray());
            }

        }

        //Builds the vertex buffer for the chunk, which displays all the blocks in the chunk.
        public void BuildVertexBuffer(ushort[,,][] blockIDs, WorldManager world)
        {
            List<VertexPositionColorTexture> vertexList = new List<VertexPositionColorTexture>(); //List of verticies which will be loaded into the vertexbuffer
            animatedFaces.Clear(); //clear animated face list

            List<Vector3> transparentBlocks = GetTransparentBlocks();

            Vector3[] directions = { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ }; //Array of all directions to check

            //Add all the faces around the empty blocks, as these are all visible blocks.
            foreach (Vector3 block in transparentBlocks)
            {
                foreach(Vector3 direction in directions) //Check all directions and add faces if needed
                {
                    Vector3 targetBlock = block + direction; //Reference to direction
                    ushort blockID = world.GetBlockAtWorldIndex(targetBlock); //Get block ID

                    //If the face has animation, then add it to animation buffer
                    if (blockID != 0 && DataManager.blockData[blockID].HasAnimationInDirection(-direction)) //Face is facing the opposite direction
                    {
                        animatedFaces.Add(new Vector3[] { targetBlock, -direction }); //Add this face to the lsit of faces which are animated
                        continue; //dont render this face yet
                    } 

                    //If the block is not animated, add it to regualr buffer
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
                vertexBuffer = new DynamicVertexBuffer(Game1._graphics.GraphicsDevice, typeof(VertexPositionColorTexture), vertexList.Count, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertexList.ToArray());
            }
            vertexList.Clear(); //free up memory

            BuildAnimationBuffer(blockIDs, world); //Build animation vertex buffer
            BuildHitboxes();

        }
        
        //Returns if the chunk has animated blocks
        public bool ContainsAnimatedBlocks()
        {
            if(animatedFaces.Count > 0)
            {
                return true;
            }
            return false;
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
                debugBuffer = new DynamicVertexBuffer(Game1._graphics.GraphicsDevice, typeof(VertexPositionColor), debugList.Count, BufferUsage.WriteOnly);
                debugBuffer.SetData(debugList.ToArray());
            }
        }

    }
}
