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
            world.CreateEntity(new Bomb(Game1._graphics, user.position, Vector3.Up, world, dataManager));
            base.OnRightClick(world, dataManager, player, user);
        }
    }
}
