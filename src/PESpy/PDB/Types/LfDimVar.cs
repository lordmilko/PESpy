using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimVar"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimVar : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int rankOffset = 4;
        private const int typOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimVar* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int rank => value->rank;

        public TypOrEnumType typ => new TypOrEnumType((byte*) value, value->typ);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //rank
            sizeof(int);     //typ

        internal LfDimVar(lfDimVar* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfDimVar, this, ViewKind.LfDimVar, typlen + sizeof(short));

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(rank), rankOffset, rank);
                    break;

                case 2:
                    structWriter.WriteField(nameof(typ), typOffset, value->typ);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
