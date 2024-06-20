﻿using Microsoft.Xna.Framework.Input;
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
using Microsoft.Xna.Framework.Content;
using Assimp;

namespace BlockGame.Components.Entities
{
    internal class Player : Entity
    {
        //Attributes
        private GraphicsDeviceManager graphics;
        private Camera camera;

        //Offset of camera from players origin
        private Vector3 cameraOffset = new Vector3(0, 0.75f, 0);

        //dimensions
        private static Vector3 playerDimensions = new Vector3(0.5f, 1.5f, 0.5f);

        public static int renderDistance = 16; //this is diameter, not raidus! :)

        //Variables storing the players chunk and its chunk last frame. Why one is an int[] and one is a Vector2 is something to fix.
        private int[] playerChunkPos;
        private Vector2 lastPlayerChunk;

        //Inventory
        private Inventory inventory;
        public int highlightedHotbarSlot;

        //Scroll bar (hotbar)
        int scrollTimer = 0;
        int scrollCooldown = 3;

        //Inventory getter
        public Inventory Inventory { get { return inventory; } }


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

        //Breaking block cooldown
        int clickTimer = 0;

        public Player(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager) : base(position, rotation, world, dataManager, playerDimensions)
        {
            graphics = _graphics;
            camera = new Camera(_graphics, position + cameraOffset, rotation);

            //Setting up inventory
            inventory = new Inventory(7,2, dataManager);
            highlightedHotbarSlot = 0;

            //Set Camera position and rotation
            this.rotation = rotation;
            this.speed = 10f;
            this.world = world;

            //Build vertex buffers around chunk where spawned
            int[] chunkPos = WorldManager.WorldPositionToChunkIndex(position);

            //Load all the chunks around the player instantly on creation
            world.LoadChunksInstantly(new Vector2(chunkPos[0], chunkPos[1]), renderDistance);

            //debug items
            inventory.AddItem(2, 1);
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
            camera.MoveTo(pos + cameraOffset, rot);
            rayOriginPosition = pos + cameraOffset;
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; //time s9ince last frame (i think)

            //Update click timer
            clickTimer -= 1;

            //Keyboard input stuff
            KeyboardInput(deltaTime);

            //Getting currentChunk (before movement) in Vector2, array format
            playerChunkPos = WorldManager.WorldPositionToChunkIndex(position);
            Vector2 lastPlayerChunk = new Vector2(playerChunkPos[0], playerChunkPos[1]);

            //Update mouse state
            currentMouseState = Mouse.GetState();

            //Moving the camera with the mouse
            CameraMovement(deltaTime);

            //Update Ray view
            RaySight();

            //Calc closest block
            CalculateClosestFace();

            //Mouse inputs (like block breaking)
            MouseInput(deltaTime);

            previousMouseState = currentMouseState;

            base.Update(gameTime);

        }

        /// <summary>
        /// Handles keyboard inputs
        /// </summary>
        /// <param name="deltaTime"></param>
        private void KeyboardInput(float deltaTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            //Getting movement vector3
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

            //Turning debug on and off
            if (Keyboard.HasBeenPressed(Keys.F3))
            {
                Game1.debug = !Game1.debug;
                enforceGravity = !enforceGravity;
                dir.Y = 0;
                velocity.Y = 0;
                dynamic_velocity.Y = 0;
            }

            //IF debug, then fly, if not, then jump
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

            //Adding direction and speed to fixed movement velocity
            if (dir != Vector3.Zero)
            {
                //normalize vector (so dont move faster diagonally)
                dir.Normalize();

                //add smooth and speed
                dir *= deltaTime * speed;
            }

            fixed_rotation_based_velocity = GetFixedMovementVector(dir);
        }

        /// <summary>
        /// Handles camera movement and rotation due to the mouse
        /// </summary>
        /// <param name="deltaTime"></param>
        private void CameraMovement(float deltaTime)
        {
            //Mouse movement
            float deltaX;
            float deltaY;

            //Building rotation buffer for camera movement
            if (currentMouseState != previousMouseState)
            {
                deltaX = currentMouseState.X - graphics.GraphicsDevice.Viewport.Width / 2;
                deltaY = currentMouseState.Y - graphics.GraphicsDevice.Viewport.Height / 2;

                mouseRotationBuffer.X -= 0.01f * deltaX * deltaTime * sensitivity;
                mouseRotationBuffer.Y -= 0.01f * deltaY * deltaTime * sensitivity;
            }

            //Clamping vertical mouse movemnt
            if (mouseRotationBuffer.Y < MathHelper.ToRadians(-89.0f))
            {
                mouseRotationBuffer.Y = mouseRotationBuffer.Y - (mouseRotationBuffer.Y - MathHelper.ToRadians(-89.0f));
            }
            else if (mouseRotationBuffer.Y > MathHelper.ToRadians(89.0f))
            {
                mouseRotationBuffer.Y = mouseRotationBuffer.Y - (mouseRotationBuffer.Y - MathHelper.ToRadians(89.0f));
            }

            //After getting mouse movement, set rotation to this using the Rotation buffer
            Rotation = new Vector3(-MathHelper.Clamp(mouseRotationBuffer.Y, MathHelper.ToRadians(-89.0f), MathHelper.ToRadians(89.0f)), MathHelper.WrapAngle(mouseRotationBuffer.X), 0);

            //Reset mouse to middle of screen
            Mouse.SetPosition(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);

        }

