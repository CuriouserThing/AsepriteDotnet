using System.Collections.Generic;

namespace Aseprite
{
    public interface ILayerGroup
    {
        IReadOnlyList<GroupLayer> Subgroups { get; }
        IReadOnlyList<ImageLayer> Images { get; }
    }
}
