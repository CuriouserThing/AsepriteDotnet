using System;
using System.IO;
using System.Linq;

namespace Aseprite
{
    public abstract class Layer : UserDataHolder
    {
        private ushort flags;
        protected byte opacity;
        protected BlendMode mode;

        public int Level { get; protected set; }
        public GroupLayer Parent { get; protected set; }
        public string Name { get; protected set; }
        public Ase Ase { get; protected set; }

        internal static Layer FromChunk(Chunk chunk, Ase ase)
        {
            using (var reader = chunk.GetDataReader())
            {
                ushort flags
                    = reader.ReadUInt16();
                var layerType
                    = reader.ReadUInt16();
                switch (layerType)
                {
                    case 0:
                        return ImageLayer.FromReader(reader, flags, ase);

                    case 1:
                        return GroupLayer.FromReader(reader, flags, ase);

                    default:
                        throw new ArgumentException($"{layerType} is not a valid layer type.");
                }
            }
        }

        protected Layer(BinaryReader reader, ushort flags, Ase ase)
        {
            Level
                = reader.ReadUInt16();
            var defaultWidth
                = reader.ReadUInt16();
            var defaultHeight
                = reader.ReadUInt16();
            mode
                = (BlendMode)reader.ReadUInt16();
            opacity
                = reader.ReadByte();
            var future
                = reader.ReadBytes(3);
            Name
                = Ase.ReadString(reader);

            this.flags = flags;
            Ase = ase;

            if (Level == 0)
            {
                Parent = null;
            }
            else
            {
                Parent = ase.Layers.Last() as GroupLayer;

                if (Parent == null)
                    throw new ArgumentException($"Layer at level {Level} (i.e. greater than 0) expects a non-null previous layer.");

                var delta = Level - Parent.Level;
                if (delta > 1)
                    throw new ArgumentException($"Level difference between this Layer and the previous is {delta} (i.e. greater than 1).");

                try
                {
                    while (delta++ < 1)
                    {
                        Parent = Parent.Parent;
                    }
                    Parent.GetHashCode(); // smelly null check
                }
                catch (NullReferenceException ex)
                {
                    throw new ArgumentNullException("Layer heirarchy is broken.", ex);
                }
            }
        }

        protected bool HasFlag(int mask) => (flags & mask) == mask;

        public bool IsVisible
            => HasFlag(0x01);

        public bool IsEditable
            => HasFlag(0x02);

        public bool IsLocked
            => HasFlag(0x04);

        private bool AllAncestorsHaveFlag(int mask)
        {
            Layer layer = Parent;
            while (layer != null)
            {
                if (!layer.HasFlag(mask))
                    return false;
                layer = layer.Parent;
            }
            return true;
        }

        public bool IsGloballyVisible
            => IsVisible && AllAncestorsHaveFlag(0x01);

        public bool IsGloballyEditable
            => IsEditable && AllAncestorsHaveFlag(0x02);

        private bool AnyAncestorHasFlag(int mask)
        {
            Layer layer = Parent;
            while (layer != null)
            {
                if (layer.HasFlag(mask))
                    return true;
                layer = layer.Parent;
            }
            return false;
        }

        public bool IsGloballyLocked
            => IsLocked || AnyAncestorHasFlag(0x04);

        public bool IsWithinCollapsedGroup
            => AnyAncestorHasFlag(0x20);
    }
}