        /// <summary>
        /// Handles mouse inputs
        /// </summary>
        /// <param name="deltaTime"></param>
        private void MouseInput(float deltaTime)
        {
            //If not on cooldown and left click, then do left click
            if (currentMouseState.LeftButton == ButtonState.Pressed && clickTimer <= 0)
            {
                //add cooldown
                clickTimer = 10;
                LeftClick();
            }

            //If not on cooldown and right click, then do right click
            if (currentMouseState.RightButton == ButtonState.Pressed && clickTimer <= 0 )
            {
                //add cooldown
                clickTimer = 10;
                RightClick();
            }

            //Scroll hotbar
            //Update timer
            scrollTimer++;

            //If scroll down, move slot down
            if(scrollTimer > scrollCooldown && previousMouseState.ScrollWheelValue < currentMouseState.ScrollWheelValue)
            {
                //If slot is already 0, go back to the top
                if(highlightedHotbarSlot == 0)
                {
                    highlightedHotbarSlot = inventory.GetWidth() - 1;
                }
                else
                {
                    highlightedHotbarSlot--;
                }
                scrollTimer = 0;
            }
            //If scroll up, move slot up
            else if (scrollTimer > scrollCooldown && previousMouseState.ScrollWheelValue > currentMouseState.ScrollWheelValue)
            {
                //If slot is already 0, go back to the top
                if (highlightedHotbarSlot == inventory.GetWidth() - 1)
                {
                    highlightedHotbarSlot = 0;
                }
                else
                {
                    highlightedHotbarSlot++;
                }
                scrollTimer = 0;
            }
        }

        private void LeftClick()
        {
            //If a valid closest block is within reach, then...
            if (Vector3.Distance(position, ClosestFace.hitbox.Max) < 10)
            {
                //Do whaterver should happen when block is left clicked (usually nothing)
                dataManager.blockData[world.GetBlockAtWorldIndex(ClosestFace.blockPosition)].OnLeftClick(inventory, world, ClosestFace.blockPosition);

            }

            //Do whatever should happen when the item is right clicked
            inventory.LeftClickItemSlot(new Vector2(highlightedHotbarSlot, inventory.GetHeight() - 1), world, this, this);
        }

        private void RightClick()
        {
            //If a valid closest and is within reach, then...
            if (Vector3.Distance(position, ClosestFace.hitbox.Max) < 10)
            {
                //Add cooldown
                clickTimer = 10;

                //Do whaterver should happen when block is right clicked (usually nothing)
                dataManager.blockData[world.GetBlockAtWorldIndex(ClosestFace.blockPosition)].OnRightClick(inventory, world, ClosestFace.blockPosition);
            }

            //Do whatever should happen when the item is right clicked
            inventory.RightClickItemSlot(new Vector2(highlightedHotbarSlot, inventory.GetHeight() - 1), world, this, this);

        }

        public override void Draw(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera, SpriteBatch spriteBatch, SkinnedEffect skinEffect)
        {
            //Draw inventory
            inventory.DrawBottomBar(new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight - 50), spriteBatch, highlightedHotbarSlot);

            //Draw crosshair
            int crossHairScale = 3;
            Rectangle atlasRect = new Rectangle(47, 28, 7, 7);
            spriteBatch.Draw(DataManager.uiAtlas, new Rectangle(graphics.GraphicsDevice.Viewport.Width / 2 - atlasRect.Width/2*crossHairScale, graphics.GraphicsDevice.Viewport.Height / 2 - atlasRect.Height / 2 * crossHairScale, atlasRect.Width*crossHairScale,atlasRect.Height*crossHairScale), atlasRect, Color.White);

            base.Draw(_graphics, basicEffect, camera, spriteBatch, skinEffect);
        }


        /// <summary>
        /// Post update function, which is used to see if the players chunk has changed chunks after updating. This is used to load new chunks
        /// </summary>
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
