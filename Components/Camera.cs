using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components
{
    internal class Camera
    {
        //Attributes
        private Vector3 cameraPosition;
        private Vector3 cameraRotation;
        private Vector3 cameraLookAt;
        private GraphicsDevice graphics;
        private BoundingFrustum boundingFrustum;

        //Properties
        public Vector3 Position
        {
            get { return cameraPosition; }
            set
            {
                cameraPosition = value;
                UpdateLookAt(); //Whenever camera pos (or rot) is changed, our look at vector needs to be edited
            }
        }
        public Vector3 Rotation
        {
            get { return cameraRotation; }
            set
            {
                cameraRotation = value;
                UpdateLookAt();
            }
        }
        public Matrix Projection
        {
            get;
            protected set;
        }

        public Matrix View
        {
            get
            {
                return Matrix.CreateLookAt(cameraPosition, cameraLookAt, Vector3.Up);
            }
        }

        public Camera(GraphicsDevice _graphics, Vector3 position, Vector3 rotation)
        {
            graphics = _graphics;

            //Setup Projection Matrix
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _graphics.Viewport.AspectRatio, 0.05f, 1000f);

            //Create the Frustum. This is used for culling.
            boundingFrustum = new BoundingFrustum(Matrix.Multiply(View, Projection));

            //Set Camera position and rotation
            MoveTo(position, rotation);
        }

        //Method that sets camera pos and rot
        public void MoveTo(Vector3 pos, Vector3 rot)
        {
            Position = pos;
            Rotation = rot;
        }

        //Updates the cameras matricies depending on its current rotation.
        public void UpdateLookAt()
        {
            //Build rotation matrix
            Matrix rotationMatrix = Matrix.CreateRotationX(cameraRotation.X) * Matrix.CreateRotationY(cameraRotation.Y);

            //Build look at offset vector
            Vector3 lookAtOffset = Vector3.Transform(Vector3.UnitZ, rotationMatrix);

            //Update camera look at vector
            cameraLookAt = cameraPosition + lookAtOffset;

            //Update frustum
            boundingFrustum = new BoundingFrustum(Matrix.Multiply(View, Projection));
        }

        /// <summary>
        /// Method takes in hitboxes and determines if they are within the camera's frustum.
        /// This is mostly used for chunk furstum culling, in which chunks which are not in the frustum are not culled.
        /// </summary>
        /// <param name="box"></param>
        /// <returns>True if in frustum, false if not.</returns>
        public bool InFrustum(BoundingBox box)
        {
            if (boundingFrustum.Contains(box) == ContainmentType.Contains || boundingFrustum.Contains(box) == ContainmentType.Intersects)
            {
                return true;
            }
            return false;
        }
    }
}
