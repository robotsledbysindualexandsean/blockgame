using BlockGame.Components.World.WorldTools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Entities
{
    /// <summary>
    /// Dropped item entity, which are items on the floor which are able to be picked up by the player/other entities.
    /// </summary>
    internal class DroppedItem : Entity
    {
        public static List<DroppedItem> droppedItems = new List<DroppedItem>(); //List containing all the dropped items. Used to iterate through and group items together.

        private static Vector3 dimensions = new Vector3(0.25f, 0.25f, 0.25f); //Dimensions for the hitbox, passed to entity constructor.
        private static float pickupRange = 1.5f; //Range that entities must be in to pick up the item
        private ushort itemID; //What item this entity "is", i.e what item is dropped.
        public ushort stack = 0; //How many items is in this entities stack

        public DroppedItem(GraphicsDeviceManager _graphics, Vector3 position, Vector3 rotation, WorldManager world, ushort itemID) : base(position, rotation, world, dimensions)
        {
            this.rotation = rotation; //Set rotation
            this.position = position; //Set position

            this.itemID = itemID; //Set what item the dropped item entity holds

            droppedItems.Add(this); //Add to the list of dropped items
            stack++; //Increment the stack by one to start


        }

        /// <summary>
        /// Update method
        /// </summary>
        /// <param name="gameTime">Stores time relating to the games frames</param>
        public override void Update(GameTime gameTime)
        {
            //If the player is in range of the item, pick it up
            if (Vector3.Distance(this.position, world.player.position) < pickupRange)
            {
                world.player.Inventory.AddItem(itemID, stack); //Add to players inventory
                world.DestroyEntity(this); //Destroy the dropped item entity (this)
                droppedItems.Remove(this); //Remove this dropped item from the items list
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the item
        /// </summary>
        /// <param name="_graphics">Graphics</param>
        /// <param name="basicEffect">BasicEffect</param>
        /// <param name="camera">Camera</param>
        /// <param name="spriteBatch">SpriteBatch</param>
        /// <param name="skinEffect">SkinEffect</param>
        public override void Draw(BasicEffect basicEffect, Camera camera, SpriteBatch spriteBatch, SkinnedEffect skinEffect)
        {
            base.Draw(basicEffect, camera, spriteBatch, skinEffect);
        }

        /// <summary>
        /// What should happen to the entity when its moved.
        /// </summary>
        protected override void OnMovement()
        {
            BuildVertexBuffer(); //Rebuild the entities Vertex Buffer

            base.OnMovement();
        }

        /// <summary>
        /// Building the item vertex buffer for rendering
        /// </summary>
        private void BuildVertexBuffer()
        {

        }

        /// <summary>
        /// Static method which drops an item in the world. This handles grouping items together which are nearby.
        /// </summary>
        /// <param name="position">What position the item should be placed</param>
        /// <param name="itemID">What item is dropped</param>
        /// <param name="world">World</param>
        public static void DropItem(Vector3 position, ushort itemID, WorldManager world)
        {
            //Checking if there is a dropped item nearby to group to
            foreach(DroppedItem item in droppedItems.ToList())
            {
                if(Vector3.Distance(item.position, position) < pickupRange) //If there is a dropped item within range...
                {
                    item.stack++; //Increase its stack
                    return; //Stop checking
                }
            }

            world.CreateEntity(new DroppedItem(Game1._graphics, position, Vector3.UnitY, world, 1)); //If method hasn't returned yet (no suitable stack to add to), make a new one
        }
    }
}
