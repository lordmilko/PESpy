using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ALIGNSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AlignSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ALIGNSYM* value;

        /// <inheritdoc cref="ALIGNSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ALIGNSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        //No fields

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        internal AlignSym(ALIGNSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ALIGNSYM, this, ViewKind.AlignSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(reclen), reclenOffset, reclen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(rectyp), rectypOffset, rectyp, sizeof(ushort));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
