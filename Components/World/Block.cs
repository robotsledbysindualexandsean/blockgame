using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World
{
    internal class Block
    {
        public static float blockSize = 1;
        private static Vector2 blockCount = new Vector2(2, 3);
        private static Vector2 sizeOfOneBlock = new Vector2(1, 1) / blockCount;

        public Vector2 atlasPos;
        public ushort lightEmittingFactor;
        public ushort blockID;

        public Block(DataManager data, ushort blockID, ushort lef)
        {
            data.blockData.Add(blockID, this);
            this.blockID = blockID;
            this.lightEmittingFactor = lef;

            if (lef > 0)
            {
                data.lightEmittingIDs.Add(blockID);
            }
        }

        public Block(DataManager data, ushort blockID, Vector2 atlasPos, ushort lef)
        {
            data.blockData.Add(blockID, this);  
            this.blockID = blockID;
            this.atlasPos = atlasPos;
            this.lightEmittingFactor = lef;

            if (lef > 0)
            {
                data.lightEmittingIDs.Add(blockID);
            }
        }

        public bool IsLightSource()
        {
            return lightEmittingFactor > 0; 
        }

        public static void AddPosZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor, Vector2 atlasPos)
        {
            //XY Plane Z+1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            Game1.TriangleCount += 2;

            //adding lines
/*            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));*/
        }

        public static void AddNegZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor, Vector2 atlasPos)
        {
            //XY Z-1 Plane
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            Game1.TriangleCount += 2;

/*            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));*/
        }

        public static void AddPosXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor, Vector2 atlasPos)
        {
            //ZY X+1 Plane
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            Game1.TriangleCount += 2;

/*            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));*/
        }

        public static void AddNegXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor, Vector2 atlasPos)
        {
            //ZY X-1 Planed
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            Game1.TriangleCount += 2;

/*            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor));
            lineList.Add(new VertexPositionColor(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), lineColor))*/;
        }

        public static void AddPosYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor, Vector2 atlasPos)
        {
            //ZX Y+1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X *sizeOfOneBlock.X, atlasPos.Y*sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X*sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            Game1.TriangleCount += 2;



        }

        public static void AddNegYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor, Vector2 atlasPos)
        {
            //ZX Y-1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(atlasPos.X * sizeOfOneBlock.X + sizeOfOneBlock.X, atlasPos.Y * sizeOfOneBlock.Y + sizeOfOneBlock.Y)));
            Game1.TriangleCount += 2;
        }
    }
}
