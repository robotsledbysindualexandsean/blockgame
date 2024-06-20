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
    internal class DroppedItem : Entity
    {
        //Listr containing all dropped items so grouping is easier
        public static List<DroppedItem> droppedItems = new List<DroppedItem>();

        private static Vector3 dimensions = new Vector3(0.25f, 0.25f, 0.25f);
        private static float pickupRange = 1.5f;
        private ushort itemID; //What item is dropped

        //how much is in this stack
        public ushort stack = 0;

        //Rendering
        private VertexBuffer vertexBuffer;

        public DroppedItem(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, DataManager dataManager, ushort itemID) : base(position, rotation, world, dataManager, dimensions)
        {
            this.world = world;

            //Set position and rotation
            this.rotation = rotation;
            this.position = position;

            this.itemID = itemID;

            //Index this to check for grouping
            droppedItems.Add(this);
            stack++;


        }

        public override void Update(GameTime gameTime)
        {
            //Pick up
            if (Vector3.Distance(this.position, world.player.position) < pickupRange)
            {
                world.player.Inventory.AddItem(itemID, stack);
                world.DestroyEntity(this);
                droppedItems.Remove(this);
            }

            base.Update(gameTime);
        }

        public override void Draw(GraphicsDeviceManager _graphics, BasicEffect basicEffect, Camera camera, SpriteBatch spriteBatch, SkinnedEffect skinEffect)
        {
            //Drawing 2D model



            base.Draw(_graphics, basicEffect, camera, spriteBatch, skinEffect);
        }

        protected override void OnMovement()
        {
            //Rebuild vb
            BuildVertexBuffer();

            base.OnMovement();
        }

        private void BuildVertexBuffer()
        {

        }

        //Static method to drop an tiem in a world.
        //Drop an item (this handles grouping, so that only new objects are made if none are nearby)
        public static void DropItem(Vector3 position, ushort itemID, WorldManager world)
        {
            //Check grouping
            foreach(DroppedItem item in droppedItems.ToList())
            {
                if(Vector3.Distance(item.position, position) < pickupRange)
                {
                    item.stack++;
                    return;
                }
            }

            //If method hasn't returned yet (no suitable stack to add to), make a new one
            world.CreateEntity(new DroppedItem(Game1._graphics, position, Vector3.UnitY, world, world.dataManager, 1));

        }
    }
}
