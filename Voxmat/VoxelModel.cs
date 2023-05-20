using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Voxmat.Graphics;

namespace Voxmat
{
    public struct Vector3i
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3i(int x = 0, int y = 0, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Vector4b
    {
        [FieldOffset(0)] public byte X;
        [FieldOffset(1)] public byte Y;
        [FieldOffset(2)] public byte Z;
        [FieldOffset(3)] public byte W;

        [FieldOffset(0)] public byte R;
        [FieldOffset(1)] public byte G;
        [FieldOffset(2)] public byte B;
        [FieldOffset(3)] public byte A; 

        public Vector4b(byte x = 0, byte y = 0, byte z = 0, byte w = 0)
        {
            R = 0;
            G = 0;
            B = 0;
            A = 0;

            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static bool operator ==(Vector4b vec1, Vector4b vec2)
        {
            return vec1.Equals(vec2);
        }

        public static bool operator !=(Vector4b vec1, Vector4b vec2)
        {
            return !(vec1 == vec2);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != GetType())
            {
                return false;
            }

            Vector4b VecObj = (Vector4b)obj;
            if(X == VecObj.X && Y == VecObj.Y && Z == VecObj.Z && W == VecObj.W)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class VoxelModel
    {
        public string Name;

        public Vector3i Size;

        public Vector4b[,,] Data;

        public float[] MeshVertices;

        private Vector3i scenePosition;
        public Vector3i ScenePosition
        {
            get => scenePosition;
            set
            {
                scenePosition = value;
                GObj.Model = Matrix4.CreateTranslation(value.X, value.Y, value.Z);
            }
        }

        public GraphicObject GObj;

        public ushort Material;

        public static string[] SupportedFormatsInput = new string[] { "Main imporint format (.xraw)|*.xraw" };

        
        public VoxelModel(int x, int y, int z)
        {
            Data = new Vector4b[x, y, z];
            Size = new Vector3i(x, y, z);
            scenePosition = new Vector3i();
        }

        public VoxelModel(Stream data)
        {
            LoadFromRaw(data);
        }

        public VoxelModel(Stream data, string name) : this(data)
        {
            Name = name;
        }

        public void LoadFromRaw(Stream data)
        {
            BinaryReader reader = new BinaryReader(data);

            char[] fileFormat = reader.ReadChars(4);
            if (new string(fileFormat) != "XRAW")
            {
                throw new FormatException("File is not XRAW");
            }

            byte channel = reader.ReadByte();
            if (channel != 0)
            {
                throw new FormatException("Only uint channels are supported");
            }

            byte channelCount = reader.ReadByte();
            if (channelCount != 4)
            {
                throw new FormatException("only RGBA layout is supported");
            }

            byte bitsPerChannel = reader.ReadByte();
            if (bitsPerChannel != 8)
            {
                throw new FormatException("A channel's size must be 8 bit");
            }

            int bytesPerIndex = reader.ReadByte() / 8;

            Vector3i notRotSize = new Vector3i();
            notRotSize.X = reader.ReadInt32();
            notRotSize.Y = reader.ReadInt32();
            notRotSize.Z = reader.ReadInt32();

            uint PaletteColorsCount = reader.ReadUInt32();

            ushort[,,] dataDiscrete = new ushort[notRotSize.X, notRotSize.Y, notRotSize.Z];

            for (int k = 0; k < notRotSize.Z; k++)
            {
                for (int j = 0; j < notRotSize.Y; j++)
                {
                    for (int i = 0; i < notRotSize.X; i++)
                    {
                        if (bytesPerIndex == sizeof(byte))
                        {
                            dataDiscrete[i, j, k] = reader.ReadByte();
                        }
                        else if (bytesPerIndex == sizeof(ushort))
                        {
                            dataDiscrete[i, j, k] = reader.ReadUInt16();
                        }
                    }
                }
            }

            Vector4b[] paletteData = new Vector4b[PaletteColorsCount];
            byte[] buff = new byte[4];
            for (int i = 0; i < PaletteColorsCount; i++)
            {
                buff = reader.ReadBytes(4);
                paletteData[i] = new Vector4b(buff[0], buff[1], buff[2], buff[3]);
            }

            Size = new Vector3i(notRotSize.X, notRotSize.Z, notRotSize.Y);
            Data = new Vector4b[Size.X, Size.Y, Size.Z];

            for (int i = 0; i < Size.X; i++)
            {
                for (int j = 0; j < Size.Y; j++)
                {
                    for (int k = 0; k < Size.Z; k++)
                    {
                        Data[i, j, k] = paletteData[dataDiscrete[i, Size.Z - k - 1, j]];
                    }
                }
            }
        }

        private  void AddVertex(List<float> intermList, Vector3 voxelPos, Vector4b color, Vector3 normal)
        {
            intermList.Add(voxelPos.X);
            intermList.Add(voxelPos.Y);
            intermList.Add(voxelPos.Z);

            intermList.Add(color.R / 255F);
            intermList.Add(color.G / 255F);
            intermList.Add(color.B / 255F);
            intermList.Add(color.A / 255F);

            intermList.Add(normal.X);
            intermList.Add(normal.Y);
            intermList.Add(normal.Z);
        }

        public void generateMesh()
        {
            Vector4b empty = new Vector4b();
            Vector3 VertexPos = new Vector3();
            List<float> intermList = new List<float>();

            for (int i = 0; i < Size.X; i++)
            {
                for (int j = 0; j < Size.Y; j++)
                {
                    for (int k = 0; k < Size.Z; k++)
                    {
                        if (Data[i, j, k] != empty)
                        {
                            if (j + 1 == Size.Y || Data[i, j + 1, k] == empty)
                            {
                                Vector3 NormalVector = new Vector3(0, 0, 1);

                                VertexPos.X = (i);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);
                            }

                            if (j - 1 == -1 || Data[i, j - 1, k] == empty)
                            {
                                Vector3 NormalVector = new Vector3(0, 0, -1);

                                VertexPos.X = (i);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);
                            }

                            if (k + 1 == Size.Z || Data[i, j, k + 1] == empty)
                            {
                                Vector3 NormalVector = new Vector3(0, 1, 0);

                                VertexPos.X = (i);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);
                            }

                            if (k - 1 == -1 || Data[i, j, k - 1] == empty)
                            {
                                Vector3 NormalVector = new Vector3(0, -1, 0);

                                VertexPos.X = (i);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);
                            }

                            if (i + 1 == Size.X || Data[i + 1, j, k] == empty)
                            {
                                Vector3 NormalVector = new Vector3(1, 0, 0);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i + 1);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);
                            }

                            if (i - 1 == -1 || Data[i - 1, j, k] == empty)
                            {
                                Vector3 NormalVector = new Vector3(-1, 0, 0);

                                VertexPos.X = (i);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i);
                                VertexPos.Y = (j + 1);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);

                                VertexPos.X = (i);
                                VertexPos.Y = (j);
                                VertexPos.Z = (k + 1);
                                AddVertex(intermList, VertexPos, Data[i, j, k], NormalVector);
                            }
                        }
                    }

                }

            }
            
            GObj = new GraphicObject(intermList.ToArray(), this);

        }

    }

}

