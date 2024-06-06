using BlockGame.Components.Entity;
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
        public BlockItem(DataManager data, ushort itemID, Rectangle atlasRect, int maxCount) : base(data, itemID, atlasRect, maxCount)
        {

        }

        public BlockItem(DataManager data, ushort itemID) : base(data, itemID)
        {

        }

        public override void OnRightClick(WorldManager world, DataManager dataManager, Player player)
        {
            ///Place a block on the closest blocks normal
            world.SetBlockAtWorldIndex(player.ClosestFace.blockPosition + player.ClosestFace.blockNormal, 5);

            //Remove that block from the players inventory
            player.Inventory.RemoveItem(new Vector2(player.highlightedHotbarSlot, player.Inventory.GetHeight()-1), itemID);
            base.OnRightClick(world, dataManager, player);
        }
    }
}
