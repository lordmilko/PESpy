using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANYREGSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct ManyRegSym2 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int countOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANYREGSYM2* value;

        public static implicit operator SymType(ManyRegSym2 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="MANYREGSYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANYREGSYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANYREGSYM2.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="MANYREGSYM2.count"/>
        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(short);   //count

        internal ManyRegSym2(MANYREGSYM2* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg"); //CV_HREG_e?
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.MANYREGSYM2, this, ViewKind.ManyRegSym2, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 4;

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

                case 2:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
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
