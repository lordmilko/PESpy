using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRREGREL"/> structure.
    /// </summary>
    public readonly unsafe struct AttrRegRel : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int typindOffset = 8;
        private const int regOffset = 12;
        private const int attrOffset = 14;
        private const int nameOffset = 22;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRREGREL* value;

        public static implicit operator SymType(AttrRegRel value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="ATTRREGREL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRREGREL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRREGREL.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="ATTRREGREL.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="ATTRREGREL.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="ATTRREGREL.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRREGREL.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(int)    + //typind
            sizeof(short)  + //reg
            8;               //attr

        internal int BytesUsed() => FixedStructSize + name.Length + 1;

        internal AttrRegRel(ATTRREGREL* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ATTRREGREL, this, ViewKind.AttrRegRel, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(7, BytesUsed());

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
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 3:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 4:
                    structWriter.WriteField(nameof(reg), regOffset, reg, sizeof(ushort));
                    break;

                case 5:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 6:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 7:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
