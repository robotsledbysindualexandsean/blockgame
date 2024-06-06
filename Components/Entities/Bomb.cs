using Assimp;
using BlockGame.Components.Items;
using BlockGame.Components.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Entities
{
    internal class Bomb : Entity
    {
        private static Vector3 dimensions = new Vector3(0.5f, 0.5f, 0.5f);
        private int countDown = 60 * 3;

        public Bomb(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager) : base(position, rotation, world, dataManager, dimensions)
        {
            this.world = world;

            //Set position and rotation
            this.rotation = rotation;
            this.position = position;

        }

        public override void Update(GameTime gameTime)
        {
            countDown--;

            //Explosion
            if(countDown <= 0)
            {
                world.CreateEntity(new Explosion(Game1._graphics, position, rotation, world, dataManager));
                world.DestroyEntity(this);
            }

            base.Update(gameTime);
        }
    }
}
