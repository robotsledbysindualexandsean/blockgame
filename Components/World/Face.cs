using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World
{
    internal class Face
    {
        public BoundingBox hitbox;
        public Vector3 blockNormal;
        public Vector3 blockPosition;

        public Face(Vector3 blockPosition, BoundingBox hitbox, Vector3 blockNormal )
        {
            this.blockPosition = blockPosition;
            this.hitbox = hitbox;
            this.blockNormal = blockNormal;
        }
    }
}
