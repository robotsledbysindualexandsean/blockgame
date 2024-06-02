using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using BlockGame.Components.World;
using System.Diagnostics;
using BlockGame.Components.Items;

namespace BlockGame.Components.Entity
{
    internal class Player : Entity
    {
        //Attributes
        private GraphicsDevice graphics;
        private Camera camera;
        public static int renderDistance = 16; //this is diameter, not raidus! :)

        //Variables storing the players chunk and its chunk last frame. Why one is an int[] and one is a Vector2 is something to fix.
        private int[] playerChunkPos;
        private Vector2 lastPlayerChunk;

        //Inventory
        private Inventory inventory = new Inventory(5, 9);

        public override Vector3 Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                camera.Rotation = rotation;
            }
        }

        public override Vector3 Position
        {
            get { return position; }
        }

        //Mouse variables used for camera movement
        private Vector3 mouseRotationBuffer;
        private MouseState currentMouseState;
        private MouseState previousMouseState;
        private float sensitivity = 4f;

        public Player(GraphicsDevice _graphics, Vector3 position, Vector3 rotation, WorldManager world) : base(position, rotation, world)
        {
            graphics = _graphics;
            camera = new Camera(_graphics, position, rotation);

            //Set Camera position and rotation
            this.rotation = rotation;
            this.speed = 10f;
            this.world = world;

            //Build vertex buffers around chunk where spawned
            int[] chunkPos = WorldManager.WorldPositionToChunkIndex(position);

            //Load all the chunks around the player instantly on creation
            world.LoadChunksInstantly(new Vector2(chunkPos[0], chunkPos[1]), renderDistance);

        }


        public Camera Camera
        {
            get { return camera; }
        }

        //Method that sets camera pos and rot
        public override void MoveTo(Vector3 pos, Vector3 rot)
        {
            position = pos;
            rotation = rot;
            camera.MoveTo(pos, rot);
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; //time s9ince last frame (i think)
            KeyboardState keyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

            //Getting currentChunk (before movement) in Vector2, array format
            playerChunkPos = WorldManager.WorldPositionToChunkIndex(position);
            Vector2 lastPlayerChunk = new Vector2(playerChunkPos[0], playerChunkPos[1]);

            //Movement
            Vector3 dir = Vector3.Zero;

            if (keyboardState.IsKeyDown(Keys.W))
            {
                dir.Z = 1;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                dir.Z = -1;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                dir.X = 1;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                dir.X = -1;
            }

            if(Keyboard.HasBeenPressed(Keys.F3))
            {
                Game1.debug = !Game1.debug;
                dir.Y = 0;
                velocity.Y = 0;
                dynamic_velocity.Y = 0;
            }
            
            if (Game1.debug)
            {
                if (keyboardState.IsKeyDown(Keys.Space))
                {
                    dir.Y = 1;
                }
                if (keyboardState.IsKeyDown(Keys.LeftShift))
                {
                    dir.Y = -1;
                }
            }
            else
            {
                if (Keyboard.HasBeenPressed(Keys.Space))
                {
                    dynamic_velocity.Y = 0.1f;
                }
            }

            if (dir != Vector3.Zero)
            {
                //normalize vector (so dont move faster diagonally)
                dir.Normalize();

                //add smooth and speed
                dir *= deltaTime * speed;
            }

            fixed_rotation_based_velocity = GetFixedMovementVector(dir);

            //Mouse movement
            float deltaX;
            float deltaY;

            if (currentMouseState != previousMouseState)
            {
                deltaX = currentMouseState.X - graphics.Viewport.Width / 2;
                deltaY = currentMouseState.Y - graphics.Viewport.Height / 2;

                mouseRotationBuffer.X -= 0.01f * deltaX * deltaTime * sensitivity;
                mouseRotationBuffer.Y -= 0.01f * deltaY * deltaTime * sensitivity;
            }

            //Clamping vertical mouse movemnt
            if (mouseRotationBuffer.Y < MathHelper.ToRadians(-75.0f))
            {
                mouseRotationBuffer.Y = mouseRotationBuffer.Y - (mouseRotationBuffer.Y - MathHelper.ToRadians(-75.0f));
            }
            else if (mouseRotationBuffer.Y > MathHelper.ToRadians(75.0f))
            {
                mouseRotationBuffer.Y = mouseRotationBuffer.Y - (mouseRotationBuffer.Y - MathHelper.ToRadians(75.0f));
            }

            //After getting mouse movement, set rotation to this using the Rotation buffer
            Rotation = new Vector3(-MathHelper.Clamp(mouseRotationBuffer.Y, MathHelper.ToRadians(-75.0f), MathHelper.ToRadians(75.0f)), MathHelper.WrapAngle(mouseRotationBuffer.X), 0);

            //Reset mouse to middle of screen
            Mouse.SetPosition(graphics.Viewport.Width / 2, graphics.Viewport.Height / 2);

            previousMouseState = currentMouseState;

            //Update ray
            RaySight();

            //Block breaking
            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                int closest = 0;
                
                for(int i = 0; i < rayHitBoxes.Count; i++)
                {
                    if(Vector3.Distance(position, rayHitBoxes[i].Max) < Vector3.Distance(position, rayHitBoxes[closest].Max))
                    {
                        closest = i;
                    }
                }

                if (closest >= 0 && closest < rayHitBoxes.Count && Vector3.Distance(position, rayHitBoxes[closest].Max) < 10)
                {
                    Vector3 block = rayHitBoxes[closest].Max - new Vector3(Block.blockSize / 2, Block.blockSize / 2, Block.blockSize / 2);

                    int[] data = WorldManager.WorldPositionToChunkIndex(block);

                    world.GetChunk(new Vector2(data[0], data[1])).SetBlock(new Vector3(data[2], data[4], data[3]), 0);
                }

            }

            //Block placing
            if (currentMouseState.RightButton == ButtonState.Pressed)
            {
                int closest = 0;

                for (int i = 0; i < rayHitBoxes.Count; i++)
                {
                    if (Vector3.Distance(position, rayHitBoxes[i].Max) < Vector3.Distance(position, rayHitBoxes[closest].Max))
                    {
                        closest = i;
                    }
                }

                if (closest >= 0 && closest < rayHitBoxes.Count && Vector3.Distance(position, rayHitBoxes[closest].Max) < 10)
                {
                    Vector3 block = rayHitBoxes[closest].Max - new Vector3(Block.blockSize / 2, Block.blockSize / 2, Block.blockSize / 2);

                    int[] data = WorldManager.WorldPositionToChunkIndex(block + rayHitBoxesNormals[closest]);

                    world.GetChunk(new Vector2(data[0], data[1])).SetBlock(new Vector3(data[2], data[4], data[3]), 5);
                }

            }

            base.Update(gameTime);



        }

        public override void Draw(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera, SpriteBatch spriteBatch)
        {
            inventory.DrawBottomBar(new Vector2(30, 600), spriteBatch);
            base.Draw(_graphics, basicEffect, camera, spriteBatch);
        }

        public override void PostUpdate()
        {
            //After movement, seeing if chunk changed
            //Getting currentChunk (before movement) in Vector2, array format
            playerChunkPos = WorldManager.WorldPositionToChunkIndex(position);
            Vector2 newPlayerChunk = new Vector2(playerChunkPos[0], playerChunkPos[1]);

            //IF changed, now we need to update chunks
            if (!newPlayerChunk.Equals(lastPlayerChunk))
            {
                world.LoadChunks(newPlayerChunk, renderDistance);
            }

            base.PostUpdate();
        }

        //method that simulates movement
        public override Vector3 PreviewMove(Vector3 amount)
        {
            //Create rotation matrix
            Matrix rotate = Matrix.CreateRotationY(rotation.Y);

            //Create movement vector
            Vector3 movement = new Vector3(amount.X, amount.Y, amount.Z);
            movement = Vector3.Transform(movement, rotate);

            //Return value of camera position + movement vector
            return position + movement;
        }

        //Actually moving the camera
        public override void Move(Vector3 scale)
        {
            MoveTo(PreviewMove(scale), Rotation);
        }

        public Vector3 GetFixedMovementVector(Vector3 amount)
        {
            //Create rotation matrix
            Matrix rotate = Matrix.CreateRotationY(rotation.Y);

            //Create movement vector
            Vector3 movement = new Vector3(amount.X, amount.Y, amount.Z);
            movement = Vector3.Transform(movement, rotate);

            //Return value of camera position + movement vector
            return movement;
        }
    }
}
