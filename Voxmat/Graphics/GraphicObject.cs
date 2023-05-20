using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxmat.Graphics
{
    public class GraphicObject
    {
        public Matrix4 Model;

        public int Vao;
        public int Vbo;
        public float[] Vertices;

        public VoxelModel ParentModel;

        public GraphicObject(float[] vertices, VoxelModel voxelModel)
        {
            InitOpenGLStructures(vertices);
            Vertices = vertices;
            Model = Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
            ParentModel = voxelModel;

        }

        public void Draw(int modelLocation,float scale = 1)
        {
            Matrix4 scaledModel = Matrix4.CreateScale(scale) * Model;
            GL.UniformMatrix4(modelLocation, false, ref scaledModel);
            GL.BindVertexArray(Vao);
            GL.DrawArrays(PrimitiveType.Quads, 0, Vertices.Length / 10);
        }

        public void Draw(int modelLocation, Vector3 positionOffset, float scale = 1)
        {
            Matrix4 scaledTranslatedModel = Matrix4.CreateTranslation(positionOffset) * Matrix4.CreateScale(scale) * Model;
            GL.UniformMatrix4(modelLocation, false, ref scaledTranslatedModel);
            GL.BindVertexArray(Vao);
            GL.DrawArrays(PrimitiveType.Quads, 0, Vertices.Length / 10);
        }

        public void DrawOutlined(int modelLocation,float thickness)
        {
            thickness += 1;
            float xOffset = (ParentModel.Size.X * thickness - ParentModel.Size.X) / 2 * -1;
            float yOffset = (ParentModel.Size.Y * thickness - ParentModel.Size.Y) / 2 * -1;
            float zOffset = (ParentModel.Size.Z * thickness - ParentModel.Size.Z) / 2 * -1;
            GL.DepthMask(false);
            Draw(modelLocation, new Vector3(xOffset, yOffset, zOffset), thickness);
            GL.DepthMask(true);
        }

        private void InitOpenGLStructures(float[] vertices)
        {
            Vao = GL.GenVertexArray();
            GL.BindVertexArray(Vao);

            Vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, Vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 10 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // color attribute
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 10 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 10 * sizeof(float), 7 * sizeof(float));
            GL.EnableVertexAttribArray(2);
        }


    }
}
