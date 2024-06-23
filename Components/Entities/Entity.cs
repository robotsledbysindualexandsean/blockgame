using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BlockGame.Components.World;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Liru3D.Models;
using Liru3D.Animations;

namespace BlockGame.Components.Entities
{
    internal class Entity
    {
        public Vector3 position; //Entity's position
        protected Vector3 lastPosition; //The entity's position last frame
        public Vector3 rotation; //Entitiy's rotation

        public Vector3 fixed_rotation_based_velocity; //Fixed rotation, based on entities facing direction (for fixed "forward" control)
        public Vector3 rotational_velocity = Vector3.Zero; //Velocity in terms of entities rotation
        public Vector3 dynamic_velocity = Vector3.Zero; //Dynamic velocity caused by "forces". This slowly dissipates to 0.
        public Vector3 velocity; //Total velocity (fixed + dynamic)

        private Vector3 dimensions; //Entities dimensions, used for hitbox

        public WorldManager world; //World
        public DataManager dataManager; //DataManager

        public float speed = 5f; //Entity's speed
        protected bool enforceGravity = true; //Is the entity affected by gravity?

        public BoundingBox hitbox; //Entity's hitbox
        private Ray viewRay; //Ray which points in the entity's facing direction
        public List<Face> rayFaces = new List<Face>(); //List of all the faces that the viewRay is hitting
        protected Vector3 rayOriginPosition; //Where the ray is shot from (this is because position =/= camera for player)
        protected Face closestFace; //Closest face to the entity

        public Vector3 direction; //Entities direction
        private Vector3 forward = new Vector3(0,0,1); //Forward direction, used for turning rotation into a ray

        private Face standingBlock; //What block the entity is currently standing on

        private List<VertexPositionColor> vertexList = new List<VertexPositionColor>(); //Debug list of hitbox verticies
        private VertexBuffer vertexBuffer; //Debug vertex buffer

        protected SkinnedModel model; //Entity's model
        protected Texture2D modelTexture; //Entity's model's texture
        protected AnimationPlayer modelAnimation; //Entity's model's animation

        //Getter for the entity's closest face
        public Face ClosestFace
        {
            get { return closestFace; }
        }

