using BlockGame.Components.World.ChunkTools;
using BlockGame.Components.World.PerlinNoise;
using BlockGame.Components.World.WorldTools;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World.Dungeon
{
    //Base dungeon manager class. Other classes inherit it based on the biome type.

    internal class DungeonManager
    {
        public static Dictionary<Vector3, List<Room>> skeletonRooms = new Dictionary<Vector3, List<Room>>(); //Hashmap of all the room skeletons. They are organized by what direction they can open up, indicated by their vector3 key. Loaded in DataManager LoadRooms()
        protected WorldManager world; //Reference to world

        public void CreateDungeon(WorldManager world)
        {
            this.world = world; //Storing world reference
            PlaceRoom(skeletonRooms.Values.ToArray()[0][0], new Vector3(0, ChunkGenerator.chunkHeight / 2, 0)
                - new Vector3(skeletonRooms.Values.ToArray()[0][0].map.GetLength(0) / 2, 0, skeletonRooms.Values.ToArray()[0][0].map.GetLength(2) / 2)); //Calculations for centering map
        }

        ///Places a room in the world. Rooms are placed from most postivie XYZ position downwards. They are not cenetered.
        private void PlaceRoom(Room room, Vector3 worldPosition)
        {
            //Loop over ever block in the room and place it
            for (int x = 0; x < room.map.GetLength(0); x++)
            {
                for (int y = 0; y < room.map.GetLength(1); y++)
                {
                    for (int z = 0; z < room.map.GetLength(2); z++)
                    {
                        Vector3 block = worldPosition + new Vector3(x, y, z); //Getting block position

                        if (room.map[x, y, z] == 1) //If floor, then build floor
                        {
                            PlaceFloorBlock(block);
                        }
                        else if (room.map[x, y, z] == 2) //If wall, then build wall
                        {
                            PlaceWallBlock(block);
                        }
                        else if (room.map[x, y, z] == 3) //If roof, then build roof
                        {
                            PlaceRoofBlock(block);
                        }
                        else if (room.map[x, y, z] == 0 && world.GetBlockAtWorldIndex(block) == 1) //If air and the world has null, set the block to air
                        {
                            world.SetBlockAtWorldIndex(block, 0);
                        }
                        else if(room.map[x, y, z] != 0)
                        {
                            PlaceFloorBlock(block);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method for what to do when a floor block is present. This is virtual and code isnt run here, more in the subclasses like forest dungeon manager
        /// </summary>
        /// <param name="block"></param>
        protected virtual void PlaceFloorBlock(Vector3 block)
        {

        }

        /// <summary>
        /// Method for what to do when a wall block is present. This is virtual and code isnt run here, more in the subclasses like forest dungeon manager
        /// </summary>
        /// <param name="block"></param>
        protected virtual void PlaceWallBlock(Vector3 block)
        {

        }

        /// <summary>
        /// Method for what to do when a roof block is present. This is virtual and code isnt run here, more in the subclasses like forest dungeon manager
        /// </summary>
        /// <param name="block"></param>
        protected virtual void PlaceRoofBlock(Vector3 block)
        {

        }

    }
}
