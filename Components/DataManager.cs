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
        public Dictionary<ushort, Block> blockData = new Dictionary<ushort, Block>();
        public Dictionary<ushort, Item> itemData = new Dictionary<ushort, Item>();

        public List<ushort> lightEmittingIDs = new List<ushort>();
        public List<Vector3> lightEmittingPos = new List<Vector3>();

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
            new Block(this, 0, 0); //Air
            new Block(this, 1, new Vector2(0, 1), 0); //Torn Wood
            new Block(this, 2, new Vector2(1, 1), 0); //Wood
            new Block(this, 3, new Vector2(0, 0), 1); //cobblestone
            new Block(this, 4, new Vector2(1, 0), 0); //stone
            new Block(this, 5, new Vector2(0, 2), 10); //glowstone
        }

        public void LoadItemData()
        {

        }
    }
}
