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

namespace BlockGame.Components.Entity
{
    internal class Entity
    {
        //Transforms
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 fixed_rotation_based_velocity;
        public Vector3 dynamic_velocity;
        public Vector3 velocity;

        //Storing World variable
        public WorldManager world;
        public DataManager dataManager;

        public float speed = 5f;

        //Collision Variables
        private BoundingBox hitbox;
        private Ray viewRay;
        protected List<Face> rayFaces;
        private Vector3 direction;
        private Vector3 forward = new Vector3(0,0,1);


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

        public Entity(Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager)
        {
            this.position = position;
            this.rotation = rotation;
            this.world = world;
            this.dataManager = dataManager;

            //Setting up inital hitbox and ray
            hitbox = new BoundingBox(new Vector3(position.X - 0.25f, position.Y - 1.25f, position.Z - 0.25f), new Vector3(position.X + 0.25f, position.Y+0.25f, position.Z + 0.25f));
            viewRay = new Ray(position, rotation);
        }

        public virtual void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; 


            //Enforce gravity
            if (dynamic_velocity.Y >= -0.1 && Game1.debug == false)
            {
                dynamic_velocity.Y -= 0.2f * deltaTime;
            }


            //Velocity is made of two components, dynamic (applied forces) and fixed (movement). These are added together once both are calculated.
            velocity = dynamic_velocity + fixed_rotation_based_velocity;

            //Apply the velocity to a temporary hitbox and then check the collisions on that. This will stop movement that goes into a block collider.
            BoundingBox tempHitbox = new BoundingBox(new Vector3(hitbox.Min.X + velocity.X, hitbox.Min.Y + velocity.Y, hitbox.Min.Z + velocity.Z), new Vector3(hitbox.Max.X + velocity.X, hitbox.Max.Y + velocity.Y, hitbox.Max.Z + velocity.Z));
            CollideBlocks(hitbox, tempHitbox);

            //Move using velocity
            MoveTo(velocity + position, Rotation);
            hitbox = new BoundingBox(new Vector3(position.X - 0.25f, position.Y - 1.25f, position.Z - 0.25f), new Vector3(position.X + 0.25f, position.Y + 0.25f, position.Z + 0.25f));

            PostUpdate();
        }

        //Method which updates the entities seen hitboxes of the ray. Todo: Make this return the list, rather than just updating a made variable.
        public void RaySight()
        {
            //YAW PTICH ROLL! Get direction using Vector3 rotation
            Matrix yawPitchRoll = Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
            direction = Vector3.Transform(forward, yawPitchRoll);

            //Cast a ray in the entities direction
            viewRay = new Ray(position, direction);
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

                if (Game1.debug)
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

        public virtual void Draw(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera, SpriteBatch spriteBatch)
        {
            if (Game1.debug)
            {
                DrawBoundingBox(_graphics, basicEffect, camera);
                DrawView(_graphics, basicEffect, camera);

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
            List<VertexPositionColor> vertexList = new List<VertexPositionColor>();

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));
            //
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Min.Y, hitbox.Max.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Min.X, hitbox.Max.Y, hitbox.Max.Z), Color.Red));

            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Min.Y, hitbox.Min.Z), Color.Red));
            vertexList.Add(new VertexPositionColor(new Vector3(hitbox.Max.X, hitbox.Max.Y, hitbox.Min.Z), Color.Red));

            //Building vertex buffer for it and drawing every frame (since movmenet every frame basically)
            VertexBuffer vertexBuffer;

            if (vertexList.Count > 0)
            {
                vertexBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), vertexList.Count, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertexList.ToArray());

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
            rotation = rot;
        }

        public virtual void PostUpdate()
        {

        }
    }
}
