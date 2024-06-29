using Assimp.Unmanaged;
using BlockGame.Components.Entities;
using BlockGame.Components.Items;
using BlockGame.Components.World.ChunkTools;
using BlockGame.Components.World.WorldTools;
using CppNet;
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
        private static Vector3[] directions = { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitX, -Vector3.UnitY, -Vector3.UnitZ }; //Array storing all the face directions so they can be looped easily in the code

        public static Vector2 BlockToUV; //The "size" of one UV coordinate in terms of blocks

        public static float blockSize = 1; //Size of one block on the atlas (this is redudant but just so its not hardcoded)

        private static float glowDistance = 0.01f; //How mcuh farther from the block the "glow" is (this is just a visual technuical thing)

        private int glowLightLevel; //glow light level for the block

        public ushort lightEmittingFactor; //How much light this block emits

        public ushort blockID; //Blocks ID

        public ushort drop; //what item this block drops

        public Vector3 dimensions; //Actual dimensions of the block. For most blocks, this is 1,1,1

        public bool transparent = true; //should this block be culled?

        public bool collide = true; //Is this block collidable?

        /// <summary>
        /// This is a dictionary that stores the FaceData objects for this block type. FaceData simply stores info like texture, animation, counter, etc. This is because there are 6 faces and to store all this here would look very messy.
        /// </summary>
        private Dictionary<Vector3, FaceData> faces = new Dictionary<Vector3, FaceData>();

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

        /*
         Main constructor for blocks. This has a LOT of parameters, but most are optional. Let's go over them quickly:
        nameID = name of the block that is put into the hashmap with nameIDs rather htan block number IDs
        blockID = block number ID
        dimensions = the dimensions of thi block. 1,1,1 is the base size of a block.
        lef = light emitting factor
        drop = what item this block drops
        glowLightLevel = what light level does this objects glow display at
        transparent & collide = does this block have collision with enttiies? and does the block feature transparent parts and requires seperate rendering

        The rest are all textures:
        textureID, glowTextureID, animationTextureID, glowAnimationTextureID = all generic textures that are applied to all faces. These can be overwritten by specifying face textures as well.
        Directional textures = these are the textures/animatiosn for a specific face. They do not need to be specified.

        TLDR: base "textureIDs" are applied first to all sides of the block. You can overrwrite these by specifying a directional texture ID. You can overwrite THOSE by adding an animationID (you dont need a textureID or directionalID to have animationID)
        This goes for glows too. This is just for flexibiliy so you can add any block orietnation combo and the game will render it.
         */
        public Block(
            string nameID, 
            ushort blockID,
            Vector3 dimensions,
            string textureID = "null", 
            ushort lef = 0, 
            ushort drop = 0,
            int glowLightLevel = 0,
            bool transparent = false,
            bool collide = true,
            string POSXTextureID = "null",
            string NEGXTextureID = "null",
            string POSYTextureID = "null",
            string NEGYTextureID = "null",
            string POSZTextureID = "null",
            string NEGZTextureID = "null",
            string glowTextureID = "null",
            string POSXGlowTextureID = "null",
            string NEGXGlowTextureID = "null",
            string POSYGlowTextureID = "null",
            string NEGYGlowTextureID = "null",
            string POSZGlowTextureID = "null",
            string NEGZGlowTextureID = "null",
            string animationTextureID = "null",
            string POSXAnimationTextureID = "null",
            string NEGXAnimationTextureID = "null",
            string POSYAnimationTextureID = "null",
            string NEGYAnimationTextureID = "null",
            string POSZAnimationTextureID = "null",
            string NEGZAnimationTextureID = "null",
            string glowAnimationTextureID = "null",
            string POSXGlowAnimationTextureID = "null",
            string NEGXGlowAnimationTextureID = "null",
            string POSYGlowAnimationTextureID = "null",
            string NEGYGlowAnimationTextureID = "null",
            string POSZGlowAnimationTextureID = "null",
            string NEGZGlowAnimationTextureID = "null"
            )
        {
            DataManager.blockData.Add(blockID, this); //Add to block hashmap

            InitializeDictionary(); //Initalize dictionary

            //Call step by step methods to initalize the textures. First the base faces are initalized, then the glows, and then the animations. The animations will overrwrite the textures and glows if they exist.
            InitalizeTextures(textureID, POSXTextureID, NEGXTextureID, POSYTextureID, NEGYTextureID, POSZTextureID, NEGZTextureID); //Initalize base textures
            InitalizeGlows(glowTextureID, POSXGlowTextureID, NEGXGlowTextureID, POSYGlowTextureID, NEGYGlowTextureID, POSZGlowTextureID, NEGZGlowTextureID); //Initialize glow textures
            InitalizeAnimationTextures(animationTextureID, POSXAnimationTextureID, NEGXAnimationTextureID, POSYAnimationTextureID, NEGYAnimationTextureID, POSZAnimationTextureID, NEGZAnimationTextureID); //Set/override all the animation data
            InitalizeGlowAnimationTextures(glowAnimationTextureID, POSXGlowAnimationTextureID, NEGXGlowAnimationTextureID, POSYGlowAnimationTextureID, NEGYGlowAnimationTextureID, POSZGlowAnimationTextureID, NEGZGlowAnimationTextureID); //Set/overide all glow aniamtion data

            //Set variables
            this.blockID = blockID;
            this.lightEmittingFactor = lef;
            this.drop = drop;
            this.dimensions = dimensions;
            this.transparent = transparent;
            this.collide = collide;
            this.glowLightLevel = glowLightLevel;

            if (lef > 0) //If a light emitting block, add it to the list of light emitting blocks
            {
                DataManager.lightEmittingIDs.Add(blockID);
            }
        }

        //This method takes in base face texture information and sets the dictioary FaceData objects to have the desired face.
        private void InitalizeTextures(string textureID,string POSXTextureID,string NEGXTextureID,string POSYTextureID,string NEGYTextureID,string POSZTextureID, string NEGZTextureID)
        {
            //First, set all the faces to have the standard textureID, even if it is null. This is so that even if specific faces are not set, therei s still one "base" face
            foreach(Vector3 direction in directions)
            {
                faces[direction].SetTexture(textureID); //Set the faces texture
            }

            //Now, we'll check if there is are any textures for specified faces
            string[] textures = { POSXTextureID, POSYTextureID, POSZTextureID, NEGXTextureID, NEGYTextureID, NEGZTextureID }; //array of striungs which will be looped
            
            for(int i = 0; i < directions.Length; i++)
            {
                if (!textures[i].Equals("null")) //If there is an actually specified texture, set it
                {
                    faces[directions[i]].SetTexture(textures[i]); //Set the faces texture
                }
            }
        }

        //This method takes in base glow texture information and sets the dictioary FaceData objects to have the desired face.
        private void InitalizeGlows(string glowTextureID, string POSXGlowTextureID, string NEGXGlowTextureID, string POSYGlowTextureID, string NEGYGlowTextureID, string POSZGlowTextureID, string NEGZGlowTextureID)
        {
            if (!glowTextureID.Equals("null")) //If there is a generic all face glowtexture specified, set it
            {
                foreach (Vector3 direction in directions)
                {
                    faces[direction].SetGlowTexture(glowTextureID); //Set the faces glow texture
                }

            }

            //Now, we'll check if there is are any textures for specified faces
            string[] textures = { POSXGlowTextureID, POSYGlowTextureID, POSZGlowTextureID, NEGXGlowTextureID, NEGYGlowTextureID, NEGZGlowTextureID }; //array of striungs which will be looped

            for (int i = 0; i < directions.Length; i++)
            {
                if (!textures[i].Equals("null"))
                {
                    faces[directions[i]].SetGlowTexture(textures[i]); //Sets the faces glow texture
                }
            }
        }

        //Method which sets up the animation textures and gives it to the faceData. This overrides regular face textures if valid animations are given.
        private void InitalizeAnimationTextures(string animationTextureID,string POSXAnimationTextureID, string NEGXAnimationTextureID, string POSYAnimationTextureID, string NEGYAnimationTextureID, string POSZAnimationTextureID, string NEGZAnimationTextureID)
        {
            if (!animationTextureID.Equals("null")) //If there is a generic all face animation specified, set it
            {
                foreach (Vector3 direction in directions)
                {
                    faces[direction].SetAnimation(GetAnimationFrames(animationTextureID)); // get all the animation frames from the id and send it ot hte face
                }

            }

            //Now, we'll check if there is are any textures for specified faces
            string[] textures = { POSXAnimationTextureID, POSYAnimationTextureID, POSZAnimationTextureID, NEGXAnimationTextureID, NEGYAnimationTextureID, NEGZAnimationTextureID }; //array of striungs which will be looped

            for (int i = 0; i < directions.Length; i++)
            {
                if (!textures[i].Equals("null"))
                {
                    faces[directions[i]].SetAnimation(GetAnimationFrames(textures[i])); //Get all animation frames and send it to the face
                }
            }
        }

        //Method which sets up the animation textures and gives it to the faceData for glows. This overrides regular face textures if valid animations are given.
        private void InitalizeGlowAnimationTextures(string glowAnimationTextureID, string POSXGlowAnimationTextureID, string NEGXGlowAnimationTextureID, string POSYGlowAnimationTextureID, string NEGYGlowAnimationTextureID, string POSZGlowAnimationTextureID, string NEGZGlowAnimationTextureID)
        {
            if (!glowAnimationTextureID.Equals("null")) //If there is a generic all face animation specified, set it
            {
                foreach (Vector3 direction in directions)
                {
                    faces[direction].SetGlowAnimation(GetAnimationFrames(glowAnimationTextureID)); // get all the animation frames from the id and send it ot hte face
                }

            }

            //Now, we'll check if there is are any textures for specified faces
            string[] textures = { POSXGlowAnimationTextureID, POSYGlowAnimationTextureID, POSZGlowAnimationTextureID, NEGXGlowAnimationTextureID, NEGYGlowAnimationTextureID, NEGZGlowAnimationTextureID }; //array of striungs which will be looped

            for (int i = 0; i < directions.Length; i++)
            {
                if (!textures[i].Equals("null"))
                {
                    faces[directions[i]].SetGlowAnimation(GetAnimationFrames(textures[i])); //Get all the animation frames and send it to the face
                }
            }
        }

        //Returns an array of all the animation frames in an animation ID
        private string[] GetAnimationFrames(string animationID)
        {
            string key = animationID + "_"; //key that is being checked if valid texture
            int counter = 0; //current frame being counted
            List<string> frames = new List<string>(); //List of all the frames in the animation

            while (DataManager.blockTexturePositions.ContainsKey(key + counter.ToString())) //If this key is valid
            {
                frames.Add(key + counter.ToString()); //Add this key to list
                counter++; //incremetn counter
            }

            return frames.ToArray(); //Return a list of strings with all the frame texture IDs
        }



        /// <summary>
        /// Method that is called when this block's textures animations need to be updated. Increments each animation by one frame.
        /// </summary>
        public void UpdateAnimations()
        {
            foreach(FaceData face in faces.Values)
            {
                if (face.HasAnimation()) //If the face has an animation, update it
                {
                    face.TickAnimation();
                }
            }
        }
        
        //returns if this block has a animation in the specified direction
        public bool HasAnimationInDirection(Vector3 dir){
            return faces[dir].HasAnimation();
        }

        /// <summary>
        /// Initalizes the dictionary
        /// This dictioanry stores the FaceData for all caridnal directions, and stores them using Vector3 keys. This is so they can be easily accessed without if statements.
        /// </summary>
        private void InitializeDictionary()
        {
            //Setup dictionary. Store a new face in each cardiunal direction
            foreach(Vector3 direction in directions)
            {
                faces.Add(direction, new FaceData());
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

        //Method which compares the glow light level to the block color and returns which should be used
        private Color ChooseGlowLightLevel(Color blockColor)
        {
            int colorValue = glowLightLevel * 17 + ChunkRenderer.defaultLightHue; //Get the actual color value from the blocks light level
            Color glowColor = new Color(colorValue, colorValue, colorValue);

            if(blockColor.R > glowColor.R) //Compare colors. Only one (R) has to be checked since RGB are all the same.
            {
                return blockColor;
            }
            else
            {
                return glowColor;
            }
        }

        /// <summary>
        /// Method which calls the appropriate AddVerticies method with the given direction.
        /// It uses the face dictitionary to determine what the starting coordinate of the uV should be and if it should render in a glow
        /// </summary>
        /// <param name="position">position of block</param>
        /// <param name="faceDirection">what face is being drawn</param>
        /// <param name="vertexList">vertex list to add to</param>
        /// <param name="color">color to tint</param>
        public void AddFaceToVertexList(Vector3 position, Vector3 faceDirection, List<VertexPositionColorTexture> vertexList, Color color)
        {
            if(faces.Count < 6)
            {
                return; //Catch in case somehow air is being rendered.
            }

            //Check which direction is being added. Then call that function
            if(faceDirection.Equals(Vector3.UnitX)) //Pos X
            {
                AddPosXVerticiesPos(position, vertexList, color, faces[Vector3.UnitX].startCoordinate); //WE grab the UVs from the facedata and add those verticies

                if (faces[Vector3.UnitX].HasGlow()) //If there is a glow texture, render that as well
                {
                    AddPosXVerticiesPosGlow(position, vertexList, color, faces[Vector3.UnitX].startCoordinateGlow);
                }
                
            }
            if (faceDirection.Equals(-Vector3.UnitX)) //Neg X
            {
                AddNegXVerticiesPos(position, vertexList, color, faces[-Vector3.UnitX].startCoordinate);//WE grab the UVs from the facedata and add those verticies

                if (faces[-Vector3.UnitX].HasGlow()) //If there is a glow texture, render that as well
                {
                    AddNegXVerticiesPosGlow(position, vertexList, color, faces[-Vector3.UnitX].startCoordinateGlow);
                }
            }
            if (faceDirection.Equals(Vector3.UnitY)) //Pos Y
            {
                AddPosYVerticiesPos(position, vertexList, color, faces[Vector3.UnitY].startCoordinate);//WE grab the UVs from the facedata and add those verticies

                if (faces[Vector3.UnitY].HasGlow()) //If there is a glow texture, render that as well
                {
                    AddPosYVerticiesPosGlow(position, vertexList, color, faces[Vector3.UnitY].startCoordinateGlow);
                }
            }
            if (faceDirection.Equals(-Vector3.UnitY)) //Neg Y
            {
                AddNegYVerticiesPos(position, vertexList, color, faces[-Vector3.UnitY].startCoordinate);//WE grab the UVs from the facedata and add those verticies

                if (faces[-Vector3.UnitY].HasGlow()) //If there is a glow texture, render that as well
                {
                    AddNegYVerticiesPosGlow(position, vertexList, color, faces[-Vector3.UnitY].startCoordinateGlow);
                }
            }
            if (faceDirection.Equals(Vector3.UnitZ)) //Pos Z
            {
                AddPosZVerticiesPos(position, vertexList, color, faces[Vector3.UnitZ].startCoordinate);//WE grab the UVs from the facedata and add those verticies

                if (faces[Vector3.UnitZ].HasGlow()) //If there is a glow texture, render that as well
                {
                    AddPosZVerticiesPosGlow(position, vertexList, color, faces[Vector3.UnitZ].startCoordinateGlow);
                }
            }
            if (faceDirection.Equals(-Vector3.UnitZ)) //Neg Z
            {
                AddNegZVerticiesPos(position, vertexList, color, faces[-Vector3.UnitZ].startCoordinate);//WE grab the UVs from the facedata and add those verticies

                if (faces[-Vector3.UnitZ].HasGlow()) //If there is a glow texture, render that as well
                {
                    AddNegZVerticiesPosGlow(position, vertexList, color, faces[-Vector3.UnitZ].startCoordinateGlow);
                }
            }
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
        public void AddPosZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color, Vector2 startCoordinate)
        {
            //XY Plane Z+1
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z + dimensions.Z / 2), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + 0.5f, position.Z + dimensions.Z / 2), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z + dimensions.Z / 2), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - 0.5f, position.Z + dimensions.Z / 2), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z + dimensions.Z / 2), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z + dimensions.Z / 2), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
        }

        public void AddNegZVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color, Vector2 startCoordinate)
        {
            //XY Z-1 Plane
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z - dimensions.Z / 2), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + 0.5f, position.Z - dimensions.Z / 2), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z - dimensions.Z / 2), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - 0.5f, position.Z - dimensions.Z / 2), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z - dimensions.Z / 2), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z - dimensions.Z / 2), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
        }

        public void AddPosXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color, Vector2 startCoordinate)
        {
            //ZY X+1 Plane
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2, position.Y + 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2, position.Y - 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
        }

        public void AddNegXVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color, Vector2 startCoordinate)
        {
            //ZY X-1 Planed
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2, position.Y + 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2, position.Y - 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
        }

        public void AddPosYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color, Vector2 startCoordinate)
        {
            //ZX Y+1
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + dimensions.Y / 2, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + dimensions.Y / 2, position.Z + 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + dimensions.Y / 2, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + dimensions.Y / 2, position.Z - 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + dimensions.Y / 2, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + dimensions.Y / 2, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
        }

        public void AddNegYVerticiesPos(Vector3 position, List<VertexPositionColorTexture> vertexList, Color color, Vector2 startCoordinate)
        {
            //ZX Y-1
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - dimensions.Y / 2, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - dimensions.Y / 2, position.Z + 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - dimensions.Y / 2, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - dimensions.Y / 2, position.Z - 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - dimensions.Y / 2, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - dimensions.Y / 2, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
        }





        //big break so its clear where the glow starts lol







        /// <summary>
        /// Same static methods as above but for specifically glows
        /// </summary>
        /// <param name="position"></param>
        /// <param name="vertexList"></param>
        /// <param name="color"></param>
        public void AddPosZVerticiesPosGlow(Vector3 position, List<VertexPositionColorTexture> vertexList, Color blockColor, Vector2 startCoordinate)
        {
            //XY Plane Z+1
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            Color color = ChooseGlowLightLevel(blockColor); //get which color to use (actual light or glow?)

            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z + dimensions.Z / 2 + glowDistance), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + 0.5f, position.Z + dimensions.Z / 2 + glowDistance), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z + dimensions.Z / 2 + glowDistance), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - 0.5f, position.Z + dimensions.Z / 2 + glowDistance), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z + dimensions.Z / 2 + glowDistance), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z + dimensions.Z / 2 + glowDistance), color, new Vector2(endCoordinate.X, startCoordinate.Y)));

        }

        public void AddNegZVerticiesPosGlow(Vector3 position, List<VertexPositionColorTexture> vertexList, Color blockColor, Vector2 startCoordinate)
        {
            //XY Z-1 Plane
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            Color color = ChooseGlowLightLevel(blockColor); //get which color to use (actual light or glow?)

            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z - dimensions.Z / 2 - glowDistance), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + 0.5f, position.Z - dimensions.Z / 2 - glowDistance), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z - dimensions.Z / 2 - glowDistance), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - 0.5f, position.Z - dimensions.Z / 2 - glowDistance), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + 0.5f, position.Z - dimensions.Z / 2 - glowDistance), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - 0.5f, position.Z - dimensions.Z / 2 - glowDistance), color, new Vector2(endCoordinate.X, startCoordinate.Y)));

        }

        public void AddPosXVerticiesPosGlow(Vector3 position, List<VertexPositionColorTexture> vertexList, Color blockColor, Vector2 startCoordinate)
        {
            //ZY X+1 Plane
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            Color color = ChooseGlowLightLevel(blockColor); //get which color to use (actual light or glow?)

            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 + glowDistance, position.Y + 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 + glowDistance, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 + glowDistance, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 + glowDistance, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 + glowDistance, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + dimensions.X / 2 + glowDistance, position.Y - 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
        }

        public void AddNegXVerticiesPosGlow(Vector3 position, List<VertexPositionColorTexture> vertexList, Color blockColor, Vector2 startCoordinate)
        {
            //ZY X-1 Planed
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            Color color = ChooseGlowLightLevel(blockColor); //get which color to use (actual light or glow?)

            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 - glowDistance, position.Y + 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 - glowDistance, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 - glowDistance, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 - glowDistance, position.Y - 0.5f, position.Z + 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 - glowDistance, position.Y + 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - dimensions.X / 2 - glowDistance, position.Y - 0.5f, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
        }

        public void AddPosYVerticiesPosGlow(Vector3 position, List<VertexPositionColorTexture> vertexList, Color blockColor, Vector2 startCoordinate)
        {
            //ZX Y+1
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            Color color = ChooseGlowLightLevel(blockColor); //get which color to use (actual light or glow?)

            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + dimensions.Y / 2 + glowDistance, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + dimensions.Y / 2 + glowDistance, position.Z + 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + dimensions.Y / 2 + glowDistance, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + dimensions.Y / 2 + glowDistance, position.Z - 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y + dimensions.Y / 2 + glowDistance, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y + dimensions.Y / 2 + glowDistance, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));

        }

        public void AddNegYVerticiesPosGlow(Vector3 position, List<VertexPositionColorTexture> vertexList, Color blockColor, Vector2 startCoordinate)
        {
            //ZX Y-1
            Vector2 endCoordinate = startCoordinate + BlockToUV;  //Calculating UV for end coords
            Color color = ChooseGlowLightLevel(blockColor); //get which color to use (actual light or glow?)

            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - dimensions.Y / 2 - glowDistance, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - dimensions.Y / 2 - glowDistance, position.Z + 0.5f), color, new Vector2(startCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - dimensions.Y / 2 - glowDistance, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - dimensions.Y / 2 - glowDistance, position.Z - 0.5f), color, new Vector2(endCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X - 0.5f, position.Y - dimensions.Y / 2 - glowDistance, position.Z - 0.5f), color, new Vector2(startCoordinate.X, startCoordinate.Y)));
            vertexList.Add(new VertexPositionColorTexture(new Vector3(position.X + 0.5f, position.Y - dimensions.Y / 2 - glowDistance, position.Z + 0.5f), color, new Vector2(endCoordinate.X, endCoordinate.Y)));
        }
    }
}
