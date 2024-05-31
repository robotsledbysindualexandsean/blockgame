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
        private string[,] slots;

        public Inventory(int height, int width) 
        {
            slots = new string[height, width];
        }

        public void DrawFullInventory()
        {
            
        }

        //Draw the inventory hotbar (used for player)
        public void DrawBottomBar(Vector2 leftConerPos, SpriteBatch spriteBatch)
        {
            for(int i = 0; i < slots.GetLength(1); i++)
            {
                spriteBatch.Draw(DataManager.uiAtlas, new Vector2(leftConerPos.X + i*16, leftConerPos.Y), Color.White);
            }
        }
    }
}
