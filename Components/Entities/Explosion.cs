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
        private static Vector3 dimensions = new Vector3(5, 5, 5); //Hitbox dimensions
        private int duration = 60 * 1; //How long the explosion is up. In frames.

        public Explosion(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager) : base(position, rotation, world, dataManager, dimensions)
        {
            this.enforceGravity = false; //Turn off gravity for this entity

            this.rotation = rotation; //Set rotation
            this.position = position; //Set position
            
        }

        public override void Update(GameTime gameTime)
        {
            //Check all blocks around the position, destroy them if they are witin the radius
            for (int x = (int)-dimensions.X / 2; x <= dimensions.X / 2; x++)
            {
                for (int y = (int)-dimensions.Y / 2; y <= dimensions.Y / 2; y++)
                {
                    for (int z = (int)-dimensions.Z / 2; z <= dimensions.Z / 2; z++)
                    {
                        Vector3 targetedBlock = this.position + new Vector3(x, y, z); //Get the targeted block vector

                        //Check if there is a block at the targeted block vector, and if it is in the explosions radius. This gives the "circular" explosion
                        if (world.GetBlockAtWorldIndex(targetedBlock) != 0 &&  Vector3.Distance(this.position, targetedBlock) <= Explosion.dimensions.X / 2)
                        {
                            dataManager.blockData[world.GetBlockAtWorldIndex(targetedBlock)].Destroy(world, targetedBlock); //Destroy the block
                        }
                    }
                }
            }

            duration--; //Lower the explosions duration timer

            if(duration <= 0)
            {
                world.DestroyEntity(this); //If duration is at 0, destroy the explosion entity
            }

            base.Update(gameTime);
        }
    }
}
