using BlockGame.Components.Entities;
using BlockGame.Components.World.WorldTools;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Items
{
    /// <summary>
    /// Item used to place blocks
    /// </summary>
    internal class BlockItem : Item
    {
        public BlockItem(string nameID, ushort itemID, Rectangle atlasRect, int maxCount, GraphicsDeviceManager graphics) : base(nameID, itemID, atlasRect, maxCount, graphics)
        {

        }

        public override void OnRightClick(WorldManager world, Player player, Entity user)
        {
            //Is the player in range to the closest block to place something down? If so, place a block
            if (Vector3.Distance(user.position, user.ClosestFace.blockPosition) < 10)
            {
                world.SetBlockAtWorldIndex(player.ClosestFace.blockPosition + player.ClosestFace.blockNormal, 5); //Place block

                //TO DO: turn this to remove from "entity" inventory not the player.
                player.Inventory.RemoveItem(new Vector2(player.highlightedHotbarSlot, player.Inventory.GetHeight() - 1), itemID); //Remove from players inventory

                base.OnRightClick(world, player, user);
            }

        }
    }
}
