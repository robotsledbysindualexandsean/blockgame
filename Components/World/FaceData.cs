using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components.World
{
    //Clas which stores specific face data for the singualr instane of each block type. This is to clean up the code so 6 face animation, counter, frame, etc. data isnt all stored in the block.cs
    internal class FaceData
    {
        public int animationCounter = 0; //counter for the animation frames
        public Vector2 startCoordinate = -Vector2.One; //starting coordinate for the block UV texture (-1,-1 for undefined)
        public string[] animationFrames; //array storing the ids of each frame in the animation

        //Glow
        public int animationCounterGlow = 0; //counter for the animation frames (glow)
        public Vector2 startCoordinateGlow = -Vector2.One; //starting coordinate for the block UV texture (glow) (-1,-1 for undefined)
        public string[] animationFramesGlow; //array storing the ids of each frame in the animation (glow)

        public FaceData()
        {

        }

        public void TickAnimation()
        {
            //Update the counter
            if(animationFrames != null) //Does this face have an animation?
            {
                if (animationCounter == animationFrames.Length - 1) //If at max frame, reset to 0th frame
                {
                    animationCounter = 0;

                }
                else
                {
                    animationCounter++; //Else just increment up one
                }
                SetTexture(animationFrames[animationCounter]);
            }

            //Update the counter (glow)
            if(animationFramesGlow != null) //Does this face have a glow animation?
            {
                if (animationCounterGlow == animationFramesGlow.Length - 1) //If at max frame, reset to 0th frame
                {
                    animationCounterGlow = 0;

                }
                else
                {
                    animationCounterGlow++; //Else just increment up one
                }

                SetGlowTexture(animationFramesGlow[animationCounterGlow]); //Set the texture coordiante to the new animation texture
            }

        }

        //Setting a blocks texture UV start coordinate to desired textureID (glow)
        public void SetGlowTexture(string texture)
        {

            if (texture.Equals("")) //If no texture was provided, do nothing.
            {
                return;
            }

            Vector2 atlasPosition = DataManager.blockTexturePositions[texture]; //getting texture
            startCoordinateGlow = atlasPosition * Block.BlockToUV; //Calculating UV for start coords and applying to the start coordinate

        }

        //does this face have an animation?
        public bool HasAnimation()
        {
            if(animationFrames != null || animationFramesGlow != null)
            {
                return true;
            }
            return false;
        }

        //Setting a blocks texture UV start coordinate to desired textureID
        public void SetTexture(string texture)
        {

            if (texture.Equals("")) //If no texture was provided, do nothing.
            {
                return;
            }

            Vector2 atlasPosition = DataManager.blockTexturePositions[texture]; //getting texture
            startCoordinate = atlasPosition * Block.BlockToUV; //Calculating UV for start coords and applying to start coordinate

        }

        /// <summary>
        /// Set the face's animation to an animation array
        /// </summary>
        /// <param name="frames">string array of textureIDs for each frame</param>
        public void SetAnimation(string[] frames)
        {
            animationFrames = frames;
            SetTexture(frames[0]); //Set texture to first frame of animation
        }

        /// <summary>
        /// Set the face's glow animation to an animation array
        /// </summary>
        /// <param name="frames">string array of textureIDs for each frame</param>
        public void SetGlowAnimation(string[] frames)
        {
            animationFramesGlow = frames;
            SetGlowTexture(frames[0]); //sets glow texture to first frame of animation
        }


        //Does this face have a glow texture?
        public bool HasGlow()
        {
            if (!startCoordinateGlow.Equals(-Vector2.One))
            {
                return true;
            }
            return false;
        }

    }
}
