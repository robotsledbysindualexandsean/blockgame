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
using BlockGame.Components.Entities;

namespace BlockGame.Components
{
    /// <summary>
    /// The Data Manager loads content at the start of the game and contains hashmaps for blocks, items, etc.
    /// All game data is sourced from these hashmaps.
    /// </summary>
    internal class DataManager
    {
        //Atlases for textures
        public static Texture2D blockAtlas;
        public static Texture2D itemAtlas;
        public static Texture2D uiAtlas;

        //Hashmaps
        public Dictionary<ushort, Block> blockData = new Dictionary<ushort, Block>();
        public Dictionary<ushort, Item> itemData = new Dictionary<ushort, Item>();

        public List<ushort> lightEmittingIDs = new List<ushort>();

        public DataManager() {

        }

        /// <summary>
        /// Loading all the atlases.
        /// </summary>
        /// <param name="content"></param>
        public void LoadContent(ContentManager content)
        {
            //Load block
            blockAtlas = content.Load<Texture2D>("atlas");

            //Load item
            itemAtlas = content.Load<Texture2D>("itematlas");

            //ui
            uiAtlas = content.Load<Texture2D>("uiatlas");
        }

        /// <summary>
        /// Loading the block hashmap.
        /// </summary>
        public void LoadBlockData()
        {
            //DataManager data, ushort blockID, Vector2 atlasPos, ushort lef, ushort drop, 
            new Block(this, 0, 0); //Air
            new Block(this, 1, new Vector2(0, 1), 0, 1); //Torn Wood
            new Block(this, 2, new Vector2(1, 1), 0, 1); //Wood
            new Block(this, 3, new Vector2(0, 0), 0, 1); //cobblestone
            new Block(this, 4, new Vector2(1, 0), 0, 1); //stone
            new Block(this, 5, new Vector2(0, 2), 15, 1); //glowstone

        }

        /// <summary>
        /// Loading the item hashmap.
        /// </summary>
        public void LoadItemData()
        {
            new Item(this, 0); // Nothing
            new BlockItem(this, 1, new Rectangle(1, 1, 16, 16), 999, Game1._graphics);
            new BombItem(this, 2, new Rectangle(1, 1, 16, 16), 5, Game1._graphics);
        }
    }
}
