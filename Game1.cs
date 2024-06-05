using Liru3D.Animations;
using Liru3D.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BlockGame.Components;
using BlockGame.Components.Entity;
using BlockGame.Components.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Principal;

namespace BlockGame
{
    public class Game1 : Game
    {
        //Graphics
        private GraphicsDeviceManager _graphics; //GraphicsDevice, used to get properties of the PC when rendering 2D and 3D
        private SpriteBatch _spriteBatch; //2D rendering object
        private BasicEffect basicEffect; //3D rendering object
        private SkinnedEffect skinEffect; //3D skinned Mesh rendering object (Basiceffect for 3D animated models)

        //Important Objects
        private Player player;
        private WorldManager world;
        private DataManager dataManager = new DataManager();
        public static Random rnd = new Random();

        //Debug variables
        public static SpriteFont debugFont;

        //Counters mostly used in early development to determine if features were working. Mostly able to be deprecated now.
        public static int TriangleCount = 0;
        public static int BlockCount = 0;
        public static int ChunkCount = 0;
        public static int ChunksRendered = 0;
        public static int RebuildCalls = 0;

        static Texture2D rect; //A texture used to draw all 2D reactangles

        static public bool debug = false; //Boolean determining if debug mode is on (essentially F3 in minecraft with noclip)

        //3D mesh testing
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
            //Generate the hashmap for block data
            dataManager.LoadBlockData();
            dataManager.LoadItemData();

            //Create the world and player
            world = new WorldManager(_graphics.GraphicsDevice, dataManager);
            player = new Player(_graphics.GraphicsDevice, new Vector3(0f, 25, 0f), Vector3.Zero, world, dataManager);

            //3D animation testing debug
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

            //Load all content via datamanager
            dataManager.LoadContent(Content);

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            //Animation debug update
            anim.Update(gameTime);

            //Update Entities
            player.Update(gameTime);

            //Update the World
            world.Update(player);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            //Starting the spritebatch. This tells the GPU to expect to get 2D render calls (or something like that)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            //Setting up graphics device before rendering
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None;
            _graphics.GraphicsDevice.RasterizerState = rs;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            //Render the world (chunks and blocks)
            world.Draw(player.Camera, basicEffect);

            //Render the player
            player.Draw(_graphics, basicEffect, player.Camera, _spriteBatch);

            //Everything below is debug stuff

            skinEffect.World = Matrix.CreateTranslation(0, 20 / 1, 0) * Matrix.CreateScale(1f, 1f, 1f);
            skinEffect.View = player.Camera.View;
            skinEffect.Projection = player.Camera.Projection;
            skinEffect.EnableDefaultLighting();


            foreach (SkinnedMesh mesh in characterModel.Meshes)
            {
                skinEffect.Texture = characterText;
                anim.SetEffectBones(skinEffect);
                skinEffect.CurrentTechnique.Passes[0].Apply();

                mesh.Draw();
            }

            //Debug Panel

            DrawRectangle(new Rectangle(0, 0, 200, 95), Color.Black);

            _spriteBatch.DrawString(debugFont, "Frames: " + (1 / gameTime.ElapsedGameTime.TotalSeconds).ToString(), new Vector2(0, 0), Color.White);

            if ((1 / gameTime.ElapsedGameTime.TotalSeconds) < 55)
            {
                //Debug.WriteLine((1 / gameTime.ElapsedGameTime.TotalSeconds));
                //Debug.WriteLine("Blocks" + Game1.BlockCount.ToString());
            }

            _spriteBatch.DrawString(debugFont, "Chunks: " + Game1.ChunkCount, new Vector2(0, 15), Color.White);
            _spriteBatch.DrawString(debugFont, "Rebuilds: " + Game1.RebuildCalls, new Vector2(0, 30), Color.White);
            _spriteBatch.DrawString(debugFont, "Triangles Drawn: " + Game1.TriangleCount, new Vector2(0, 45), Color.White);
            Vector3 temp = player.Position / Block.blockSize;
            temp.Floor();
            _spriteBatch.DrawString(debugFont, "Position: " + (temp).ToString(), new Vector2(0, 60), Color.White);
            _spriteBatch.DrawString(debugFont, "Chunks Rendered: " + (ChunksRendered).ToString(), new Vector2(0, 75), Color.White);

            //Debug Map
            /*            int[,] noise = world.dungeonMap;
                        for (int x = 0; x < (WorldManager.chunksGenerated - 2) * Chunk.chunkLength; x++)
                        {
                            for (int y = 0; y < (WorldManager.chunksGenerated - 2) * Chunk.chunkWidth; y++)
                            {
                                *//*                    DrawRectangle(new Rectangle(_graphics.PreferredBackBufferWidth - x, y, 1, 1), new Color(Convert.ToByte(Math.Clamp(127.5f - noise[x, y] * 127.5, 0, Convert.ToByte(255))), Convert.ToByte(127.5f), Convert.ToByte(127.5f)));
                                */
            /*                    Debug.WriteLine(noise[x, y]);  
            *//*

            //draw map
            if (noise[x, y] == 1)
            {
                DrawRectangle(new Rectangle(_graphics.PreferredBackBufferWidth - x, y, 1, 1), Color.Black);
            }
            else
            {
                DrawRectangle(new Rectangle(_graphics.PreferredBackBufferWidth - x, y, 1, 1), Color.White);
            }

        }
    }*/


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
