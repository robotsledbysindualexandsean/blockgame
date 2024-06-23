using Assimp;
using BlockGame.Components.Items;
using BlockGame.Components.World;
using Liru3D.Animations;
using Liru3D.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Entities
{
    /// <summary>
    /// Bomb entity. Has a timer which, when it hits 0, explodes.
    /// </summary>
    internal class Bomb : Entity
    {
        private static Vector3 dimensions = new Vector3(0.5f, 0.5f, 0.5f); //Stores the dimensions of the hitbox for this specific entity. Passed to Entity constructor.

        private int countDown = 60 * 3; //Countdown to bomb explosion. Currently stored in frames.

        public Bomb(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager) : base(position, rotation, world, dataManager, dimensions)
        {
            this.rotation = rotation; //Set position
            this.position = position; //Set rotation
            elasticity = 0.2f;

            LoadModel(); //Loads the entities model into the superclass Entity variables
        }

        /// <summary>
        /// Loads the entities modes into the superclass Entity variables.
        /// If an entity is meant to have a model, this function must be present and run on construction.
        /// </summary>
        protected override void LoadModel()
        {
            model = dataManager.models["bomb"]; //Loads model
            modelTexture = dataManager.modelTextures["bomb"]; //Loads texture

            modelAnimation = new AnimationPlayer(model); //Creates animation player. This is necessary for all models.
            modelAnimation.Animation = null; //Set to have no animations.

            base.LoadModel();
        }

        /// <summary>
        /// Updates the entity
        /// </summary>
        /// <param name="gameTime">Stores the game's data regarding times between frames</param>
        public override void Update(GameTime gameTime)
        {
            countDown--; //Lowers countdown once per frame

            //When the countdown hits 0, create the explosion.
            if(countDown <= 0)
            {
                world.CreateEntity(new Explosion(Game1._graphics, position, rotation, world, dataManager)); //Create the explpsion entity
                world.DestroyEntity(this); //Destroy this (bomb) entity
            }

            base.Update(gameTime);
        }
    }
}
