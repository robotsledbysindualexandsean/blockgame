using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.Items
{
    internal class Item
    {
        public string id;
        private Vector2 texturePos;
        public int maxCount;

        public Item(string id, Vector2 texturePos, int maxCount)
        {
            this.id = id;
            this.texturePos = texturePos;
            this.maxCount = maxCount;
        }
    }
}
