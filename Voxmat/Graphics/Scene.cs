using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Voxmat.Graphics
{
    public unsafe class Scene
    {
        public Vector3i Size;

        public List<VoxelModel> Models;

        public Camera MainCamera;
        public Form1 MainForm;
        public Controls MainControls;

        public VoxelModel SelectedModel;

        public int VertexShader;
        public int FragmentShader;
        public int ShaderProgram;

        public int VertexShaderOutline;
        public int FragmentShaderOutline;
        public int ShaderProgramOutline;

        public int ViewLocation;
        public int ProjectionLocation;
        public int ModelLocation;

        public int ViewLocationOutline;
        public int ProjectionLocationOutline;
        public int ModelLocationOutline;

        public string sceneFormatName = "voxmat";
        public string exportFormat = "ama";

        public Vector4 BackgroundColor;
        public Vector4 GridColor;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AMAcell
        {
            public Vector4b Color;
            public ushort Material;
        }

        public static unsafe void LoadImageFromStream(string path, out int[] pixels, out Vector2 size)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(stream);

                pixels = new int[bmp.Width * bmp.Height];
                BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                int toCopy = bmp.Width * bmp.Height * 4;
                fixed (void* destPtr = pixels)
                    System.Buffer.MemoryCopy((void*)bmpData.Scan0, destPtr, toCopy, toCopy);

                bmp.UnlockBits(bmpData);

                size = new Vector2(bmp.Width, bmp.Height);
            }

        }

        public Scene(Camera camera, Controls controls, Form1 mainForm, Vector3i size)
        {
            MainCamera = camera;
            MainForm = mainForm;
            MainControls = controls;
            Models = new List<VoxelModel>();
            Size = size;
            //glControl1.MakeCurrent();

            Color backColorRev = MainForm.glControl1.BackColor;
            BackgroundColor = new Vector4(backColorRev.R / 255f, backColorRev.G / 255f,
                        backColorRev.B / 255f, backColorRev.A / 255f);

            GridColor = new Vector4();


            InitOpenGL();

        }

        public void InitOpenGL()
        {
            GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, BackgroundColor.W);
            //vec4 rotNormal = model * vec4(aNormal, 1.0);
            VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShader, Shaders.vertexCode);
            GL.CompileShader(VertexShader);

            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShader, Shaders.fragmentCode);
            GL.CompileShader(FragmentShader);

            ShaderProgram = GL.CreateProgram();
            GL.AttachShader(ShaderProgram, VertexShader);
            GL.AttachShader(ShaderProgram, FragmentShader);
            GL.LinkProgram(ShaderProgram);
            GL.UseProgram(ShaderProgram);

            GL.DeleteShader(VertexShader);
            GL.DeleteShader(FragmentShader);


            VertexShaderOutline = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShaderOutline, Shaders.vertexCodeOutline);
            GL.CompileShader(VertexShaderOutline);

            FragmentShaderOutline = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShaderOutline, Shaders.fragmentCodeOutline);
            GL.CompileShader(FragmentShaderOutline);

            ShaderProgramOutline = GL.CreateProgram();
            GL.AttachShader(ShaderProgramOutline, VertexShaderOutline);
            GL.AttachShader(ShaderProgramOutline, FragmentShaderOutline);
            GL.LinkProgram(ShaderProgramOutline);
            GL.UseProgram(ShaderProgramOutline);

            GL.DeleteShader(VertexShaderOutline);
            GL.DeleteShader(FragmentShaderOutline);

            ViewLocation = GL.GetUniformLocation(ShaderProgram, "view");
            ProjectionLocation = GL.GetUniformLocation(ShaderProgram, "projection");
            ModelLocation = GL.GetUniformLocation(ShaderProgram, "model");

            ViewLocationOutline = GL.GetUniformLocation(ShaderProgramOutline, "view");
            ProjectionLocationOutline = GL.GetUniformLocation(ShaderProgramOutline, "projection");
            ModelLocationOutline = GL.GetUniformLocation(ShaderProgramOutline, "model");

            GL.Enable(EnableCap.DepthTest);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

        }

        public void DrawScene()
        {
            MainControls.ProcessInput();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view = Camera.MatrixToFloat(MainCamera.GetViewMatrix());
            Matrix4 projection = Camera.MatrixToFloat(MainCamera.GetProjectionMatrix());

            DrawGrid(view, projection);

            foreach (VoxelModel model in Models)
            {
                if (model == SelectedModel)
                {
                    GL.UseProgram(ShaderProgramOutline);
                    GL.UniformMatrix4(ViewLocationOutline, false, ref view);
                    GL.UniformMatrix4(ProjectionLocationOutline, false, ref projection);

                    model.GObj.DrawOutlined(ModelLocationOutline, 0.1f);
                }

                GL.UseProgram(ShaderProgram);
                GL.UniformMatrix4(ViewLocation, false, ref view);
                GL.UniformMatrix4(ProjectionLocation, false, ref projection);

                model.GObj.Draw(ModelLocation);
            }

        }

        public void AddModel(VoxelModel model)
        {
            Models.Add(model);
            model.generateMesh();
        }

        public void DrawGrid(Matrix4 view, Matrix4 projection)
        {
            GL.UseProgram(0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadMatrix(ref view);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadMatrix(ref projection);

            GL.Begin(PrimitiveType.Lines);
            GL.Color3(GridColor.X, GridColor.Y, GridColor.Z);

            /*Vector3 testFront = (view * new Vector4((float)MainCamera.front.X, (float)MainCamera.front.Y, (float)MainCamera.front.Z, 1)).Xyz;
            testFront *= 10;
            testFront.Y += 10;
            GL.Vertex3(MainCamera.Position);
            GL.Vertex3(testFront);*/

            for(int i = 0;i <= Size.X;i++)
            {
                GL.Vertex3(i, 0, 0);
                GL.Vertex3(i, 0, Size.Z);
            }
            for (int k = 0; k <= Size.Z; k++)
            {
                GL.Vertex3(0, 0, k);
                GL.Vertex3(Size.X, 0, k);
            }

            for (int j = 0; j <= Size.Y; j++)
            {
                GL.Vertex3(0, j, 0);
                GL.Vertex3(Size.X, j, 0);
            }
            for (int i = 0; i <= Size.X; i++)
            {
                GL.Vertex3(i, 0, 0);
                GL.Vertex3(i, Size.Y, 0);
            }

            for (int k = 0; k <= Size.Z; k++)
            {
                GL.Vertex3(0, 0, k);
                GL.Vertex3(0, Size.Y, k);
            }
            for (int j = 0; j <= Size.Y; j++)
            {
                GL.Vertex3(0, j, 0);
                GL.Vertex3(0, j, Size.Z);
            }

            GL.End();

            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
        }

        public void SaveScene(Stream file)
        {
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(sceneFormatName.ToCharArray());

            writer.Write(Size.X);
            writer.Write(Size.Y);
            writer.Write(Size.Z);

            writer.Write(Models.Count);

            foreach(VoxelModel model in Models)
            {
                writer.Write(model.Name.Length);
                writer.Write(model.Name.ToCharArray());

                writer.Write(model.Size.X);
                writer.Write(model.Size.Y);
                writer.Write(model.Size.Z);

                writer.Write(model.ScenePosition.X);
                writer.Write(model.ScenePosition.Y);
                writer.Write(model.ScenePosition.Z);

                writer.Write(model.Material);

                byte[] buff = new byte[sizeof(Vector4b)];

                for(int k = 0; k < model.Size.Z; k++)
                {
                    for (int j = 0; j < model.Size.Y; j++)
                    {
                        for (int i = 0; i < model.Size.X; i++)
                        {
                            fixed (void* strPtr = &model.Data[i,j,k])
                                fixed (void* bufPtr = buff)
                                    Unsafe.CopyBlock(bufPtr, strPtr, (uint)sizeof(Vector4b));

                            writer.Write(buff);
                        }
                    }
                }
            }
        }

        public void LoadScene(Stream file)
        {
            BinaryReader reader = new BinaryReader(file);

            string formatName = new string(reader.ReadChars(sceneFormatName.Length));

            if(formatName != sceneFormatName)
            {
                throw new FileLoadException("The chosen file format isn't supported");
            }

            Size.X = reader.ReadInt32();
            Size.Y = reader.ReadInt32();
            Size.Z = reader.ReadInt32();

            int modelsCount = reader.ReadInt32();

            for(int modI = 0; modI < modelsCount; modI++)
            {
                int nameLength = reader.ReadInt32();
                string name = new string(reader.ReadChars(nameLength));

                int modelSizeX = reader.ReadInt32();
                int modelSizeY = reader.ReadInt32();
                int modelSizeZ = reader.ReadInt32();

                VoxelModel addedModel = new VoxelModel(modelSizeX, modelSizeY, modelSizeZ);

                addedModel.Name = name;

                Vector3i scenePos = new Vector3i();
                scenePos.X = reader.ReadInt32();
                scenePos.Y = reader.ReadInt32();
                scenePos.Z = reader.ReadInt32();

                addedModel.Material = reader.ReadUInt16();

                byte[] buff = new byte[sizeof(Vector4b)];

                for (int k = 0; k < addedModel.Size.Z; k++)
                {
                    for (int j = 0; j < addedModel.Size.Y; j++)
                    {
                        for (int i = 0; i < addedModel.Size.X; i++)
                        {
                            buff = reader.ReadBytes(sizeof(Vector4b));

                            fixed (void* strPtr = &addedModel.Data[i, j, k])
                                fixed (void* bufPtr = buff)
                                    Unsafe.CopyBlock(strPtr, bufPtr, (uint)sizeof(Vector4b));
                        }
                    }
                }

                AddModel(addedModel);

                addedModel.ScenePosition = scenePos;
            }
        }


        public void ExportMaterialized(Stream file)
        {
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(exportFormat.ToCharArray());

            writer.Write(Size.X);
            writer.Write(Size.Y);
            writer.Write(Size.Z);

            AMAcell[,,] data = new AMAcell[Size.X, Size.Y, Size.Z];

            foreach (VoxelModel model in Models)
            {
                for (int k = 0; k < model.Size.Z; k++)
                {
                    for (int j = 0; j < model.Size.Y; j++)
                    {
                        for (int i = 0; i < model.Size.X; i++)
                        {
                            Vector3 worldPos = (model.GObj.Model * new Vector4(i, j, k, 1)).Xyz;
                            Vector3i worldPosI = new Vector3i((int)Math.Round(worldPos.X), 
                                        (int)Math.Round(worldPos.Y), (int)Math.Round(worldPos.Z));

                            if ((worldPosI.X < 0 || worldPos.X >= Size.X) ||
                                (worldPosI.Y < 0 || worldPos.Y >= Size.Y) ||
                                (worldPosI.Z < 0 || worldPos.Z >= Size.Z))
                                continue;

                            ushort clarifyMaterial;
                            if (model.Data[i, j, k] == new Vector4b())
                                clarifyMaterial = 0;
                            else
                                clarifyMaterial = model.Material;

                            data[worldPosI.X, worldPosI.Y, worldPosI.Z] =
                                new AMAcell { Color = model.Data[i, j, k], Material = clarifyMaterial};
                        }
                    }
                }
            }

            for (int k = 0; k < Size.Z; k++)
            {
                for (int j = 0; j < Size.Y; j++)
                {
                    for (int i = 0; i < Size.X; i++)
                    {
                        byte[] buff = new byte[sizeof(AMAcell)];

                        fixed (void* strPtr = &data[i, j, k])
                            fixed (void* bufPtr = buff)
                                Unsafe.CopyBlock(bufPtr, strPtr, (uint)sizeof(AMAcell));

                        writer.Write(buff);
                    }
                }
            }

        }
    }
}
