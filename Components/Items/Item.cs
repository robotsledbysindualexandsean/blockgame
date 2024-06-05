using BlockGame.Components.Entity;
using BlockGame.Components.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Items
{
    internal class Item
    {
        //Where on the atlas this is (x,y,length,width)
        protected ushort itemID;
        public Rectangle atlasRect;
        public int maxCount;

        //Items that are stored in item id hashmap, storing their id, where theyre texture is on the atlas, and theyre max count.
        public Item(ushort itemID, Rectangle atlasRect, int maxCount)
        {
            this.atlasRect = atlasRect;
            this.maxCount = maxCount;
            this.itemID = itemID;
        }

        public Item(ushort itemID)
        {
            this.itemID = itemID;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 origin, int scale)
        {
            spriteBatch.Draw(DataManager.itemAtlas, new Rectangle((int)origin.X - atlasRect.Width / 2 * scale, (int)origin.Y - atlasRect.Height / 2 * scale, atlasRect.Width * scale, atlasRect.Height * scale), atlasRect, Color.White);
        }

        /// <summary>
        /// What the item does on right click
        /// </summary>
        /// <param name="world"></param>
        /// <param name="player">T</param>
        public virtual void OnRightClick(WorldManager world, DataManager dataManager, Player player)
        {

        }

        /// <summary>
        /// What the item does on right click
        /// </summary>
        /// <param name="world"></param>
        /// <param name="player"></param>
        public void OnLeftClick(WorldManager world, DataManager dataManager, Player player)
        {
            //Add the drop to the players inventory
            player.Inventory.AddItem(dataManager.blockData[world.GetBlockAtWorldIndex(player.ClosestFace.blockPosition)].drop);
            world.SetBlockAtWorldIndex(player.ClosestFace.blockPosition, 0);
        }
    }
}
