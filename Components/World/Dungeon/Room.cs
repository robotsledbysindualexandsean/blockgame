using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World.Dungeon
{
    //Room object stores data regarding room skeletons
    internal class Room
    {
        public string id;
        public List<int[]> doors;
        public ushort[,,] map;

        public Room()
        {

        }
    }
}
