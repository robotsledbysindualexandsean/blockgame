using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


//A lot of this code is from: https://rtouti.github.io/graphics/perlin-noise-algorithm
//I wish I understood it better but i understand it OK which is good enough for now

namespace BlockGame.Components.World.PerlinNoise
{

    /// <summary>
    /// Old 2D perlin noise geneartor. Deprecated but useful to keep.
    /// </summary>
    internal static class Perlin
    {
        //Random values 0-255 inclusives
        private static readonly int[] permutation = { 151,160,137,91,90,15,               
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,};

        //We make a version of this that is doubled, to avoid a later buffer overflow issue
        private static readonly int[] permutation2;

        static Perlin()
        {
            //Copying permutation twice into perlin noise
            RandomizeArray(permutation);
            Debug.WriteLine(permutation[0]);
            permutation2 = new int[512];
            
            for( int i = 0; i < 512; i++)
            {
                permutation2[i] = permutation[i % 256];
            }
        }

        //perlin noise function, which returns a double (0-1) when given a coordinate
        public static float noise(Vector2 p)
        {
            int X = (int)(p.X) & 255;
            int Y = (int)(p.Y) & 255;

            //our t0 t1
            float t0 = p.X - MathF.Floor(p.X);
            float t1 = p.Y - MathF.Floor(p.Y);

            //Getting corner vectors
            Vector2 topRight = new Vector2(t0 - 1.0f, t1 - 1.0f);
            Vector2 topLeft = new Vector2(t0, t1 - 1.0f);
            Vector2 bottomRight = new Vector2(t0 - 1.0f, t1);
            Vector2 bottomLeft = new Vector2(t0, t1);

            //Getting the value of each of these corners using the permutation array
            int valueTopRight = permutation2[permutation2[X + 1] + Y + 1];
            int valueTopLeft = permutation2[permutation2[X] + Y + 1];
            int valueBottomRight = permutation2[permutation2[X + 1] + Y];
            int valueBottomLeft = permutation2[permutation2[X]+Y];

            //Calculating dot products
            float dotTopRight = Vector2.Dot(topRight, GetConstantVector(valueTopRight));
            float dotTopLeft = Vector2.Dot(topLeft, GetConstantVector(valueTopLeft));
            float dotBottomRight = Vector2.Dot(bottomRight, GetConstantVector(valueBottomRight));
            float dotBottomLeft = Vector2.Dot(bottomLeft, GetConstantVector(valueBottomLeft));

            float fade_t0 = fade(t0);
            float fade_t1 = fade(t1);

            //Return lin interp(?) of this all
            return lerp(fade_t0, lerp(fade_t1, dotBottomLeft, dotTopLeft), lerp(fade_t1, dotBottomRight, dotTopRight));
        }

        private static Vector2 GetConstantVector(int v)
        {
            int h = v & 3;
            if (h == 0)
            {
                return new Vector2(1.0f, 1.0f);
            }
            else if(h == 1)
            {
                return new Vector2(-1.0f, 1.0f);
            }
            else if (h == 2)
            {
                return new Vector2(-1.0f, -1.0f);
            }
            else
            {
                return new Vector2(1.0f, -1.0f);
            }
        }

        //Fade function
        private static float fade(float t)
        {
            return t * t * t * ((6 * t - 15) * t + 10);
        }

        //linear interpoltation
        private static float lerp(float t, float a1, float a2)
        {
            return a1 + t*(a2 - a1);
        }

        public static float[,] GeneratePerlinNoise(int length, int width, int numOctaves)
        {
            float[,] noise = new float[length, width];

            for(int x = 0; x < length; x++)
            {
                for(int y = 0; y < width; y++)
                {
                    //octaves (freq, amp ,etc)
                    float result = 0.0f;
                    float amplitude = 1.0f;
                    float frequency = 0.005f;

                    for (int octave = 0; octave < numOctaves; octave++)
                    {
                        float n = amplitude * Perlin.noise(new Vector2(x * frequency, y * frequency));
                        result += n;

                        amplitude *= 0.5f;
                        frequency *= 2.0f;
                    }

                    noise[x, y] = result;
                }
            }

            return noise;
        }

        ///returns random noise array
        public static float[,] GenerateRandomNoise(int length, int width)
        {
            float[,] noise = new float[length, width]; //generate float array with length to width, values will be from 0-1

            //Generate random number: 0 or 1, fill array
            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    noise[x, y] = Game1.rnd.NextInt64(0, 2);
                }
            }

            return noise;
        }

        //https://code-maze.com/csharp-efficiently-randomize-an-array/#:~:text=The%20OrderBy()%20method%20is,shuffle%20the%20array%20elements%20randomly.
        private static void RandomizeArray(int[] array)
        {
            int count = array.Length;
            while (count > 1)
            {
                int i = Random.Shared.Next(count--);
                (array[i], array[count]) = (array[count], array[i]);
            }
        }
    }
}
