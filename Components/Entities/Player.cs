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
using Microsoft.Xna.Framework.Content;
using Assimp;

namespace BlockGame.Components.Entities
{
    internal class Player : Entity
    {
        private GraphicsDeviceManager graphics; //GraphicsDevice
        private Camera camera; //Player camera

        private Vector3 cameraOffset = new Vector3(0, 0.75f, 0); //How much the camera is offset from the actual players origin position

        private static Vector3 playerDimensions = new Vector3(0.5f, 1.5f, 0.5f); //Hitbox dimensions

        public static int renderDistance = 16; //Chunk render distance, in diameter not radius

        private Vector2 playerChunkPos; //Players current chunk position
        private Vector2 lastPlayerChunk; //Players chunk from last frame

        private Inventory inventory; //Inventory
        public int highlightedHotbarSlot; //Which hotbar slot is highlighted by the player

        int scrollTimer = 0; //Timer for scroll wheel delay
        int scrollCooldown = 3; //Cooldown for scrolling

        public Inventory Inventory { get { return inventory; } } //Inventory getter


        //TODO: Entities need view rotation and a physical rotation
        public override Vector3 Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                camera.Rotation = rotation; //For the player, set camera rotation as well as entity rotation
            }
        }

        public override Vector3 Position
        {
            get { return position; }
        }

        private Vector3 mouseRotationBuffer; //Buffer used for moving the camera with the mouse
        private MouseState currentMouseState; //Current mouse info
        private MouseState previousMouseState; //Mouse info from the previous frame
        private float sensitivity = 4f; //Mouse sensitvitiy

        int clickTimer = 0; //Timer/cooldown for interacting with things (left/right click)

        public Player(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager) : base(position, rotation, world, dataManager, playerDimensions)
        {
            graphics = _graphics; //Store graphicsdevice

            camera = new Camera(_graphics, position + cameraOffset, rotation); //Create the camera

            inventory = new Inventory(7,2, dataManager); //Make inventory
            highlightedHotbarSlot = 0; //Set the current hotbar slot to 0

            this.rotation = rotation; //Set rotation
            this.speed = 10f; //Set the players movement speed

            int[] chunkPos = WorldManager.posInWorlditionToChunkIndex(position); //Get the players current chunk position

            world.LoadChunksInstantly(new Vector2(chunkPos[0], chunkPos[1]), renderDistance); //Load all the chunks around the player instantly

            inventory.AddItem(2, 1); //Add bomb (debug)
        }


        public Camera Camera
        {
            get { return camera; }
        }

        /// <summary>
        /// Move to a specific postiion/rotation.
        /// Unlike most entities, when the player moves, the camera must move with it
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        public override void MoveTo(Vector3 pos, Vector3 rot)
        {
            position = pos;
            rotation = rot;
            camera.MoveTo(pos + cameraOffset, rot); //Move camera
            rayOriginPosition = pos + cameraOffset; //Set the position where the view ray is sent to the cameras offset
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; //Time since last frame

            //Update click timer
            clickTimer -= 1;

            //Keyboard input stuff
            KeyboardInput(deltaTime);

            //Getting currentChunk (before movement) in Vector2, array format
            playerChunkPos = new Vector2(WorldManager.posInWorlditionToChunkIndex(position)[0], WorldManager.posInWorlditionToChunkIndex(position)[1]);
            Vector2 lastPlayerChunk = new Vector2(playerChunkPos.X, playerChunkPos.Y);

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

            previousMouseState = currentMouseState; //Update prev mouse state

            base.Update(gameTime);

        }

        /// <summary>
        /// Handles keyboard inputs
        /// </summary>
        /// <param name="deltaTime"></param>
        private void KeyboardInput(float deltaTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();  //Get keyboard information

            //Vector for what direction the player is moving
            Vector3 dir = Vector3.Zero;

            //Basic movement inputs
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
                enforceGravity = !enforceGravity; //Turn off player gravity

                //Reset Y velocities
                dir.Y = 0; 
                velocity.Y = 0;
                dynamic_velocity.Y = 0;
            }

            //Debug mode flying
            if (Game1.debug)
            {
                if (keyboardState.IsKeyDown(Keys.Space))
                {
                    dir.Y = 1; //Go up
                }
                if (keyboardState.IsKeyDown(Keys.LeftShift))
                {
                    dir.Y = -1; //Go down
                }
            }
            else
            {
                //If not in debug mode, then jump (add Y velocity)
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

            fixed_rotation_based_velocity = GetFixedMovementVector(dir); //Set the fixed velocity to the direction
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
                if(highlightedHotbarSlot == 0)
                {
                    highlightedHotbarSlot = inventory.GetWidth() - 1;  //If slot is already 0, go back to the top
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
                if (highlightedHotbarSlot == inventory.GetWidth() - 1)
                {
                    highlightedHotbarSlot = 0;  //If slot is already 0, go back to the top
                }
                else
                {
                    highlightedHotbarSlot++;
                }

                scrollTimer = 0; //Reset the scrolling timer
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
            int crossHairScale = 3; //Scale for the crosshair size
            Rectangle atlasRect = new Rectangle(47, 28, 7, 7); //Crosshair texture position in UIatlas
            spriteBatch.Draw(DataManager.uiAtlas, new Rectangle(graphics.GraphicsDevice.Viewport.Width / 2 - atlasRect.Width/2*crossHairScale, graphics.GraphicsDevice.Viewport.Height / 2 - atlasRect.Height / 2 * crossHairScale, atlasRect.Width*crossHairScale,atlasRect.Height*crossHairScale), atlasRect, Color.White);

            base.Draw(_graphics, basicEffect, camera, spriteBatch, skinEffect);
        }


        /// <summary>
        /// Post update function, which is used to see if the players chunk has changed chunks after updating. This is used to load new chunks
        /// Generally can be used for running code after update.
        /// </summary>
        public override void PostUpdate()
        {
            //After movement, seeing if chunk changed
            //Getting currentChunk (before movement) in Vector2, array format
            playerChunkPos = new Vector2(WorldManager.posInWorlditionToChunkIndex(position)[0], WorldManager.posInWorlditionToChunkIndex(position)[1]);
            Vector2 newPlayerChunk = new Vector2(playerChunkPos.X, playerChunkPos.Y);

            //If the player has moved chunks,  we need to update chunks
            if (!newPlayerChunk.Equals(lastPlayerChunk))
            {
                world.LoadChunks(newPlayerChunk, renderDistance); //Load the chunks in the render distance
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

        /// <summary>
        /// Turning the players direction into a vector, based on cameras rotation.
        /// </summary>
        /// <param name="amount">Player direction to move</param>
        /// <returns></returns>
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
