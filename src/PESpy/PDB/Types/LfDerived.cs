using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDerived"/> structure.
    /// </summary>
    public readonly unsafe struct LfDerived : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDerived* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //count

        internal LfDerived(lfDerived* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read drvdcls");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfDerived, this, ViewKind.LfDerived, typlen + sizeof(short));

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
