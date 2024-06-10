using BlockGame.Components.Entities;
using BlockGame.Components.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Items
{
    internal class Inventory
    {
        //u short storing the block id, and its count in the inventory slot
        private ushort[,][] slots;

        private DataManager dataManager;

        public Inventory(int width, int height, DataManager dataManager) 
        {
            this.dataManager = dataManager;

            //Setting up and creating array
            slots = new ushort[width, height][];

            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    slots[x,y] = new ushort[2];
                    slots[x, y][0] = 0;
                    slots[x, y][1] = 0;
                }
            }
        }

        public void DrawFullInventory()
        {
            
        }

        public void AddItem(ushort itemID, ushort amount)
        {
            //Check if the item is in the inventory already. If so, then add it to that slot (if possible)
            //Loop from bottom bar to top, left to right
            for(int y = slots.GetLength(1) -1; y >= 0; y--)
            {
                for(int x = 0; x < slots.GetLength(0); x++)
                {
                    //If the item is found in the inventory under max count, add another count
                    if (slots[x, y][0] == itemID && slots[x,y][1] < dataManager.itemData[itemID].maxCount)
                    {
                        slots[x, y][1] += amount;
                        return;
                    }
                }
            }
            //If it hasn't returned by here, then a new slot must be filled
            for (int y = slots.GetLength(1) - 1; y >= 0; y--)
            {
                for (int x = 0; x < slots.GetLength(0); x++)
                {
                    //If the item is found in the inventory under max count, add another count
                    if (slots[x, y][0] == 0)
                    {
                        slots[x, y][0] = itemID;
                        slots[x, y][1] += amount;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Removes item from inventory slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="amount"></param>
        public void RemoveItem(Vector2 slot, ushort amount)
        {
            if (slots[(int)slot.X, (int)slot.Y][1] <= amount)
            {
                slots[(int)slot.X, (int)slot.Y][1] = 0;
                slots[(int)slot.X, (int)slot.Y][0] = 0;
            }
            else
            {
                slots[(int)slot.X, (int)slot.Y][1] -= amount;
            }
        }

        //Draw the inventory hotbar (used for player)
        //This isn't a UI component since I think it will literally only be used here so its redudant to make its own object.
        //Also it will have no interactivity (not clickable)
        public void DrawBottomBar(Vector2 origin, SpriteBatch spriteBatch, int highlightedSlot)
        {
            //Draw actual hotbar
            //Where on the atlas the hotbar is
            Rectangle atlasRect = new Rectangle(1, 1, 218, 26);
            float scale = 2;
            spriteBatch.Draw(DataManager.uiAtlas, new Rectangle((int)(origin.X - atlasRect.Width / 2 * scale), (int)(origin.Y - atlasRect.Height /2 * scale), (int)(atlasRect.Width * scale), (int)(atlasRect.Height * scale)), atlasRect, Color.White);

            //Where the first center of slot is, and the distance between each one
            Vector2 startingPos = new Vector2(13, 13) * scale; //First item is at 12,12 on the sprite
            int distanceBetweenSlots = (int)(32 * scale); //Each slot is 33 pixels away

            //Draw items
            for (int x = 0; x < slots.GetLength(0); x++)
            {
                if (slots[x, slots.GetLength(1) - 1][0] != 0)
                {
                    
                    int itemTextureWidth = dataManager.itemData[slots[x, slots.GetLength(1) - 1][0]].atlasRect.Width;

                    //Draw
                    dataManager.itemData[slots[x, slots.GetLength(1) - 1][0]].Draw(spriteBatch, new Vector2(origin.X - atlasRect.Width / 2 * scale + (int)startingPos.X + distanceBetweenSlots* x, origin.Y), scale) ;
                    
                    //Item count
                    spriteBatch.DrawString(Game1.debugFont, slots[x, slots.GetLength(1) - 1][1].ToString(), new Vector2(origin.X - atlasRect.Width / 2 * scale + (int)startingPos.X + distanceBetweenSlots * x - 5, origin.Y + 30), Color.White);
                }
                
            }

            //Draw highlighted slot
            Rectangle highlightAtlasPos = new Rectangle(1, 28, 28, 28);

            //Center of the highlighted slot
            Vector2 centerHighlight = new Vector2(origin.X - atlasRect.Width / 2 * scale + (int)startingPos.X + distanceBetweenSlots * highlightedSlot, origin.Y);

            spriteBatch.Draw(DataManager.uiAtlas, new Rectangle((int)(centerHighlight.X - highlightAtlasPos.Width / 2 * scale), (int)(centerHighlight.Y - highlightAtlasPos.Height / 2 * scale ), (int)(highlightAtlasPos.Width * scale), (int)(highlightAtlasPos.Height * scale)), highlightAtlasPos, Color.White);
        }

        public void RightClickItemSlot(Vector2 itemSlot, WorldManager world, Player player, Entity user)
        {
            //Do item right click
            dataManager.itemData[slots[(int)itemSlot.X, (int)itemSlot.Y][0]].OnRightClick(world, dataManager,player, user);
        }

        public void LeftClickItemSlot(Vector2 itemSlot, WorldManager world, Player player, Entity user)
        {
            //Do item left click
            dataManager.itemData[slots[(int)itemSlot.X, (int)itemSlot.Y][0]].OnLeftClick(world, dataManager, player, user);
        }

        public int GetHeight()
        {
            return slots.GetLength(1);
        }

        public int GetWidth()
        {
            return slots.GetLength(0);
        }
    }
}
