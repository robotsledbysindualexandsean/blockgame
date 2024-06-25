using BlockGame.Components.Entities;
using BlockGame.Components.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World
{
    /// <summary>
    /// Class which stores block data. Each object is "static", in the sense that all blocks of that ID use that object to perform actions.
    /// </summary>
    internal class Block
    {
        public static Vector2 PixelToUV; //The "size" of one UV coordinate in terms of pixels

        public static float blockSize = 1; //Size of one block on the atlas (this is redudant but just so its not hardcoded)

        //Rectangle of sprite on spritesheet atlas, for all faces
        public Rectangle bounds;

        public Vector2 startCoordinate; //"Start" coordinates of UV. 
        public Vector2 endCoordinate; //"End" coordinates of UV

        public ushort lightEmittingFactor; //How much light this block emits

        public ushort blockID; //Blocks ID

        public ushort drop; //what item this block drops

        public Block(DataManager data, ushort blockID, ushort lef)
        {
            data.blockData.Add(blockID, this); //Add to the block hashmap

            //Set variables
            this.blockID = blockID;
            this.lightEmittingFactor = lef;
        }

        public Block(DataManager data, ushort blockID, Rectangle bounds, ushort lef, ushort drop)
        {
            data.blockData.Add(blockID, this); //Add to block hashmap

            //These coordinates are used to tell vertricies what pat of the texture they are.
            startCoordinate = new Vector2(bounds.X, bounds.Y) * PixelToUV; //Calculating UV for start coords
            endCoordinate = (new Vector2(bounds.X, bounds.Y) + new Vector2(bounds.Width, bounds.Height)) * PixelToUV;  //Calculating UV for end coords

            //Set variables
            this.blockID = blockID;
            this.lightEmittingFactor = lef;
            this.drop = drop;
            this.bounds = bounds;

            if (lef > 0) //If a light emitting block, add it to the list of light emitting blocks
            {
                data.lightEmittingIDs.Add(blockID);
            }
        }

        /// <summary>
        /// What should happen when a block is destroyed
        /// </summary>
        /// <param name="world"></param>
        /// <param name="blockPosition"></param>
        public void Destroy(WorldManager world, Vector3 blockPosition)
        {
            DroppedItem.DropItem(blockPosition + new Vector3(0, 0.5f, 0), 1, world); //Drop item entity (0.5f above to avoid clipping)

            world.SetBlockAtWorldIndex(blockPosition, 0); //Set the block to air
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
        /// 
        /// SOME EXPLANTION OF UVs
        /// Each face needs its UV coordinates for the blockatlas. Idk much about how UV coords work, but the main idea is that im taking
        /// the size of the atlas, and dividng it by the amount of blocks, to get a UV coordinate for one "block" unit. Then, I apply that to the face.
        /// TO DO:
        /// Multiple face deisgns
        /// smaller block sizes
        /// animation
        /// </summary>
        /// <param name="position">3D position in the world</param>
        /// <param name="vertexList">The list the verticies should be added to</param>
        /// <param name="lineList">The list the lines of the faces should be added to (deprecated debug)</param>
        /// <param name="color">The color tint (lighting)</param>
        /// <param name="lineColor">Color of deprecated lines</param>
        /// <param name="atlasPos">The position of the texture on the atlas</param>
        public void AddPosZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor)
        {
            //XY Plane Z+1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
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

        public void AddNegZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor )
        {
            //XY Z-1 Plane
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
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

        public void AddPosXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor)
        {
            //ZY X+1 Plane
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
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

        public void AddNegXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor)
        {
            //ZY X-1 Planed
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
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

        public void AddPosYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor)
        {
            //ZX Y+1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            Game1.TriangleCount += 2;



        }

        public void AddNegYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, List<VertexPositionColor> lineList, Color color, Color lineColor)
        {
            //ZX Y-1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            Game1.TriangleCount += 2;
        }
    }
}
