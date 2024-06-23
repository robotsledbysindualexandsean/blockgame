using BlockGame.Components.Entities;
using BlockGame.Components.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Items
{
    internal class BombItem : Item
    {
        private float throwSpeed = 17f;

        public BombItem(DataManager data, ushort itemID, Rectangle atlasRect, int maxCount, GraphicsDeviceManager graphics) : base(data, itemID, atlasRect, maxCount, graphics)
        {

        }

        public override void OnRightClick(WorldManager world, DataManager dataManager, Player player, Entity user)
        {
            //When used, create a bomb entity
            Bomb bomb = new Bomb(Game1._graphics, user.position + Vector3.UnitY, Vector3.Up, world, dataManager); //Making object
            world.CreateEntity(bomb); //Adding object to world list

            bomb.dynamic_velocity = player.dynamic_velocity + new Vector3(player.direction.X * throwSpeed, player.direction.Y * throwSpeed + 5f, player.direction.Z * throwSpeed); //Giving the bomb inital velocity
            bomb.rotational_velocity = player.direction; //Giving the bomb a psuedorandom direction

            base.OnRightClick(world, dataManager, player, user);
        }
    }
}
