using System.IO;

namespace Aseprite
{
    internal class Chunk
    {
        public int FrameIndex;
        public ChunkType ChunkType;
        public byte[] Data;

        public Chunk(int frame, ChunkType type, byte[] data)
        {
            FrameIndex = frame;
            ChunkType = type;
            Data = data;
        }

        static public Chunk FromReader(BinaryReader reader, int frame)
        {
            var chunkSize
                = reader.ReadUInt32();
            var chunkType
                = reader.ReadUInt16();
            var chunkBytes
                = reader.ReadBytes((int)chunkSize - 6);
            return new Chunk(frame, (ChunkType)chunkType, chunkBytes);
        }

        public BinaryReader GetDataReader()
        {
            return new BinaryReader(new MemoryStream(Data));
        }
    }
}
