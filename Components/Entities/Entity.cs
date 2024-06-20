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
        //Transforms
        public Vector3 position;
        protected Vector3 lastPosition;
        public Vector3 rotation;
        public Vector3 fixed_rotation_based_velocity; //Fixed rotation, based on entities facing direction (for fixed "forward" control)
        public Vector3 rotational_velocity = Vector3.Zero; //Velocity in terms of entities rotation
        public Vector3 dynamic_velocity = Vector3.Zero; //Dynamic velocity caused by "forces". This slowly dissipates to 0.
        public Vector3 velocity;
        private Vector3 dimensions;

        //Storing World variable
        public WorldManager world;
        public DataManager dataManager;

        public float speed = 5f;

        //Collision Variables
        public BoundingBox hitbox;
        private Ray viewRay;
        public List<Face> rayFaces = new List<Face>();
        public Vector3 direction;
        private Vector3 forward = new Vector3(0,0,1);
        protected Vector3 rayOriginPosition; //Where the ray is shot from (this is because position =/= camera for player)

        //Is this affected by gravity?
        protected bool enforceGravity = true;

        //Closest things:
        protected Face closestFace;

        //Standing Block
        // Stores a reference to what block is being stood on. So that when the only velocity is gravity, collision is not checked if still stnading on that block.
        private Face standingBlock;

        //Debug
        private List<VertexPositionColor> vertexList = new List<VertexPositionColor>(); //debug hitbox list
        private VertexBuffer vertexBuffer;

        //Rendering
        protected SkinnedModel model;
        protected Texture2D modelTexture;
        protected AnimationPlayer modelAnimation;

        public Face ClosestFace
        {
            get { return closestFace; }
        }

        public virtual Vector3 Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
               
            }
        }

        public virtual Vector3 Position
        {
            get { return position; }
        }

        public Entity(Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager, Vector3 dimensions)
        {
            this.position = position;
            this.rotation = rotation;
            this.world = world;
            this.dataManager = dataManager;
            this.dimensions = dimensions;

            //Setting up inital hitbox and ray
            hitbox = new BoundingBox(new Vector3(position.X - dimensions.X / 2, position.Y - dimensions.Y /2, position.Z - dimensions.Z /2), new Vector3(position.X + dimensions.X / 2, position.Y+dimensions.Y / 2, position.Z + dimensions.Z / 2));
            viewRay = new Ray(position, rotation);
        }

        public virtual void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            lastPosition = position;

            //YAW PTICH ROLL! Get direction using Vector3 rotation
            Matrix yawPitchRoll = Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
            direction = Vector3.Transform(forward, yawPitchRoll);

            //Do rotation
            rotation += rotational_velocity;

            //reduce dynamic velocity
            dynamic_velocity.X /= 1.25f;
            dynamic_velocity.Z /= 1.25f;
            rotational_velocity /= 1.25f;

            //Enforce gravity
            if (dynamic_velocity.Y >= -0.1 && enforceGravity)
            {
                dynamic_velocity.Y -= 0.2f * deltaTime;
            }

            //Velocity is made of two components, dynamic (applied forces) and fixed (movement). These are added together once both are calculated.
            velocity = dynamic_velocity + fixed_rotation_based_velocity;

            //Check if the entity is still colliding with theyre standing block. If so, then just quickly reset  Y velocity that to 0 to avoid collision checks.
            BoundingBox tempHitbox = new BoundingBox(new Vector3(hitbox.Min.X + velocity.X, hitbox.Min.Y + velocity.Y, hitbox.Min.Z + velocity.Z), new Vector3(hitbox.Max.X + velocity.X, hitbox.Max.Y + velocity.Y, hitbox.Max.Z + velocity.Z));
            if (standingBlock != null && tempHitbox.Intersects(standingBlock.hitbox))
            {
                velocity.Y = 0;
            }

            //Only update collision is there is velocity
            if (velocity != Vector3.Zero)
            {
                standingBlock = null;

                //Apply the velocity to a temporary hitbox and then check the collisions on that. This will stop movement that goes into a block collider.
                CollideBlocks(hitbox, tempHitbox);

                //Move using velocity
                MoveTo(velocity + position, Rotation);
                hitbox = new BoundingBox(new Vector3(position.X - dimensions.X / 2, position.Y - dimensions.Y / 2, position.Z - dimensions.Z / 2), new Vector3(position.X + dimensions.X / 2, position.Y + dimensions.Y / 2, position.Z + dimensions.Z / 2));

                //Debug update bounding box
                if (!lastPosition.Equals(position))
                {
                    OnMovement();
                    BuildBoundingBoxDebug();
                }
            }

            //Update animation
            if(modelAnimation != null)
            {
                modelAnimation.Update(gameTime);
            }

            PostUpdate();
        }

        ///Loading a model
        protected virtual void LoadModel()
        {

        }

        /// <summary>
        /// Method should something special happen to the entity on it's movement.
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
            if (rayFaces.Count <= 0)
            {
                return;
            }

            //Get the closest face to the player
            closestFace = rayFaces[0];

            for (int i = 0; i < rayFaces.Count; i++)
            {
                if (Vector3.Distance(position, rayFaces[i].hitbox.Max) < Vector3.Distance(position, closestFace.hitbox.Max))
                {
                    closestFace = rayFaces[i];
                }
            }
        }

        /// <summary>
        /// Method which updates this entities list of faces it is hitting.
        /// This is NOT called on base entity update, it will only be called in higher classes
        /// This is because not all entities need this info.
        /// </summary>
        public void RaySight()
        {
            //Cast a ray in the entities direction
            viewRay = new Ray(rayOriginPosition, direction);
            Chunk[,] chunks = world.GetChunksNearby(position, 1);

            //List of faces hit by the entitys look ray
            rayFaces = new List<Face>();

            //If the ray hits a block in the chunks, add that face to the list of faces
            foreach (Chunk chunk in chunks)
            {

                if (chunk == null)
                {
                    continue;
                }

                List<Face> faces = chunk.visibleFaces;

                for(int i = 0; i < faces.Count; i++)
                {
                    if (viewRay.Intersects(faces[i].hitbox) != null)
                    {
                        rayFaces.Add(faces[i]);

                    }
                }
            }
        }

        /// <summary>
        /// Method which checks collisions with blocks, and then stop movement if it hits a block.
        /// Takes the current hitbox of the entity as well as the predicted hitbox once movement is applied.
        /// </summary>
        /// <param name="currentHitbox"></param>
        /// <param name="predictedHitbox"></param>
        public void CollideBlocks(BoundingBox currentHitbox, BoundingBox predictedHitbox) {
            Chunk[,] chunks = world.GetChunksNearby(position, 1);

            foreach (Chunk chunk in chunks)
            {
                if(chunk == null)
                {
                    continue;
                }

                chunk.drawHitboxes = true;
                List<Face> faces = chunk.visibleFaces;

                if (Game1.debug && this.GetType() == typeof(Player))
                {
                    continue;
                }

                //For each block, check if the hitbox is intersecting it AND if it is within it's range (i.e is accurately above the block and hitting a top). Update the velocity based on normal data
                for(int i = 0; i < faces.Count; i++)
                {
                    if (predictedHitbox.Contains(faces[i].hitbox) == ContainmentType.Intersects && currentHitbox.Contains(faces[i].hitbox) != ContainmentType.Intersects)
                    {
                        //If the faces normal is in this direction, then reset the velocity in that direction
                        if (faces[i].blockNormal.Equals(new Vector3(0,0,1)) )
                        {
                            //Reset velocity to 0 in that direction
                            if(velocity.Z < 0 && currentHitbox.Min.Z > faces[i].hitbox.Max.Z)
                            {
                                velocity.Z = 0;
                            }
                        }
                        if (faces[i].blockNormal.Equals(new Vector3(0, 0, -1)))
                        {
                            //Reset velocity to 0 in that direction
                            if (velocity.Z > 0 && currentHitbox.Max.Z < faces[i].hitbox.Min.Z)
                            {
                                velocity.Z = 0;
                            }
                        }
                        if (faces[i].blockNormal.Equals(new Vector3(0, 1, 0)))
                        {
                            //Reset velocity to 0 in that direction
                            if (velocity.Y < 0 && currentHitbox.Min.Y > faces[i].hitbox.Max.Y)
                            {
                                velocity.Y = 0;
                                standingBlock = faces[i];
                            }
                        }
                        if(faces[i].blockNormal.Equals(new Vector3(0, -1, 0)))
                        {
                            //Reset velocity to 0 in that direction
                            if (velocity.Y > 0 && currentHitbox.Max.Y < faces[i].hitbox.Min.Y)
                            {
                                velocity.Y = 0;
                            }
                        }
                        if (faces[i].blockNormal.Equals(new Vector3(1, 0, 0)))
                        {
                            //Reset velocity to 0 in that direction
                            if (velocity.X < 0 && currentHitbox.Min.X > faces[i].hitbox.Max.X)
                            {
                                velocity.X = 0;
                            } 
                        }
                        if(faces[i].blockNormal.Equals(new Vector3(-1, 0, 0)))
                        {
                            //Reset velocity to 0 in that direction
                            if (velocity.X > 0 && currentHitbox.Max.X < faces[i].hitbox.Min.X)
                            {
                                velocity.X = 0;
                            }
                        }
                    }
                }
            }
        }

        public virtual void Draw(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera, SpriteBatch spriteBatch, SkinnedEffect skinEffect)
        {
            if (Game1.debug)
            {
                DrawBoundingBox(_graphics, basicEffect, camera);
/*                DrawView(_graphics, basicEffect, camera);
*/
            }

            //Draw model
            if(model != null)
            {
                skinEffect.World = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y) * Matrix.CreateRotationZ(rotation.Z) * Matrix.CreateTranslation(position - new Vector3(0, this.dimensions.Y / 2, 0)) *Matrix.CreateScale(1f, 1f, 1f);
                skinEffect.View = world.player.Camera.View;
                skinEffect.Projection = world.player.Camera.Projection;
                skinEffect.EnableDefaultLighting();
                skinEffect.DirectionalLight0.Enabled = true;
                skinEffect.SpecularColor = new Vector3(0,0,0); //turn off shine (kinda works)

                //Set the models lighting to be its current blocks lighting
                ushort lightLevel = world.GetBlockLightLevelAtWorldIndex(this.position+ new Vector3(0,1,0));
                //Diffuse color ranges from 0-1, so trake 1/maxlightlevel, then times that by its light level, to set its diffuse color.
                skinEffect.DiffuseColor = new Vector3(1f/(float)DataManager.maxLightLevel*(float)lightLevel, 1f / (float)DataManager.maxLightLevel * (float)lightLevel, 1f / (float)DataManager.maxLightLevel * (float)lightLevel); 


                foreach (SkinnedMesh mesh in model.Meshes)
                {
                    skinEffect.Texture = modelTexture;

                    if(modelAnimation.Animation != null)
                    {
                        modelAnimation.SetEffectBones(skinEffect);
                    }

                    skinEffect.CurrentTechnique.Passes[0].Apply();

                    mesh.Draw();
                }
            }
        }

        //Drawing view ray (debug)
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

        //Draw a bounding box (this actually works for any in theory but is spedcifically for hitboxes right now...)
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

        //method that simulates movement
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

        //Actually moving the camera
        public virtual void Move(Vector3 scale)
        {
            MoveTo(PreviewMove(scale), Rotation);
        }

        //Method that sets camera pos and rot
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
