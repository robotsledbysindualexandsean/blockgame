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
        private static Vector3 dimensions = new Vector3(50f, 50f, 50f);
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
            //Destroy blocks it collides with
            Chunk[,] chunks = world.GetChunksNearby(position, 1);

            foreach(Chunk chunk in chunks)
            {
                foreach(Face face in chunk.visibleFaces)
                {
                    if (Vector3.Distance(face.blockPosition, this.position) <= Explosion.dimensions.X / 2)
                    {
                        world.SetBlockAtWorldIndex(face.blockPosition, 0);
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
