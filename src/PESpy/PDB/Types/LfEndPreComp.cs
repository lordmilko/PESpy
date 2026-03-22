using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEndPreComp"/> structure.
    /// </summary>
    public readonly unsafe struct LfEndPreComp : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int signatureOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEndPreComp* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int signature => value->signature;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //signature

        internal LfEndPreComp(lfEndPreComp* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfEndPreComp easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfEndPreComp, this, ViewKind.LfEndPreComp, typlen + sizeof(short));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, typlen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(signature), signatureOffset, signature);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
