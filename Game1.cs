using Liru3D.Animations;
using Liru3D.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BlockGame.Components;
using BlockGame.Components.Entities;
using BlockGame.Components.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Principal;
using BlockGame.Components.World.WorldTools;

namespace BlockGame
{
    public class Game1 : Game
    {
        // Graphics:
        public static GraphicsDeviceManager _graphics; // GraphicsDevice, used to get properties of the PC when rendering 2D and 3D.
        private SpriteBatch _spriteBatch; // 2D rendering object.
        private BasicEffect basicEffect; // 3D rendering object.
        private SkinnedEffect skinEffect; // 3D skinned Mesh rendering object (Basiceffect for 3D animated models).

        // Important Objects:
        private WorldManager world;
        public static Random rnd = new Random();

        // Debug variables:
        public static SpriteFont debugFont;

        // Counters mostly used in early development to determine if features were working. Mostly able to be deprecated now.
        public static int TriangleCount = 0;
        public static int ChunkCount = 0;
        public static int ChunksRendered = 0;
        public static int RebuildCalls = 0;
        static Texture2D rect;
        static public bool debug = false;
        public static int LightingPasses = 0;

        // 3D mesh testing:
        SkinnedModel characterModel;
        AnimationPlayer anim;
        Texture2D characterText;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            //Setting Resolution
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;

        }

        protected override void Initialize()
        {
            //Load all content via datamanager
            DataManager.LoadContent(Content);

            //Generate the hashmap for block data
            DataManager.LoadBlockData();
            DataManager.LoadItemData();


            world = new WorldManager(); // Create the world and player.

            // 3D animation testing debug:
            characterModel = Content.Load<SkinnedModel>("MrFriendlyKindaFinished3");
            characterText = Content.Load<Texture2D>("friend_diffuse");

            basicEffect = new BasicEffect(_graphics.GraphicsDevice);
            skinEffect = new SkinnedEffect(_graphics.GraphicsDevice);
            anim = new AnimationPlayer(characterModel);
            anim.Animation = characterModel.Animations[0];

            anim.IsLooping = true;
            anim.IsPlaying = true;
            anim.CurrentTime = 1.0f;
            anim.CurrentTick = 5;

            debugFont = Content.Load<SpriteFont>("Debug");

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            //Animation debug update
            anim.Update(gameTime);

            //Update the World
            world.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameTime">Game time in frames.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp); // Starting the spritebatch. This tells the GPU to expect to get 2D render calls (or something like that).

            // Setting up graphics device before rendering:
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None;
            _graphics.GraphicsDevice.RasterizerState = rs;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            world.Draw(basicEffect, _spriteBatch, skinEffect); // Render the world (chunks and blocks).

            DrawRectangle(new Rectangle(0, 0, 250, 125), Color.Black); // Draw the debug panel black background.

            // Information lines in the debug panel:
            _spriteBatch.Draw(DataManager.blockAtlas, new Rectangle(150, 150, DataManager.blockAtlas.Width, DataManager.blockAtlas.Height), Color.White);
            _spriteBatch.DrawString(debugFont, "Frames: " + (1 / gameTime.ElapsedGameTime.TotalSeconds).ToString(), new Vector2(0, 0), Color.White);
            _spriteBatch.DrawString(debugFont, "Chunks: " + Game1.ChunkCount, new Vector2(0, 15), Color.White);
            _spriteBatch.DrawString(debugFont, "Rebuilds: " + Game1.RebuildCalls, new Vector2(0, 30), Color.White);
            _spriteBatch.DrawString(debugFont, "Triangles Drawn: " + Game1.TriangleCount, new Vector2(0, 45), Color.White);
            _spriteBatch.DrawString(debugFont, "Lighting Passes: " + Game1.LightingPasses, new Vector2(0, 60), Color.White);
            _spriteBatch.DrawString(debugFont, "Entities: " + world.entities.Count, new Vector2(0, 75), Color.White);
            _spriteBatch.DrawString(debugFont, "Chunks Rendered: " + (ChunksRendered).ToString(), new Vector2(0, 90), Color.White);

            // Player position information (also in debug panel):
            Vector3 temp = world.player.Position / Block.blockSize; // Temporary Vector3 representing the location of the player.
            temp.Floor();
            _spriteBatch.DrawString(debugFont, "Position: " + (temp).ToString(), new Vector2(0, 105), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);

        }

        /// <summary>
        /// Draws a Rectangle in the Spritebatch
        /// </summary>
        /// <param name="coords">Rectangle that needs to be drawn</param>
        /// <param name="color">Color to draw (remember, new colors use bytes)</param> 
        private void DrawRectangle(Rectangle coords, Color color)
        {
            if (rect == null)
            {
                rect = new Texture2D(_graphics.GraphicsDevice, 1, 1);
                rect.SetData(new[] { Color.White });
            }

            _spriteBatch.Draw(rect, coords, color);
        }

    }
}
