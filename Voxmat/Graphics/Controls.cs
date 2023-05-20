using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Voxmat.Graphics
{
    public class Controls
    {
        public float MovementSensetivity = 0.2f;
        public float RotationSensetivity = 0.3f;
        public float ZoomSensetivity = 0.01f;

        public Camera MainCamera;
        public Form1 MainForm;

        public Vector2? LastMousePos;
        public Vector2 MousePos;


        public Controls(Camera camera, Form1 form)
        {
            MainCamera = camera;
            MainForm = form;
            LastMousePos = null;
        }

        private Vector2 getMouseDelta()
        {
            MousePos = MainForm.MousePos;
            if (LastMousePos == null)
            {
                LastMousePos = MousePos;
            }
            return MousePos - (Vector2)LastMousePos;
        }

        public void RotationCameraControl(Vector2 mouseDelta)
        {
            if (MainForm.IsMouseOnRender && MainForm.IsRightButtonDown)
            {

                float angleX = mouseDelta.X * (float)(Math.PI / 180) * RotationSensetivity;
                float angleY = mouseDelta.Y * (float)(Math.PI / 180) * RotationSensetivity;

                Matrix4d horRot = Matrix4d.CreateFromAxisAngle(MainCamera.Up, -angleX);
                Matrix4d verRot = Matrix4d.CreateFromAxisAngle(MainCamera.Right, -angleY);

                Vector3d position = Vector4d.Transform(new Vector4d(MainCamera.Position - MainCamera.LookAt, 1),horRot).Xyz + MainCamera.LookAt;
                MainCamera.Position = Vector4d.Transform(new Vector4d(position - MainCamera.LookAt, 1), verRot).Xyz + MainCamera.LookAt;

                MainCamera.up = Vector4d.Transform(new Vector4d(MainCamera.up, 1), verRot).Xyz;
                MainCamera.right = Vector4d.Transform(new Vector4d(MainCamera.right, 1), horRot).Xyz;
                MainCamera.front = Vector3d.Normalize(MainCamera.LookAt - MainCamera.Position);

                MainCamera.right.Y = 0;
                MainCamera.up = Vector3d.Normalize(-Vector3d.Cross(MainCamera.front, MainCamera.right));

                //var rollDegradation = Math.Acos()

            }

        }

        public void ZoomControl(float wheelDelta)
        {
            double distanceCoeff = Math.Max(Vector3d.Distance(MainCamera.Position, MainCamera.LookAt) / 15, 1);
            MainCamera.Position = MainCamera.Position + MainCamera.Front * wheelDelta * ZoomSensetivity * distanceCoeff;
            MainForm.WheelDelta = 0;
        }

        public void MovementCameraControl(Vector2 mouseDelta)
        {

            if (MainForm.IsMouseOnRender && MainForm.IsMiddleButtonDown)
            {
                Vector3 posChange = new Vector3(mouseDelta * new Vector2(-1,1) * MovementSensetivity);
                Vector3d worldPosChange = Vector3d.Normalize(MainCamera.Up) *
                    posChange.Y + Vector3d.Normalize(MainCamera.Right) * posChange.X;
                MainCamera.Position += worldPosChange * MovementSensetivity;
                MainCamera.LookAt += worldPosChange * MovementSensetivity;
                //MainCamera.UpdateVectorsFromLookAt();
            }
            
        }

        public void UnitMoveObject(VoxelModel model, Keys key)
        {
            Vector3i pos = model.ScenePosition;
            switch(key)
            {
                case Keys.W:
                    model.ScenePosition = new Vector3i(pos.X, pos.Y, pos.Z - 1);
                    break;
                case Keys.S:
                    model.ScenePosition = new Vector3i(pos.X, pos.Y, pos.Z + 1);
                    break;
                case Keys.A:
                    model.ScenePosition = new Vector3i(pos.X - 1, pos.Y, pos.Z);
                    break;
                case Keys.D:
                    model.ScenePosition = new Vector3i(pos.X + 1, pos.Y, pos.Z);
                    break;
                case Keys.Space:
                    model.ScenePosition = new Vector3i(pos.X, pos.Y + 1, pos.Z);
                    break;
                case Keys.C:
                    model.ScenePosition = new Vector3i(pos.X, pos.Y - 1, pos.Z);
                    break;
            }
        }

        public void ControlObject(VoxelModel model, Keys key)
        {
            Vector3i pos = model.ScenePosition;
            switch (key)
            {
                case Keys.Delete:
                    MainForm.DeleteModel(model);
                    break;
            }
        }

        public void ProcessInput()
        {
            Vector2 mouseDelta = getMouseDelta();

            MovementCameraControl(mouseDelta);
            RotationCameraControl(mouseDelta);
            ZoomControl(MainForm.WheelDelta);

            LastMousePos = MousePos;
        }
    }
}
