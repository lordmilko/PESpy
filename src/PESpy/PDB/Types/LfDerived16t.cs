using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDerived_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDerived16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDerived_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfDerived16t(lfDerived_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read drvdcls");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfDerived_16t, this, ViewKind.LfDerived16t, typlen + sizeof(short));

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
