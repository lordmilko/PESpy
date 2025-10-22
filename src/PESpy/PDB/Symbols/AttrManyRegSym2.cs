using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRMANYREGSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct AttrManyRegSym2 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int attrOffset = 8;
        private const int countOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRMANYREGSYM2* value;

        /// <inheritdoc cref="ATTRMANYREGSYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRMANYREGSYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRMANYREGSYM2.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="ATTRMANYREGSYM2.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRMANYREGSYM2.count"/>
        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            8              + //attr
            sizeof(short);   //count

        internal AttrManyRegSym2(ATTRMANYREGSYM2* value)
        {
            this.value = value;
            Debug.Assert(false, "Implement reg and name, which are both variable length arrays"); //CV_HREG_e?
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ATTRMANYREGSYM2, this, ViewKind.AttrManyRegSym2, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 5;

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
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 4:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
