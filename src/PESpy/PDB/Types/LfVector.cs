using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVector"/> structure.
    /// </summary>
    public readonly unsafe struct LfVector : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int elemtypeOffset = 4;
        private const int countOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVector* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType elemtype => new TypOrEnumType((byte*) value, value->elemtype);

        public int count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //elemtype
            sizeof(int);     //count

        internal LfVector(lfVector* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        public static implicit operator LfEasy(LfVector easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfVector, typlen + sizeof(short));

        int IViewable.NumChildren() => 4;

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
                    structWriter.WriteField(nameof(elemtype), elemtypeOffset, value->elemtype);
                    break;

                case 3:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
