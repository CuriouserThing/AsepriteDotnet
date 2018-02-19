using System.Drawing;

namespace Aseprite
{
    internal struct GrayColor
    {
        public readonly byte V, A;

        public GrayColor(byte v, byte a)
        {
            V = v;
            A = a;
        }

        public GrayColor(int v, int a)
        {
            V = (byte)v;
            A = (byte)a;
        }

        public Color ToColor()
            => Color.FromArgb(A, V, V, V);
    }
}
