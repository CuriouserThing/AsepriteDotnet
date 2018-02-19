using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aseprite
{
    public class GroupLayer : Layer, ILayerGroup
    {
        public IReadOnlyList<Layer> Children => Ase.Layers
            .Where(l => l.Parent == this)
            .ToArray();

        public IReadOnlyList<ImageLayer> Images => Ase.Layers
            .Where(l => l is ImageLayer && l.Parent == this)
            .Select(l => l as ImageLayer)
            .ToArray();

        public IReadOnlyList<GroupLayer> Subgroups => Ase.Layers
            .Where(l => l is GroupLayer && l.Parent == this)
            .Select(l => l as GroupLayer)
            .ToArray();

        public bool IsCollapsed
            => HasFlag(0x20);

        protected GroupLayer(BinaryReader reader, ushort flags, Ase ase) : base(reader, flags, ase)
        {
        }

        internal static GroupLayer FromReader(BinaryReader reader, ushort flags, Ase ase)
            => new GroupLayer(reader, flags, ase);
    }
}
