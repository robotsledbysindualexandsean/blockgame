using BlockGame.Components.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using BlockGame.Components.Items;

namespace BlockGame.Components
{
    internal class DataManager
    {
        public static Texture2D blockAtlas;
        public static Texture2D itemAtlas;
        public static Texture2D uiAtlas;

        //Blocks
        public Dictionary<string, Block> blockData = new Dictionary<string, Block>();
        public Dictionary<string, Item> itemData = new Dictionary<string, Item>();


        public DataManager() {

        }

        public void LoadContent(ContentManager content)
        {
            //Load block
            blockAtlas = content.Load<Texture2D>("atlas");

            //Load item
            itemAtlas = content.Load<Texture2D>("itematlas");

            //ui
            uiAtlas = content.Load<Texture2D>("uiatlas");
        }

        public void LoadBlockData()
        {
            blockData.Add("0", null); //Air
            blockData.Add("1", new Block(new Vector2(0, 1), 0)); //Torn Wood
            blockData.Add("2", new Block(new Vector2(1, 1), 0)); //Wood
            blockData.Add("3", new Block(new Vector2(0, 0), 0)); //cobblestone
            blockData.Add("4", new Block(new Vector2(1, 0), 0)); //stone
            blockData.Add("5", new Block(new Vector2(0, 2), 9)); //glowstone
        }

        public void LoadItemData()
        {

        }
    }
}
