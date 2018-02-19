using Ionic.Zlib;
using System;
using System.Drawing;
using System.IO;

namespace Aseprite
{
    public abstract class Cel : UserDataHolder
    {
        protected ushort layerIndex;
        public short X { get; protected set; }
        public short Y { get; protected set; }
        public byte Opacity { get; protected set; }
        public CelExtraData ExtraData { get; internal set; }

        internal Cel(ushort layerIndex, short x, short y, byte opacity)
        {
            this.layerIndex = layerIndex;
            X = x;
            Y = y;
            Opacity = opacity;
        }

        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract Frame Frame { get; }

        public Ase Ase
            => Frame.Ase;

        public ImageLayer Layer
            => Ase.Layers[layerIndex] as ImageLayer;

        public byte GlobalOpacity
            => (byte)Blending.BlendMultiply(Opacity, Layer.Opacity);
    }

    internal class Cel<T> : Cel where T : struct
    {
        private T[,] values;
        private Func<BlendMode, Blender<T>> getBlender;
        private Frame<T> frame;

        public static Cel<T> FromChunk(Chunk chunk, Ase<T> ase)
        {
            using (var reader = chunk.GetDataReader())
            {
                ushort layerIndex
                    = reader.ReadUInt16();
                short celX
                    = reader.ReadInt16();
                short celY
                    = reader.ReadInt16();
                byte celOpacity
                    = reader.ReadByte();
                ushort celType
                    = reader.ReadUInt16();
                var _future
                    = reader.ReadBytes(7);

                switch (celType)
                {
                    case 0:
                    case 2:
                        ushort width
                            = reader.ReadUInt16();
                        ushort height
                            = reader.ReadUInt16();
                        const int HEADER_LENGTH = 20;
                        int dataLength = chunk.Data.Length - HEADER_LENGTH;
                        byte[] data = new byte[dataLength];
                        Array.Copy(chunk.Data, HEADER_LENGTH, data, 0, dataLength);
                        if (celType == 2)
                        {
                            data = ZlibStream.UncompressBuffer(data);
                        }
                        var values = new T[width, height];
                        using (var dataStream = new MemoryStream(data))
                        using (var dataReader = new BinaryReader(dataStream))
                        {
                            for (var y = 0; y < height; ++y)
                            {
                                for (var x = 0; x < width; ++x)
                                {
                                    values[x, y] = ase.ReadValue(dataReader);
                                }
                            }
                        }
                        return new Cel<T>(layerIndex, celX, celY, celOpacity, ase.GetFrame(chunk.FrameIndex), values, ase.GetBlender);

                    case 1:
                        ushort linkedIndex
                            = reader.ReadUInt16();
                        return ase.GetFrame(linkedIndex).GetCel(ase.Layers[layerIndex]);

                    default:
                        throw new ArgumentException("Unknown cel type encountered.");
                }
            }
        }

        public Cel(ushort layerIndex, short x, short y, byte opacity, Frame<T> frame, T[,] values, Func<BlendMode, Blender<T>> getBlender)
            : base(layerIndex, x, y, opacity)
        {
            this.frame = frame;
            this.values = values;
            this.getBlender = getBlender;
        }

        public T this[int x, int y]
            => values[x, y];

        public override int Width
            => values.GetLength(0);

        public override int Height
            => values.GetLength(1);

        public override Frame Frame
            => frame;

        public Blender<T> Blender
            => getBlender(Layer.Mode);

        public void RenderCanvassed(T[,] dest, Point destOffset)
        {
            var blend = Blender;
            byte opacity = GlobalOpacity;

            int frameTop = Math.Max(0, (int)Y);
            int destBottom = destOffset.Y + Math.Min(Ase.ImagePixelHeight, Y + Height);
            int frameLeft = Math.Max(0, (int)X);
            int destRight = destOffset.X + Math.Min(Ase.ImagePixelWidth, X + Width);

            for (int cy = frameTop - Y, dy = frameTop + destOffset.Y;
                dy < destBottom;
                ++cy, ++dy)
            {
                for (int cx = frameLeft - X, dx = frameLeft + destOffset.X;
                    dx < destRight;
                    ++cx, ++dx)
                {
                    dest[dx, dy] = blend(dest[dx, dy], values[cx, cy], opacity);
                }
            }
        }

        public void RenderCanvassed(T[,] dest)
        {
            RenderCanvassed(dest, new Point(0, 0));
        }

        public T[,] RenderCanvassed()
        {
            var output = new T[Ase.ImagePixelWidth, Ase.ImagePixelHeight];
            RenderCanvassed(output, new Point(0, 0));
            return output;
        }
    }
}
