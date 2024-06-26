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
using Liru3D.Models;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;

namespace BlockGame.Components
{
    /// <summary>
    /// The Data Manager loads content at the start of the game and contains hashmaps for blocks, items, etc.
    /// All game data is sourced from these hashmaps.
    /// </summary>
    static class DataManager
    {
        //Atlases for textures:
        public static Texture2D blockAtlas; // Atlas for block textures.
        public static Texture2D itemAtlas; // Atlas for item sprites.
        public static Texture2D uiAtlas; // Atlas for UI elements.

        //Hashmaps:
        public static Dictionary<string, Vector2> blockTexturePositions = new Dictionary<string, Vector2>(); //Vector2 positions of all blocks in the atlas by name
        public static Dictionary<string, Block> blockDataID = new Dictionary<string, Block>(); //All block objects by keyed by a string, not id number
        public static Dictionary<ushort, Block> blockData = new Dictionary<ushort, Block>(); // All block objects.

        public static Dictionary<string, Item> itemDataID = new Dictionary<string, Item>(); //All block objects keyed by a string, not id number
        public static Dictionary<ushort, Item> itemData = new Dictionary<ushort, Item>(); // All item objects.

        public static Dictionary<string, SkinnedModel> models = new Dictionary<string, SkinnedModel>(); // All models that are laoded.
        public static Dictionary<string, Texture2D> modelTextures = new Dictionary<string, Texture2D>(); // All model textures that are loaded.

        public static List<ushort> lightEmittingIDs = new List<ushort>(); // List of block IDs that emit light.
        public static ushort maxLightLevel = 15;

        /// <summary>
        /// Loading all the atlases.
        /// </summary>
        /// <param name="content"></param>
        public static void LoadContent(ContentManager content)
        {
            GenerateBlockAtlas(content);
            itemAtlas = content.Load<Texture2D>("itematlas"); // Load item atlas.
            uiAtlas = content.Load<Texture2D>("uiatlas"); // Load UI atlas.
            LoadModels(content); // Load models.
        }

        /// <summary>
        /// Loading the block hashmap.
        /// </summary>
        public static void LoadBlockData()
        {
            //Rectangles are (startPos.X, startPos.Y, Width, Height)
            new Block(nameID: "air",
                blockID: 0,
                lef: 0,
                transparent: true,
                collide: false);

            new Block(nameID: "torn_wood",
                blockID: 1,
                texture: "wood_log_side",
                lef: 0,
                drop: 1,
                dimensions: new Vector3(1, 1, 1),
                transparent: false,
                collide: true);

            new Block(nameID: "wood",
                blockID: 2,
                texture: "wood_log_top",
                lef: 0,
                drop: 1,
                dimensions: new Vector3(1, 1, 1),
                transparent: false,
                collide: true);

            new Block(nameID: "cobblestone",
                blockID: 3,
                texture: "cobblestone",
                lef: 0,
                drop: 1,
                dimensions: new Vector3(1, 1, 1),
                transparent: false,
                collide: true);


            new Block(nameID: "stone",
                blockID: 4,
                texture: "sparse_cobblestone",
                lef: 0,
                drop: 1,
                dimensions: new Vector3(1, 1, 1),
                transparent: false,
                collide: true);

            new Block(nameID: "glowstone",
                blockID: 5,
                texture: "grass_top",
                lef: 15,
                drop: 1,
                dimensions: new Vector3(1f, 1, 1f),
                transparent: false,
                collide: false);
        }

        /// <summary>
        /// Loading the item hashmap.
        /// </summary>
        public static void LoadItemData()
        {
            new Item("air", 0); // Nothing
            new BlockItem("wood_block", 1, new Rectangle(1, 1, 16, 16), 999, Game1._graphics);
            new BombItem("bomb", 2, new Rectangle(18, 1, 7, 10), 5, Game1._graphics);
        }

        /// <summary>
        /// Models in the game that can be used.
        /// </summary>
        public static void LoadModels(ContentManager Content)
        {
            models.Add("test", Content.Load<SkinnedModel>("MrFriendlyKindaFinished3"));
            modelTextures.Add("test", Content.Load<Texture2D>("friend_diffuse"));
            models.Add("bomb", Content.Load<SkinnedModel>("Bomb"));
            modelTextures.Add("bomb", Content.Load<Texture2D>("Bomb_Diffuse"));
        }

        /// <summary>
        /// Method to create the block atlas using textures in the block folders. Stores the positions of each block in a reference dictionary.
        /// </summary>
        private static void GenerateBlockAtlas(ContentManager content)
        {
            DirectoryInfo dir = new DirectoryInfo(content.RootDirectory + "\\" + "blocks"); //Reference content folder
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(); //If no directory exists, error
            }

            FileInfo[] fileInfo = dir.GetFiles("*.*"); //Get all the file data in the blocks folder

            int blockSize = 16;
            int w = (int)Math.Ceiling(Math.Sqrt(fileInfo.Length)); //width in blocks for txture
            int h = (int)Math.Ceiling(Math.Sqrt(fileInfo.Length)); //height of block for texture

            blockAtlas = new Texture2D(Game1._graphics.GraphicsDevice, w * blockSize, h * blockSize); //Create a blank 2D square texture atlas

            int fileCounter = 0; //counter for how many files we've iterated

            ///This is going to iterate every texture in the folder, add it to the atlas, and reference it's position in a dictionary.
            for (int y = 0; y < h; y++) //Looping through XY to create 2D atlas
            {
                for (int x = 0; x < w; x++)
                {
                    //Make sure there are still files to add to atlas
                    if (fileCounter >= fileInfo.Length)
                    {
                        break;
                    }

                    string name = Path.GetFileNameWithoutExtension(fileInfo[fileCounter].Name); //get texture name
                    Texture2D texture = content.Load<Texture2D>("blocks\\" + name); //Load the texture

                    byte[] textureData = new byte[texture.Width * texture.Height * 4]; //Create array to store texture data in bytes
                    texture.GetData<byte>(textureData); //Get the data from the texture in terms of colors

                    blockAtlas.SetData(0, 0, new Rectangle(blockSize * x, blockSize * y, blockSize, blockSize), textureData, 0, blockSize * blockSize * 4); //Set the data in the atlas
                    blockTexturePositions.Add(name, new Vector2(x, y)); //Add this position to the dictionary for reference by name

                    fileCounter++; //increment counter
                }
            }

            Block.BlockToUV = Vector2.One / new Vector2(w, h); //UVs range from 0-1. Set PixelToUV by dividing 1 by block count X,Y.
        }
    }
}
