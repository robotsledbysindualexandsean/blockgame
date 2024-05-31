using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World.Dungeon
{
    internal class DungeonManager
    {
        private Random rnd = new Random();

        static int roomsGenereated = 0;

        //Generate a dungeon and return it in an int[,] array. Used to cut the dungeon into the world map.
        public int[,] GenerateDungeon(int width, int height)
        {
            int[,] map = GenerateEmpty(width, height);

            PlaceRoom(map, Room.roomObjs[0], new Vector2(width / 2, height / 2), Vector2.Zero);

            return map;

        }

        public void PlaceRoom(int[,] map, Room room, Vector2 index, Vector2 enteringDirection)
        {
            roomsGenereated++;

            int roomColumns = room.layout.GetLength(1);
            int roomRows = room.layout.GetLength(0);

            if (CheckOverlap(map, room.layout, index))
            {
                return;
            }

            for (int row = 0; row < roomRows; row++)
            {
                for (int column = 0; column < roomColumns; column++)
                {
                    map[row + (int)index.Y, column + (int)index.X] = room.layout[row, column];
                }
            }

            //To bottom
            if (room.downDoor != Vector2.Zero && !enteringDirection.Equals(new Vector2(0, 1)))
            {
                Room newRoom = Room.upRooms[rnd.Next(0, Room.upRooms.Length)];
                PlaceRoom(map, newRoom, room.downDoor + index - new Vector2(newRoom.upDoor.X, -1), new Vector2(0, -1));
            }
            //To left
            if (room.leftDoor != Vector2.Zero && !enteringDirection.Equals(new Vector2(-1, 0)))
            {
                Room newRoom = Room.rightRooms[rnd.Next(0, Room.rightRooms.Length)];
                PlaceRoom(map, newRoom, index + room.leftDoor - new Vector2(newRoom.layout.GetLength(1), newRoom.rightDoor.Y), new Vector2(1, 0));
            }
            //To right
            if (room.rightDoor != Vector2.Zero && !enteringDirection.Equals(new Vector2(1, 0)))
            {
                Room newRoom = Room.leftRooms[rnd.Next(0, Room.leftRooms.Length)];
                PlaceRoom(map, newRoom, index + room.rightDoor - new Vector2(-1, newRoom.leftDoor.Y), new Vector2(-1, 0));
            }
            //To top
            if (room.upDoor != Vector2.Zero && !enteringDirection.Equals(new Vector2(0, -1)))
            {
                Room newRoom = Room.downRooms[rnd.Next(0, Room.downRooms.Length)];
                PlaceRoom(map, newRoom, index + room.upDoor - new Vector2(newRoom.downDoor.X, newRoom.layout.GetLength(0)), new Vector2(0, 1));
            }


        }

        private bool CheckOverlap(int[,] map, int[,] room, Vector2 index)
        {
            int[,] tempMap = map.Clone() as int[,];

            //Place room
            int roomRows = room.GetLength(0);
            int roomColumns = room.GetLength(1);

            bool overlap = false;

            for (int y = 0; y < roomRows; y++)
            {
                for (int x = 0; x < roomColumns; x++)
                {

                    if (x < 0 || y + (int)index.Y >= tempMap.GetLength(0) || x + (int)index.X < 0)
                    {
                        return true;
                    }
                    if (y < 0 || x + (int)index.X >= tempMap.GetLength(1) || y + (int)index.Y < 0)
                    {
                        return true;
                    }
                    if (tempMap == null || room == null)
                    {
                        return true;
                    }

                    if (tempMap[y + (int)index.Y, x + (int)index.X] != 0 && room[y, x] != 0)
                    {
                        return true;
                    }

                    /*                  *//*  Debug.WriteLine(x + (int)index.X);
                                        Debug.WriteLine(y + (int)index.Y);*//*
                                        tempMap[ y + (int)index.Y, x + (int)index.X] += room[y, x];

                                        if(tempMap[y + (int)index.Y, x + (int)index.X] == 0 || tempMap[y + (int)index.Y, x + (int)index.X] == 1 || tempMap[y + (int)index.Y, x + (int)index.X] == 3 | tempMap[y + (int)index.Y, x + (int)index.X] == 4)
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            return true;
                                        }*/
                }
            }
            return false;
        }

        public int[,] GenerateEmpty(int width, int height)
        {
            int[,] map = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    map[x, y] = 0;
                }
            }

            return map;
        }
    }
}

