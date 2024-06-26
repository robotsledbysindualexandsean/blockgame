using BlockGame.Components.Entities;
using BlockGame.Components.World.WorldTools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Items
{
    /// <summary>
    /// Base item class. Items are "static" (one object is in the item hashmap that is used for all items of that type)
    /// </summary>
    internal class Item
    {
        public ushort itemID; //Item id
        public Rectangle atlasRect; //Where the items graphic is on the itematlas
        public int maxCount; //Max count of this item
        protected GraphicsDeviceManager graphics; //Graphicsdevice

        public Item(string nameID, ushort itemID, Rectangle atlasRect, int maxCount, GraphicsDeviceManager graphics)
        {
            DataManager.itemData.Add(itemID, this); //Add to item hashmap
            DataManager.itemDataID.Add(nameID, this); //Add to item hashmap with name

            //Set variables
            this.atlasRect = atlasRect;
            this.maxCount = maxCount;
            this.itemID = itemID;
            this.graphics = graphics;
        }

        //Constructor for the 0, nothing item
        public Item(string nameID, ushort itemID)
        {
            DataManager.itemData.Add(itemID, this);
            this.itemID = itemID;
        }

        /// <summary>
        /// Draw item at desired position (origin), and at the desired scale
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="origin">Position where the item si to be drawn center wise</param>
        /// <param name="scale">Scale of the item</param>
        public void Draw(SpriteBatch spriteBatch, Vector2 origin, float scale)
        {
            spriteBatch.Draw(DataManager.itemAtlas, new Rectangle((int)(origin.X - atlasRect.Width / 2 * scale), (int)(origin.Y - atlasRect.Height / 2 * scale), (int)(atlasRect.Width * scale), (int)(atlasRect.Height * scale)), atlasRect, Color.White);
        }

        /// <summary>
        /// What the item does on right click
        /// </summary>
        /// <param name="world"></param>
        /// <param name="player"></param>
        public virtual void OnRightClick(WorldManager world, Player player, Entity user)
        {

        }

        /// <summary>
        /// What the item does on right click
        /// This is HERE and not in BLOCK.cs because ultimately, this wont just break the block it will damage it instead, and only give the item when the
        /// block is broken.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="player"></param>
        public virtual void OnLeftClick(WorldManager world, Player player, Entity user)
        {
            //Destroy block
            DataManager.blockData[world.GetBlockAtWorldIndex(user.ClosestFace.blockPosition)].Destroy(world, user.ClosestFace.blockPosition);

        }
    }
}
