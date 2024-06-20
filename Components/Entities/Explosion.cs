using BlockGame.Components.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Entities
{
    internal class Explosion : Entity
    {
        private static Vector3 dimensions = new Vector3(25, 25, 25);
        private int duration = 60 * 1;

        public Explosion(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager) : base(position, rotation, world, dataManager, dimensions)
        {
            this.world = world;
            this.enforceGravity = false;

            //Set position and rotation
            this.rotation = rotation;
            this.position = position;

        }

        public override void Update(GameTime gameTime)
        {
            //Check all blocks within the radius, destroy them
            for (int x = (int)-dimensions.X / 2; x <= dimensions.X / 2; x++)
            {
                for (int y = (int)-dimensions.Y / 2; y <= dimensions.Y / 2; y++)
                {
                    for (int z = (int)-dimensions.Z / 2; z <= dimensions.Z / 2; z++)
                    {
                        //A distance check is performed to give a circular "radius" rather than a square
                        Vector3 targetedBlock = this.position + new Vector3(x, y, z);
                        if (world.GetBlockAtWorldIndex(targetedBlock) != 0 &&  Vector3.Distance(this.position, targetedBlock) <= Explosion.dimensions.X / 2)
                        {
                            dataManager.blockData[world.GetBlockAtWorldIndex(targetedBlock)].Destroy(world, targetedBlock);

                        }
                    }
                }
            }

            //Duration update
            duration--;

            if(duration <= 0)
            {
                world.DestroyEntity(this);
            }

            base.Update(gameTime);
        }
    }
}
