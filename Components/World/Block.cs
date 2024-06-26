using BlockGame.Components.Entities;
using BlockGame.Components.Items;
using BlockGame.Components.World.WorldTools;
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
        public static Vector2 BlockToUV; //The "size" of one UV coordinate in terms of blocks

        public static float blockSize = 1; //Size of one block on the atlas (this is redudant but just so its not hardcoded)

        public string texture; //Texture ID of what is to be drawn

        public ushort lightEmittingFactor; //How much light this block emits

        private Vector2 startCoordinate; //Where UV starts for thsi texture
        private Vector2 endCoordinate; //Where UV ends for this texture

        public ushort blockID; //Blocks ID

        public ushort drop; //what item this block drops

        public Vector3 dimensions; //Actual dimensions of the block. For most blocks, this is 1,1,1

        public bool transparent = true; //should this block be culled?

        public bool collide = true; //Is this block collidable?

        public Block(string nameID, ushort blockID, ushort lef, bool transparent, bool collide)
        {
            DataManager.blockData.Add(blockID, this); //Add to the block hashmap
            DataManager.blockDataID.Add(nameID, this); //Add to block hashmap, with name keys

            //Set variables
            this.blockID = blockID;
            this.lightEmittingFactor = lef;
            this.transparent = transparent;
            this.collide = collide;
        }

        public Block(string nameID, ushort blockID, string texture, ushort lef, ushort drop, Vector3 dimensions, bool transparent, bool collide)
        {
            DataManager.blockData.Add(blockID, this); //Add to block hashmap

            //These coordinates are used to tell vertricies what pat of the texture they are.
            Vector2 atlasPosition = DataManager.blockTexturePositions[texture];
            startCoordinate = atlasPosition * BlockToUV; //Calculating UV for start coords
            endCoordinate = atlasPosition * BlockToUV + BlockToUV;  //Calculating UV for end coords

            //Set variables
            this.blockID = blockID;
            this.lightEmittingFactor = lef;
            this.drop = drop;
            this.texture = texture;
            this.dimensions = dimensions;
            this.transparent = transparent;
            this.collide = collide;

            if (lef > 0) //If a light emitting block, add it to the list of light emitting blocks
            {
                DataManager.lightEmittingIDs.Add(blockID);
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
        public void AddPosZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color)
        {
            //XY Plane Z+1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + dimensions.Z / 2 * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + dimensions.Z / 2 * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + dimensions.Z / 2 * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + dimensions.Z / 2 * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z + dimensions.Z / 2 * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z + dimensions.Z / 2 * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));

        }

        public void AddNegZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color)
        {
            //XY Z-1 Plane
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - dimensions.Z / 2 * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - dimensions.Z / 2 * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - dimensions.Z / 2 * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - dimensions.Z / 2 * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + 0.5f * blockSize, position.Z - dimensions.Z / 2 * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - 0.5f * blockSize, position.Z - dimensions.Z / 2 * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));

        }

        public void AddPosXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color)
        {
            //ZY X+1 Plane
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
        }

        public void AddNegXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color)
        {
            //ZY X-1 Planed
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 * blockSize, position.Y + 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 * blockSize, position.Y - 0.5f * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 * blockSize, position.Y + 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 * blockSize, position.Y - 0.5f * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
        }

        public void AddPosYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color)
        {
            //ZX Y+1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + dimensions.Y / 2 * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + dimensions.Y / 2 * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + dimensions.Y / 2 * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + dimensions.Y / 2 * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y + dimensions.Y / 2 * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y + dimensions.Y / 2 * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));

        }

        public void AddNegYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color)
        {
            //ZX Y-1
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - dimensions.Y / 2 * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - dimensions.Y / 2 * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - dimensions.Y / 2 * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - dimensions.Y / 2 * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f * blockSize, position.Y - dimensions.Y / 2 * blockSize, position.Z - 0.5f * blockSize), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f * blockSize, position.Y - dimensions.Y / 2 * blockSize, position.Z + 0.5f * blockSize), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
        }
    }
}
