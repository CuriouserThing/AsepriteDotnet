using System.Drawing;
using System.IO;

namespace Aseprite
{
    internal delegate Color Colorizer<T>(T value) where T : struct;

    internal static class Colorizing
    {
        public static byte ReadIndex(BinaryReader reader)
        {
            return reader.ReadByte();
        }

        public static GrayColor ReadGray(BinaryReader reader)
        {
            byte v = reader.ReadByte();
            byte a = reader.ReadByte();
            return new GrayColor(v, a);
        }

        public static Color ReadColor(BinaryReader reader)
        {
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            byte a = reader.ReadByte();
            return Color.FromArgb(a, r, g, b);
        }

        public static Colorizer<byte> GetIndexColorizer(Palette palette)
        {
            return index => palette[index];
        }

        public static Colorizer<GrayColor> GetGrayColorizer(Palette palette)
        {
            return gray => gray.ToColor();
        }

        public static Colorizer<Color> GetColorPassthrough(Palette palette)
        {
            return color => color;
        }
    }
}
