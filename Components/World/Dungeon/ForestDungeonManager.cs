using BlockGame.Components.World.PerlinNoise;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World.Dungeon
{
    internal class ForestDungeonManager : DungeonManager
    { 
        /// <summary>
        /// Place the grass floor on this block
        /// </summary>
        /// <param name="block"></param>
        protected override void PlaceFloorBlock(Vector3 block)
        {
            float perlinOffset = Perlin.GeneratePerlinValueAtPoint(new Vector2(block.X, block.Z), 5); //Generate a perlin noise output for this block
            perlinOffset *= 5; //increase the offset value

            for(int i = 0; i < perlinOffset; i++)
            {
                world.SetBlockAtWorldIndex(block + new Vector3(0, i, 0), blockNameID: "dirt");
            }

            int rnd = Game1.rnd.Next(0, 15); //0-14. 14 is glowing grass
            if(rnd == 14)
            {
                world.SetBlockAtWorldIndex(block + new Vector3(0, perlinOffset, 0), blockNameID: "glowing_grass_block");
            }
            else
            {
                world.SetBlockAtWorldIndex(block + new Vector3(0, perlinOffset, 0), blockNameID: "grass_block");
            }
            base.PlaceWallBlock(block);
        }

        /// <summary>
        /// Places vines coming down
        /// </summary>
        /// <param name="block"></param>
        protected override void PlaceRoofBlock(Vector3 block)
        {
            int maxVineLength = 5;
            int vineLength = Game1.rnd.Next(1, maxVineLength); //Get a random number from 1 - max vine length

            //Place vine blocks based on the length downwards
            for(int i = 0; i < vineLength; i++)
            {
                int rnd = Game1.rnd.Next(0, 15); //Generate a random number from 0-15. 15 is glowing vine, 1 is regular vine

                if(rnd == 14) //if 4 was generated, place glowing vine
                {
                    world.SetBlockAtWorldIndex(block - new Vector3(0, i, 0), blockNameID: "glowing_vines"); //Place glowing vine
                }
                else //Else, place a regular vine
                {
                    world.SetBlockAtWorldIndex(block - new Vector3(0, i, 0), blockNameID: "vines"); //Place glowing vine
                }
            }

            base.PlaceWallBlock(block);
        }

        /// <summary>
        /// Places wall block (random stone)
        /// </summary>
        /// <param name="block"></param>
        protected override void PlaceWallBlock(Vector3 block)
        {
            int rnd = Game1.rnd.Next(0, 2); //Generate random number 0 to 1

            if(rnd == 0) //If 0, place cobble
            {
                world.SetBlockAtWorldIndex(block, blockNameID: "cobblestone");
            }
            else //If 1, then place sparse boble
            {
                world.SetBlockAtWorldIndex(block, blockNameID: "sparse_cobblestone");
            }

            base.PlaceWallBlock(block);
        }
    }
}
