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
        public BombItem(DataManager data, ushort itemID, Rectangle atlasRect, int maxCount, GraphicsDeviceManager graphics) : base(data, itemID, atlasRect, maxCount, graphics)
        {

        }

        public override void OnRightClick(WorldManager world, DataManager dataManager, Player player, Entity user)
        {
            Bomb bomb = new Bomb(Game1._graphics, user.position, Vector3.Up, world, dataManager);
            world.CreateEntity(bomb);

            bomb.dynamic_velocity = Vector3.Normalize(new Vector3(player.direction.X, 0.05f, player.direction.Z)) ;
            bomb.rotational_velocity = player.direction;
            base.OnRightClick(world, dataManager, player, user);
        }
    }
}
