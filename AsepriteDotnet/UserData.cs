using System;
using System.Drawing;

namespace Aseprite
{
    public class UserData
    {
        readonly public bool HasText, HasColor;
        readonly public string Text;
        readonly public Color Color;

        public UserData()
        {
            HasText = false;
            HasColor = false;
            Text = String.Empty;
            Color = default;
        }

        public UserData(string text)
        {
            HasText = true;
            HasColor = false;
            Text = text;
            Color = default;
        }

        public UserData(Color color)
        {
            HasText = false;
            HasColor = true;
            Text = String.Empty;
            Color = color;
        }

        public UserData(string text, Color color)
        {
            HasText = true;
            HasColor = true;
            Text = text;
            Color = color;
        }

        internal static UserData FromChunk(Chunk chunk)
        {
            using (var reader = chunk.GetDataReader())
            {
                switch (reader.ReadUInt32())
                {
                    case 0b00:
                        return new UserData();

                    case 0b01:
                        return new UserData(Ase.ReadString(reader));

                    case 0b10:
                        return new UserData(Colorizing.ReadColor(reader));

                    case 0b11:
                        return new UserData(Ase.ReadString(reader), Colorizing.ReadColor(reader));

                    default:
                        throw new ArgumentException($"Unknown user data flags encountered at {reader.BaseStream.Position - 4}.");
                }
            }
        }
    }
}
