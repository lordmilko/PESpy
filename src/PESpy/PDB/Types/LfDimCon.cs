using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimCon"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimCon : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typOffset = 4;
        private const int rankOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimCon* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType typ => new TypOrEnumType((byte*) value, value->typ);

        public short rank => value->rank;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //typ
            sizeof(short);   //rank

        internal LfDimCon(lfDimCon* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }

        public static implicit operator LfEasy(LfDimCon easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfDimCon, this, ViewKind.LfDimCon, typlen + sizeof(short));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(typ), typOffset, value->typ);
                    break;

                case 2:
                    structWriter.WriteField(nameof(rank), rankOffset, rank);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
