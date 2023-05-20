using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxmat.Graphics
{
    public class Camera
    {
        public Vector3d LookAt;
        public Vector3d front = -Vector3d.UnitZ;

        public Vector3d up = Vector3d.UnitY;

        public Vector3d right = Vector3d.UnitX;

        private double pitch;

        private double yaw = -MathHelper.PiOver2; // Without this you would start rotate 90 degrees to the right

        private double fov = MathHelper.PiOver2;

        public Camera(Vector3d position, double aspectRatio)
        {
            Position = position;
            InitialPosition = Position;
            AspectRatio = aspectRatio;
            LookAt = new Vector3d(0, 0, 0);
            UpdateVectors();
        }

        public void Reset()
        {

            Position = InitialPosition;
            LookAt = new Vector3d(0, 0, 0);

            front = -Vector3d.UnitZ;
            up = Vector3d.UnitY;
            right = Vector3d.UnitX;

            pitch = 0;
            yaw = -MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right
            fov = MathHelper.PiOver2;
    }

        public Vector3d Position;

        public Vector3d InitialPosition;

        public double AspectRatio { private get; set; }

        public Vector3d Front => front;

        public Vector3d Up => up;

        public Vector3d Right => right;

        public double Pitch
        {
            get => MathHelper.RadiansToDegrees(pitch);
            set
            {
                double angle = MathHelper.Clamp(value, -89f, 89f);
                pitch = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        public double Yaw
        {
            get => MathHelper.RadiansToDegrees(yaw);
            set
            {
                yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        public double Fov
        {
            get => MathHelper.RadiansToDegrees(fov);
            set
            {
                double angle = MathHelper.Clamp(value, 1f, 45f);
                fov = MathHelper.DegreesToRadians(angle);
            }
        }

        public Matrix4d GetViewMatrix()
        {
            return Matrix4d.LookAt(Position, Position + front, up);
        }

        public Matrix4d GetProjectionMatrix()
        {
            return Matrix4d.CreatePerspectiveFieldOfView(fov, AspectRatio, 0.01f, 1000f);
        }

        private void UpdateVectors()
        {
            front.X = Math.Cos(pitch) * Math.Cos(yaw);
            front.Y = Math.Sin(pitch);
            front.Z = Math.Cos(pitch) * Math.Sin(yaw);
            front = Vector3d.Normalize(front);

            right = Vector3d.Normalize(Vector3d.Cross(front, Vector3d.UnitY));

            up = Vector3d.Cross(right, front);
            up = Vector3d.Normalize(up);
        }

        public static Matrix4 MatrixToFloat(Matrix4d matd)
        {
            Matrix4 mat = new Matrix4((float)matd.M11, (float)matd.M12, (float)matd.M13, (float)matd.M14,
                                  (float)matd.M21, (float)matd.M22, (float)matd.M23, (float)matd.M24,
                                  (float)matd.M31, (float)matd.M32, (float)matd.M33, (float)matd.M34,
                                  (float)matd.M41, (float)matd.M42, (float)matd.M43, (float)matd.M44);
            return mat;
        }


    }
}
