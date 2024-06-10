using BlockGame.Components.Entities;
using BlockGame.Components.Items;
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
        //Size of one block on the atlas (this is redudant but just so its not hardcoded)
        public static float blockSize = 1;

        //How many blocks x blocks the atlas it
        private static Vector2 blockCount = new Vector2(2, 3);

        //Size of one block in terms of UV coordinates
        private static Vector2 sizeOfOneBlock = new Vector2(1, 1) / blockCount;

        //What position the blocks texture is on the atlas
        public Vector2 atlasPos;
        public ushort lightEmittingFactor;
        public ushort blockID;

        //what item this block drops
        public ushort drop;



        public Block(DataManager data, ushort blockID, ushort lef)
        {
            data.blockData.Add(blockID, this);
            this.blockID = blockID;
            this.lightEmittingFactor = lef;
        }


        public Block(DataManager data, ushort blockID, Vector2 atlasPos, ushort lef, ushort drop)
        {
            data.blockData.Add(blockID, this);
            this.blockID = blockID;
            this.lightEmittingFactor = lef;

            this.drop = drop;
            this.atlasPos = atlasPos;

            if (lef > 0)
            {
                data.lightEmittingIDs.Add(blockID);
            }
        }

        public void Destroy(WorldManager world, Vector3 blockPosition)
        {
            DroppedItem.DropItem(blockPosition + new Vector3(0, 1, 0), 1, world);
            world.SetBlockAtWorldIndex(blockPosition, 0);
        }

        public bool IsLightSource()
        {
            return lightEmittingFactor > 0; 
        }

        /// <summary>
        /// On the blocks left click, what happens?
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="world"></param>
        public void OnLeftClick(Inventory inventory, WorldManager world, Vector3 blockPosition)
        {

        }

        /// <summary>
        /// On the blocks right click, what happens?
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="world"></param>
        public void OnRightClick(Inventory inventory, WorldManager world, Vector3 blockPosition)
        {

        }



        /// <summary>
        /// Below are static methods for adding face verticies to the vertex list
        /// </summary>
        /// <param name="position">3D position in the world</param>
        /// <param name="vertexList">The list the verticies should be added to</param>
        /// <param name="lineList">The list the lines of the faces should be added to (deprecated debug)</param>
        /// <param name="color">The color tint (lighting)</param>
        /// <param name="lineColor">Color of deprecated lines</param>
        /// <param name="atlasPos">The position of the texture on the atlas</param>
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
