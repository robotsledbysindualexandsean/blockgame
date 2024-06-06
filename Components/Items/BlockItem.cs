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
    internal class BlockItem : Item
    {
        //Items that are stored in item id hashmap, storing their id, where theyre texture is on the atlas, and theyre max count.
        public BlockItem(DataManager data, ushort itemID, Rectangle atlasRect, int maxCount, GraphicsDeviceManager graphics) : base(data, itemID, atlasRect, maxCount, graphics)
        {

        }

        public override void OnRightClick(WorldManager world, DataManager dataManager, Player player, Entity user)
        {
            //Check distance
            if(Vector3.Distance(user.position, user.ClosestFace.blockPosition) < 10)
            {

                ///Place a block on the closest blocks normal
                world.SetBlockAtWorldIndex(player.ClosestFace.blockPosition + player.ClosestFace.blockNormal, 5);

                //Remove that block from the players inventory
                player.Inventory.RemoveItem(new Vector2(player.highlightedHotbarSlot, player.Inventory.GetHeight() - 1), itemID);
                base.OnRightClick(world, dataManager, player, user);
            }

        }
    }
}
