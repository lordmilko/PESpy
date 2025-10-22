using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfRefSym"/> structure.
    /// </summary>
    public readonly unsafe struct LfRefSym : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfRefSym* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfRefSym(lfRefSym* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read Sym");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfRefSym, this, ViewKind.LfRefSym, typlen + sizeof(short));

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
