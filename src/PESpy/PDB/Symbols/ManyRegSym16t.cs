using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANYREGSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ManyRegSym16t : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int countOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANYREGSYM_16t* value;

        public static implicit operator SymType(ManyRegSym16t value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="MANYREGSYM_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANYREGSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANYREGSYM_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="MANYREGSYM_16t.count"/>
        public byte count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //typind
            sizeof(byte);    //count

        internal ManyRegSym16t(MANYREGSYM_16t* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.MANYREGSYM_16t, this, ViewKind.ManyRegSym16t, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
