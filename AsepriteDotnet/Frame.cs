using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Aseprite
{
    public abstract class Frame
    {
        protected FrameHeader header;

        public ushort MillisecondDuration => header.MillisecondDuration;
        public Ase Ase { get; protected set; }

        internal Frame(FrameHeader header, Ase ase)
        {
            this.header = header;
            Ase = ase;
        }

        public abstract IReadOnlyList<Cel> Cels { get; }

        public abstract void Render(Action<Point, Color> renderCallback);
    }

    internal class Frame<T> : Frame where T : struct
    {
        private Colorizer<T> colorize;

        private List<Cel<T>> cels = new List<Cel<T>>();
        public override IReadOnlyList<Cel> Cels => cels;

        public Cel<T> GetCel(Layer layer)
            => cels.First(c => c.Layer == layer);

        public void AddCel(Cel<T> cel)
            => cels.Add(cel);

        public Frame(FrameHeader header, Ase<T> ase) : base(header, ase)
        {
            colorize = ase.GetColorizer(ase.Palette);
        }

        public void Render(T[,] dest, Point destOffset, bool renderReferenceLayers = false)
        {
            for (var i = 0; i < Cels.Count; ++i)
            {
                var cel = cels[i];
                if (cel.Layer.IsGloballyVisible && (renderReferenceLayers || !cel.Layer.IsReference))
                    cel.RenderCanvassed(dest, destOffset);
            }
        }

        public void Render(T[,] dest)
        {
            Render(dest, new Point(0, 0));
        }

        public T[,] Render()
        {
            var output = new T[Ase.ImagePixelWidth, Ase.ImagePixelHeight];
            Render(output, new Point(0, 0));
            return output;
        }

        public override void Render(Action<Point, Color> renderCallback)
        {
            T[,] values = Render();
            int width = values.GetLength(0);
            int height = values.GetLength(1);
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    renderCallback(new Point(x, y), colorize(values[x, y]));
                }
            }
        }
    }
}
