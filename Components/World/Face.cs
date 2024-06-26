using Assimp;
using BlockGame.Components.Entities;
using BlockGame.Components.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World
{
    /// <summary>
    /// A face object which stores important data used for collisions, rendering etc.
    /// Tells the hitbox, the normal of the block, and the position.
    /// This is only used for visible faces.
    /// </summary>
    internal class Face
    {
        public BoundingBox hitbox; //Face htibox

        public Vector3 blockNormal; //What direction the face is looking

        public Vector3 blockPosition; //Face's block's position

        public float distanceToPlayer; //storing distance to player. Used for transperency depth.

        public bool hasCollision;

        public Face(Vector3 blockPosition, BoundingBox hitbox, Vector3 blockNormal)
        {
            this.blockPosition = blockPosition;
            this.hitbox = hitbox;
            this.blockNormal = blockNormal;
        }

        //Constructor for no hitbox (used for rendering
        public Face(Vector3 blockPosition, Vector3 blockNormal, Player player)
        {
            this.blockPosition = blockPosition;
            this.blockNormal = blockNormal;
            this.distanceToPlayer = Vector3.Distance(blockPosition + blockNormal, player.position);
        }

    }
}
