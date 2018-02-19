using System;
using System.IO;

namespace Aseprite
{
    public class FrameHeader
    {
        private const int HEADER_SIZE = 16;
        private const ushort MAGIC = 0xF1FA;

        public uint ByteSize { get; private set; }
        public ushort MagicNumber { get; private set; }
        public ushort ChunkCount { get; private set; }
        public ushort MillisecondDuration { get; private set; }

        internal FrameHeader(BinaryReader reader)
        {
            var data = reader.ReadBytes(HEADER_SIZE);
            using (var stream = new MemoryStream(data))
            using (reader = new BinaryReader(stream))
            {
                ByteSize
                    = reader.ReadUInt32();
                MagicNumber
                    = reader.ReadUInt16();
                ChunkCount
                    = reader.ReadUInt16();
                MillisecondDuration
                    = reader.ReadUInt16();

                if (MagicNumber != MAGIC)
                {
                    throw new ArgumentException($"Magic number {MAGIC} expected in frame header.");
                }
            }
        }
    }
}
