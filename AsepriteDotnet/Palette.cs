using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Aseprite
{
    public class Palette : IReadOnlyList<Color>
    {
        private List<Color> colors;
        private int transparentIndex;

        internal Palette(int transparentIndex)
        {
            colors = new List<Color>();
            this.transparentIndex = transparentIndex;
        }

        public Color this[int index]
        {
            get
            {
                if (index == transparentIndex)
                    return Color.Transparent;
                else
                    return colors[index];
            }
            private set
            {
                while (colors.Count <= index)
                {
                    colors.Add(default);
                }
                colors[index] = value;
            }
        }

        public int Count
            => colors.Count;

        public IEnumerator<Color> GetEnumerator()
            => colors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => colors.GetEnumerator();

        internal void AddFromNewChunk(Chunk chunk)
        {
            using (var reader = chunk.GetDataReader())
            {
                uint size = reader.ReadUInt32();
                while (colors.Count < size)
                {
                    colors.Add(default);
                }

                int first
                    = (int)reader.ReadUInt32();
                int last
                    = (int)reader.ReadUInt32();
                var future
                    = reader.ReadBytes(8);

                for (int i = first; i <= last; ++i)
                {
                    bool hasName
                        = (reader.ReadUInt16() & 0x1) == 1;
                    byte r
                        = reader.ReadByte();
                    byte g
                        = reader.ReadByte();
                    byte b
                        = reader.ReadByte();
                    byte a
                        = reader.ReadByte();
                    if (hasName)
                        Ase.ReadString(reader);

                    colors[i] = Color.FromArgb(a, r, g, b);
                }
            }
        }

        internal void AddFromOldChunk(Chunk chunk)
        {
            using (var reader = chunk.GetDataReader())
            {
                int index = 0;
                int packetCount = reader.ReadUInt16();
                for (int i = 0; i < packetCount; ++i)
                {
                    index += reader.ReadByte(); // number of entries to skip
                    int colorCount = reader.ReadByte();
                    if (colorCount == 0)
                        colorCount = 256;
                    for (int j = 0; j < colorCount; ++j)
                    {
                        this[index++] = Color.FromArgb(
                            reader.ReadByte(),
                            reader.ReadByte(),
                            reader.ReadByte());
                    }
                }
            }
        }
    }
}
