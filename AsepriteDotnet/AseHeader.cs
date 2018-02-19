using System;
using System.IO;

namespace Aseprite
{
    public class AseHeader
    {
        private const int HEADER_SIZE = 128;
        private const ushort MAGIC = 0xA5E0;

        public uint FileSize { get; private set; }
        public ushort MagicNumber { get; private set; }
        public ushort FrameCount { get; private set; }
        public ushort ImagePixelWidth { get; private set; }
        public ushort ImagePixelHeight { get; private set; }
        public ushort ColorDepth { get; private set; }
        public uint Flags { get; private set; }
        public ushort Speed { get; private set; }
        public byte TransparentColorIndex { get; private set; }
        public ushort ColorCount { get; private set; }
        public byte PixelRatioWidth { get; private set; }
        public byte PixelRatioHeight { get; private set; }

        internal AseHeader(BinaryReader reader)
        {
            var data = reader.ReadBytes(HEADER_SIZE);
            using (var stream = new MemoryStream(data))
            using (reader = new BinaryReader(stream))
            {
                FileSize
                    = reader.ReadUInt32();
                MagicNumber
                    = reader.ReadUInt16();
                FrameCount
                    = reader.ReadUInt16();
                ImagePixelWidth
                    = reader.ReadUInt16();
                ImagePixelHeight
                    = reader.ReadUInt16();
                ColorDepth
                    = reader.ReadUInt16();
                var flags
                    = reader.ReadUInt32();
                Speed
                    = reader.ReadUInt16();
                var zero1
                    = reader.ReadUInt32();
                var zero2
                    = reader.ReadUInt32();
                TransparentColorIndex
                    = reader.ReadByte();
                var ignore
                    = reader.ReadBytes(3);
                ColorCount
                    = reader.ReadUInt16();
                PixelRatioWidth
                    = reader.ReadByte();
                PixelRatioHeight
                    = reader.ReadByte();

                if (MagicNumber != MAGIC)
                {
                    throw new ArgumentException($"Magic number {MAGIC} expected in ASE file header.");
                }
                if (ColorCount == 0)
                {
                    ColorCount = 256;
                }
                if (PixelRatioWidth == 0 || PixelRatioHeight == 0)
                {
                    PixelRatioWidth = 1;
                    PixelRatioHeight = 1;
                }
            }
        }
    }
}
