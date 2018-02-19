using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Aseprite
{
    public enum AnimationDirection { Forward, Reverse, PingPong }

    public class FrameTag
    {
        public ushort FromFrame { get; protected set; }
        public ushort ToFrame { get; protected set; }
        public AnimationDirection Direction { get; protected set; }
        public Color Color { get; protected set; }
        public string Name { get; protected set; }

        public FrameTag(ushort fromFrame, ushort toFrame, AnimationDirection direction, Color color, string name)
        {
            FromFrame = fromFrame;
            ToFrame = toFrame;
            Direction = direction;
            Color = color;
            Name = name;
        }

        private static FrameTag FromReader(BinaryReader reader)
        {
            ushort fromFrame
                = reader.ReadUInt16();
            ushort toFrame
                = reader.ReadUInt16();
            var direction
                = reader.ReadByte();
            var future
                = reader.ReadBytes(8);
            Color color
                = Color.FromArgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            var zero
                = reader.ReadByte();
            string tagName
                = Ase.ReadString(reader);

            return new FrameTag(fromFrame, toFrame, (AnimationDirection)direction, color, tagName);
        }

        internal static IEnumerable<FrameTag> TagsFromChunk(Chunk chunk)
        {
            using (var reader = chunk.GetDataReader())
            {
                ushort tagNumber
                    = reader.ReadUInt16();
                var future
                    = reader.ReadBytes(8);
                for (var j = 0; j < tagNumber; ++j)
                {
                    yield return FromReader(reader);
                }
            }
        }
    }
}
