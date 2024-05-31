using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components
{
    //Decrepated
    internal class Floor
    {
        //Attributes
        private int floorWidth;
        private int floorHeight; //z dir
        private VertexBuffer floorBuffer;
        private GraphicsDevice graphics;
        private Color[] floorColors = new Color[2] { Color.White, Color.Black };

        public Floor(GraphicsDevice _graphics, int width, int height)
        {
            this.graphics = _graphics;
            this.floorWidth = width;
            this.floorHeight = height;
            BuildFloorBuffer();
        }

        //Build vertex buffer
        private void BuildFloorBuffer()
        {
            List<VertexPositionColor>vertexList = new List<VertexPositionColor>();
            int counter = 0;

            //Loop to create floor
            for(int i = 0; i < floorWidth; i++)
            {
                counter++;
                for(int j = 0; j < floorHeight; j++)
                {
                    counter++;
                    //Loop to add verticies
                    foreach(VertexPositionColor vertex in FloorTile(i,j, floorColors[counter % 2]))
                    {
                        vertexList.Add(vertex);
                    }
                }
            }

            //Create buffer
            floorBuffer = new VertexBuffer(graphics, VertexPositionColor.VertexDeclaration, vertexList.Count, BufferUsage.None);
            floorBuffer.SetData<VertexPositionColor>(vertexList.ToArray());
        }

        //Define a suingle tile in floor
        private List<VertexPositionColor> FloorTile(int xOffset, int zOffset, Color tileColor)
        {
            List<VertexPositionColor> vList = new List<VertexPositionColor>();
            vList.Add(new VertexPositionColor(new Vector3(0 + xOffset, 0, 0 + zOffset), tileColor));
            vList.Add(new VertexPositionColor(new Vector3(1 + xOffset, 0, 0 + zOffset), tileColor));
            vList.Add(new VertexPositionColor(new Vector3(0 + xOffset, 0, 1 + zOffset), tileColor));
            vList.Add(new VertexPositionColor(new Vector3(1 + xOffset, 0, 0 + zOffset), tileColor));
            vList.Add(new VertexPositionColor(new Vector3(1 + xOffset, 0, 1 + zOffset), tileColor));
            vList.Add(new VertexPositionColor(new Vector3(0 + xOffset, 0, 1 + zOffset), tileColor));
            return vList;

        }

        //Draw method
        public void Draw(Camera camera, BasicEffect basicEffect)
        {
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            //Loop through and draw each vertex
            foreach(EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.SetVertexBuffer(floorBuffer);
                graphics.DrawPrimitives(PrimitiveType.TriangleList, 0, floorBuffer.VertexCount / 3); //count is vertex / 3 since each triangle has 3 vertices
            }
        }

    }
}
