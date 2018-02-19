using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Aseprite
{
    public abstract class Ase : ILayerGroup
    {
        protected AseHeader header;

        public Palette Palette { get; protected set; }

        protected List<Layer> layers = new List<Layer>();
        public IReadOnlyList<Layer> Layers => layers;

        public IReadOnlyList<GroupLayer> Subgroups => layers
            .Where(l => l is GroupLayer && l.Level == 0)
            .Select(l => l as GroupLayer)
            .ToArray();

        public IReadOnlyList<ImageLayer> Images => layers
            .Where(l => l is ImageLayer && l.Level == 0)
            .Select(l => l as ImageLayer)
            .ToArray();

        public abstract IReadOnlyList<Frame> Frames { get; }

        protected List<FrameTag> frameTags = new List<FrameTag>();
        public IReadOnlyList<FrameTag> FrameTags => frameTags;

        protected List<Slice> slices = new List<Slice>();
        public IReadOnlyList<Slice> Slices => slices;

        public int ImagePixelWidth
            => header.ImagePixelWidth;

        public int ImagePixelHeight
            => header.ImagePixelHeight;

        public bool LayerOpacityIsValid
            => (header.Flags & 0x01) == 1;

        public float PixelRatio
            => (float)header.PixelRatioWidth / header.PixelRatioHeight;

        public static Ase FromReader(BinaryReader reader)
        {
            var header = new AseHeader(reader);
            switch (header.ColorDepth)
            {
                case 8:
                    return new Ase<byte>(header, reader,
                        Colorizing.ReadIndex,
                        Colorizing.GetIndexColorizer,
                        Blending.GetIndexBlender);

                case 16:
                    return new Ase<GrayColor>(header, reader,
                        Colorizing.ReadGray,
                        Colorizing.GetGrayColorizer,
                        Blending.GetGrayBlender);

                case 32:
                    return new Ase<Color>(header, reader,
                        Colorizing.ReadColor,
                        Colorizing.GetColorPassthrough,
                        Blending.GetColorBlender);

                default:
                    throw new FormatException("Unsupported color depth encountered.");
            }
        }

        public static Ase FromStream(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
                return FromReader(reader);
        }

        public static Ase FromFile(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open))
                return FromStream(stream);
        }

        internal static string ReadString(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }
    }

    internal class Ase<T> : Ase where T : struct
    {
        public Func<BinaryReader, T> ReadValue { get; private set; }
        public Func<Palette, Colorizer<T>> GetColorizer { get; private set; }
        public Func<BlendMode, Blender<T>> GetBlender { get; private set; }

        private Frame<T>[] frames;
        public override IReadOnlyList<Frame> Frames => frames;

        public Frame<T> GetFrame(int frameIndex)
            => frames[frameIndex];

        public Ase(AseHeader header, BinaryReader reader, Func<BinaryReader, T> readValue, Func<Palette, Colorizer<T>> getColorizer, Func<BlendMode, Blender<T>> getBlender)
        {
            base.header = header;
            frames = new Frame<T>[header.FrameCount];
            Palette = new Palette(header.TransparentColorIndex);
            ReadValue = readValue;
            GetColorizer = getColorizer;
            GetBlender = getBlender;

            var chunks = new List<Chunk>();
            for (var i = 0; i < frames.Length; ++i)
            {
                var frameHeader = new FrameHeader(reader);
                frames[i] = new Frame<T>(frameHeader, this);
                for (var j = 0; j < frameHeader.ChunkCount; ++j)
                {
                    chunks.Add(Chunk.FromReader(reader, i));
                }
            }

            var prevCel = default(Cel);
            var prevUDH = default(UserDataHolder);
            foreach (var chunk in chunks)
            {
                switch (chunk.ChunkType)
                {
                    case ChunkType.Palette:
                        Palette.AddFromNewChunk(chunk);
                        break;

                    case ChunkType.OldPalette4:
                    case ChunkType.OldPalette11:
                        if (!chunks.Any(c => c.ChunkType == ChunkType.Palette))
                            Palette.AddFromOldChunk(chunk);
                        break;

                    case ChunkType.Layer:
                        var layer = Layer.FromChunk(chunk, this);
                        layers.Add(layer);
                        prevUDH = layer;
                        break;

                    case ChunkType.Cel:
                        var cel = Cel<T>.FromChunk(chunk, this);
                        frames[chunk.FrameIndex].AddCel(cel);
                        prevCel = cel;
                        prevUDH = cel;
                        break;

                    case ChunkType.CelExtra:
                        prevCel.ExtraData = CelExtraData.FromChunk(chunk);
                        break;

                    case ChunkType.UserData:
                        prevUDH.UserData = UserData.FromChunk(chunk);
                        break;

                    case ChunkType.FrameTags:
                        frameTags.AddRange(FrameTag.TagsFromChunk(chunk));
                        break;

                    case ChunkType.Slice:
                        slices.Add(Slice.FromChunk(chunk));
                        break;

                    case ChunkType.Mask:
                        throw new ArgumentException("Deprecated 'mask' chunk type encountered.");

                    case ChunkType.Path:
                        throw new ArgumentException("Unused and unknown 'path' chunk type encountered.");

                    default:
                        throw new ArgumentException($"Unknown chunk type {chunk.ChunkType} encountered.");
                }
            }
        }
    }
}