        //Getter and Setter for entities rotation
        public virtual Vector3 Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
               
            }
        }

        //Getter for entities position
        public virtual Vector3 Position
        {
            get { return position; }
        }

        public Entity(Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager, Vector3 dimensions)
        {
            this.position = position; //Set position
            this.rotation = rotation; //Set rotation
            this.world = world; //Set world
            this.dataManager = dataManager; //Set DataManager
            this.dimensions = dimensions; //Set hitbox dimensions

            hitbox = new BoundingBox(new Vector3(position.X - dimensions.X / 2, position.Y - dimensions.Y /2, position.Z - dimensions.Z /2), new Vector3(position.X + dimensions.X / 2, position.Y+dimensions.Y / 2, position.Z + dimensions.Z / 2)); //Create the inital hitbox
            viewRay = new Ray(position, rotation); //Create the inital ray
        }

        public virtual void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; //Get the time between this frame and the last
            lastPosition = position; //Update last position

            Matrix yawPitchRoll = Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z); //Turn entity rotation into a direction matrix
            direction = Vector3.Transform(forward, yawPitchRoll); //Turn the entity rotation into a direction vector (using direction matrix)

            rotation += rotational_velocity; //Update rotational velocity

            //Reduce the all velocities towards 0. This is "friction" so to say.
            dynamic_velocity.X /= 1.25f;
            dynamic_velocity.Z /= 1.25f;
            rotational_velocity /= 1.25f;

            if (dynamic_velocity.Y >= -0.1 && enforceGravity) //If the entity shoud have gravity and isn't at terminal velocity
            {
                dynamic_velocity.Y -= 0.2f * deltaTime; //Accelerate Y velocity downwards
            }

            velocity = dynamic_velocity + fixed_rotation_based_velocity; //Velocity is made of two components, dynamic (applied forces) and fixed (movement). These are added together once both are calculated.

            //Create a temporary hitbox with velocity added to perform future collision calcualtions
            BoundingBox tempHitbox = new BoundingBox(new Vector3(hitbox.Min.X + velocity.X, hitbox.Min.Y + velocity.Y, hitbox.Min.Z + velocity.Z), new Vector3(hitbox.Max.X + velocity.X, hitbox.Max.Y + velocity.Y, hitbox.Max.Z + velocity.Z)); 

            //Check if the entity is still colliding with their standing block. If so, then just quickly reset Y velocity that to 0 to avoid collision checks.
            if (standingBlock != null && tempHitbox.Intersects(standingBlock.hitbox) && world.GetBlockAtWorldIndex(standingBlock.blockPosition) != 0)
            {
                velocity.Y = 0; //Set velocity to 0
            }

            //Only update block collision if there is any velocity (no movement means no collision has changed)
            if (velocity != Vector3.Zero)
            {
                standingBlock = null; //Reset the entity's standing block

                CollideBlocks(hitbox, tempHitbox); //Apply the velocity to a temporary hitbox and then check the collisions on that. This will stop movement that goes into a block collider.

                MoveTo(velocity + position, Rotation); //Apply velocity

                //Remake hitbox
                hitbox = new BoundingBox(new Vector3(position.X - dimensions.X / 2, position.Y - dimensions.Y / 2, position.Z - dimensions.Z / 2), new Vector3(position.X + dimensions.X / 2, position.Y + dimensions.Y / 2, position.Z + dimensions.Z / 2));

                //If position has changed
                if (!lastPosition.Equals(position))
                {
                    OnMovement(); //Rebuild vertex buffer 
                    BuildBoundingBoxDebug(); //Build debug bounding box buffer
                }
            }

            //Update models animation
            if(modelAnimation != null)
            {
                modelAnimation.Update(gameTime);
            }

            PostUpdate(); //Run post update
        }

        /// <summary>
        /// Loads a model. This is class specific.
        /// </summary>
        protected virtual void LoadModel()
        {

        }

        /// <summary>
        /// Method should something special happen to the entity on it's movement.
        /// Runs when position has changed.
        /// </summary>
        protected virtual void OnMovement()
        {

        }

        /// <summary>
        /// Calculates which face (of the faces hit by the players ray) is closest. Not run every update frame for the base entity class
        /// Only run when needed (similar to RaySight()). Must be run before raysight for accurate results.
        /// </summary>
        protected void CalculateClosestFace()
        {
            //If no faces hit, change nothing.
            if (rayFaces.Count <= 0)
            {
                return;
            }

            closestFace = rayFaces[0]; //Set closest face to 0 index to start

            //Check all faces, find the closest
            for (int i = 0; i < rayFaces.Count; i++)
            {
                if (Vector3.Distance(position, rayFaces[i].blockPosition + rayFaces[i].blockNormal) < Vector3.Distance(position, closestFace.blockPosition + closestFace.blockNormal)) //Check if distance of current iteration 'i' is closer than the currnet "closest"
                {
                    closestFace = rayFaces[i]; //Set as closest
                }
            }
        }

        /// <summary>
        /// Method which updates this entit's list of faces it is hitting (rayFaces).
        /// This is NOT called on base entity update, it will only be called in higher classes
        /// This is because not all entities need this info.
        /// </summary>
        public void RaySight()
        {
            viewRay = new Ray(rayOriginPosition, direction); //Cast a ray in the looking direction

            Chunk[,] chunks = world.GetChunksNearby(position, 1); //Get the chunks the entity is in

            rayFaces = new List<Face>(); //Reset list

            //For each chunk, if a ray hits a face in the chunk, then add it to the list.
            foreach (Chunk chunk in chunks)
            {
                //If on the edge of the world, skip that chunk.
                if (chunk == null)
                {
                    continue;
                }

                List<Face> faces = chunk.visibleFaces; //Get chunk's visible faces

                //Check each visble face, if it collides with ray
                for(int i = 0; i < faces.Count; i++)
                {
                    if (viewRay.Intersects(faces[i].hitbox) != null)
                    {
                        rayFaces.Add(faces[i]); //If ray collides, then  add to list

                    }
                }
            }
        }

        /// <summary>
        /// Method which checks collisions with blocks, and then stop movement if it hits a block.
        /// Takes the current hitbox of the entity as well as the predicted hitbox once movement is applied.
        /// </summary>
        /// <param name="currentHitbox">Current entities hitbox</param>
        /// <param name="predictedHitbox">Hitbox which will happen after velocity is applied</param>
        public void CollideBlocks(BoundingBox currentHitbox, BoundingBox predictedHitbox) {
            Chunk[,] chunks = world.GetChunksNearby(position, 1); //Get chunks nearby

            //Check each face in each chunk. If the player collides with it, stop movement in that direction.
            foreach (Chunk chunk in chunks)
            {
                //Check edge case, edge of the world.
                if(chunk == null)
                {
                    continue;
                }

                chunk.drawHitboxes = true; //Draw hitboxes of chunks nearby
                List<Face> faces = chunk.visibleFaces; //Get visible faces in chunk

                //If the player is in debug mode, skip this
                if (Game1.debug && this.GetType() == typeof(Player))
                {
                    continue;
                }

                //This loop checks if the entity is intersecting a face, and if so, stops its velocity in that direction.
                for(int i = 0; i < faces.Count; i++)
                {
                    //If the predicted hitbox intesects a face, then continue
                    if (predictedHitbox.Contains(faces[i].hitbox) == ContainmentType.Intersects && currentHitbox.Contains(faces[i].hitbox) != ContainmentType.Intersects)
                    {
                        if (faces[i].blockNormal.Equals(new Vector3(0,0,1))) //Checking what direction this face's normal is
                        {
                            if(velocity.Z < 0 && currentHitbox.Min.Z > faces[i].hitbox.Max.Z) // If the hitbox is within the range of this face(i.e is actually in FRONT of the face, the stop all velocity in that direction)
                            {
                                velocity.Z = 0; //Reset velocity
                            }
                        }
                        if (faces[i].blockNormal.Equals(new Vector3(0, 0, -1))) //Checking what direction this face's normal is
                        {
                            
                            if (velocity.Z > 0 && currentHitbox.Max.Z < faces[i].hitbox.Min.Z) // If the hitbox is within the range of this face(i.e is actually in FRONT of the face, the stop all velocity in that direction)
                            {
                                velocity.Z = 0; //Reset velocity
                            }
                        }
                        if (faces[i].blockNormal.Equals(new Vector3(0, 1, 0))) //Checking what direction this face's normal is
                        {
                            if (velocity.Y < 0 && currentHitbox.Min.Y > faces[i].hitbox.Max.Y) // If the hitbox is within the range of this face(i.e is actually in FRONT of the face, the stop all velocity in that direction)
                            {
                                velocity.Y = 0; //Reset velocity
                                standingBlock = faces[i]; //Set the entity's standing block
                            }
                        }
                        if(faces[i].blockNormal.Equals(new Vector3(0, -1, 0))) //Checking what direction this face's normal is
                        {
                            if (velocity.Y > 0 && currentHitbox.Max.Y < faces[i].hitbox.Min.Y) // If the hitbox is within the range of this face(i.e is actually in FRONT of the face, the stop all velocity in that direction)
                            {
                                velocity.Y = 0; //Reset velocity
                            }
                        }
                        if (faces[i].blockNormal.Equals(new Vector3(1, 0, 0))) //Checking what direction this face's normal is
                        {
                            if (velocity.X < 0 && currentHitbox.Min.X > faces[i].hitbox.Max.X) // If the hitbox is within the range of this face(i.e is actually in FRONT of the face, the stop all velocity in that direction)
                            {
                                velocity.X = 0; //Reset velocity
                            } 
                        }
                        if(faces[i].blockNormal.Equals(new Vector3(-1, 0, 0))) //Checking what direction this face's normal is
                        {
                            if (velocity.X > 0 && currentHitbox.Max.X < faces[i].hitbox.Min.X) // If the hitbox is within the range of this face(i.e is actually in FRONT of the face, the stop all velocity in that direction)
                            {
                                velocity.X = 0; //Reset velocity
                            }
                        }
                    }
                }
            }
        }

        public virtual void Draw(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera, SpriteBatch spriteBatch, SkinnedEffect skinEffect)
        {
            //If the game is in debug mode, then draw the entity's bounding box
            if (Game1.debug)
            {
                DrawBoundingBox(_graphics, basicEffect, camera); //Draw boudning box
/*                DrawView(_graphics, basicEffect, camera); //Draw view ray
*/
            }

            //Drawing the entity's model
            if(model != null)
            {
                //Set the models position, scale, and rotation
                skinEffect.World = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y) * Matrix.CreateRotationZ(rotation.Z) * Matrix.CreateTranslation(position - new Vector3(0, this.dimensions.Y / 2, 0)) *Matrix.CreateScale(1f, 1f, 1f);
                
                skinEffect.View = world.player.Camera.View; //Set view matrix
                skinEffect.Projection = world.player.Camera.Projection; //Set projection matrix

                skinEffect.EnableDefaultLighting(); //Turn on lighting so that textures are shown
                skinEffect.SpecularColor = new Vector3(0,0,0); //Turn off the shine default lighting has

                ushort lightLevel = world.GetBlockLightLevelAtWorldIndex(this.position+ new Vector3(0,1,0)); //Get the block the entity is on's light level

                //Diffuse color ranges from 0-1, so trake 1/maxlightlevel, then times that by its light level, to set its diffuse color.
                skinEffect.DiffuseColor = new Vector3(1f/(float)DataManager.maxLightLevel*(float)lightLevel, 1f / (float)DataManager.maxLightLevel * (float)lightLevel, 1f / (float)DataManager.maxLightLevel * (float)lightLevel); //Set the entity's light level

                foreach (SkinnedMesh mesh in model.Meshes)
                {
                    skinEffect.Texture = modelTexture; //Set the models texture

                    if(modelAnimation.Animation != null)
                    {
                        modelAnimation.SetEffectBones(skinEffect); //Set the animations bones
                    }

                    skinEffect.CurrentTechnique.Passes[0].Apply();

                    mesh.Draw();
                }
            }
        }

        //Draws debug ray
        private void DrawView(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera)
        {
            //Draw line
            List<VertexPositionColor> line = new List<VertexPositionColor>();
            line.Add(new VertexPositionColor(position + new Vector3(0,-1,0), Color.Blue));
            line.Add(new VertexPositionColor(position + new Vector3(0, -1, 0) + direction * 100, Color.Blue));
            VertexBuffer vertexBuffer;

            if (line.Count > 0)
            {
                vertexBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), line.Count, BufferUsage.WriteOnly);
                vertexBuffer.SetData(line.ToArray());

                basicEffect.VertexColorEnabled = true;
                basicEffect.View = camera.View;
                basicEffect.Projection = camera.Projection;
                basicEffect.World = Matrix.Identity;

                //Loop through and draw each vertex
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                    _graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, vertexBuffer.VertexCount / 2); //count is vertex / 3 since each triangle has 3 vertices
                }
            }
        }

        //Draw a bounding box
        private void DrawBoundingBox(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera)
        {
            if (vertexList.Count > 0)
            {
                basicEffect.VertexColorEnabled = true;
                basicEffect.View = camera.View;
                basicEffect.Projection = camera.Projection;
                basicEffect.World = Matrix.Identity;

                //Loop through and draw each vertex
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                    _graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, vertexBuffer.VertexCount / 2); //count is vertex / 3 since each triangle has 3 vertices
                }
            }

        }

        //buildng the entity's hitbox bounding box
        private void BuildBoundingBoxDebug()
        {
            vertexList = new List<VertexPositionColor>
                {
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red),
                    //
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red),
                    new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red)
                };

            vertexBuffer = new VertexBuffer(Game1._graphics.GraphicsDevice, typeof(VertexPositionColor), vertexList.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertexList.ToArray());
        }

        /// <summary>
        /// Method which previews the entities movement before it actually moves. This is just used for some player stuff specifically.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public virtual Vector3 PreviewMove(Vector3 amount)
        {
            //Create rotation matrix
            Matrix rotate = Matrix.CreateRotationY(rotation.Y);

            //Create movement vector
            Vector3 movement = new Vector3(amount.X, amount.Y, amount.Z);
            movement = Vector3.Transform(movement, rotate);

            //Return value of camera position + movement vector
            return position + movement;
        }

        //???
        public virtual void Move(Vector3 scale)
        {
            MoveTo(PreviewMove(scale), Rotation);
        }

        /// <summary>
        /// Moves the entity to the given postiion and rotation
        /// </summary>
        /// <param name="pos">position to be moved to</param>
        /// <param name="rot">rotation to be given</param>
        public virtual void MoveTo(Vector3 pos, Vector3 rot)
        {
            position = pos;
            rayOriginPosition = pos;
            rotation = rot;
        }

        public virtual void PostUpdate()
        {

        }
    }
}
