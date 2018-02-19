using System.IO;

namespace Aseprite
{
    public class ImageLayer : Layer
    {
        public byte Opacity => opacity;
        public BlendMode Mode => mode;

        public bool IsBackground
            => HasFlag(0x08);

        public bool PrefersLinkedCels
            => HasFlag(0x10);

        public bool IsReference
            => HasFlag(0x40);

        protected ImageLayer(BinaryReader reader, ushort flags, Ase ase) : base(reader, flags, ase)
        {
        }

        internal static ImageLayer FromReader(BinaryReader reader, ushort flags, Ase ase)
            => new ImageLayer(reader, flags, ase);
    }
}
