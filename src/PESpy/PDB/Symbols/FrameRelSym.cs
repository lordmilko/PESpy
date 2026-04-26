using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMERELSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FrameRelSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int typindOffset = 8;
        private const int attrOffset = 12;
        private const int nameOffset = 20;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMERELSYM* value;

        public static implicit operator SymType(FrameRelSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="FRAMERELSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FRAMERELSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FRAMERELSYM.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="FRAMERELSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="FRAMERELSYM.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="FRAMERELSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(int)    + //typind
            8;               //attr

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal FrameRelSym(FRAMERELSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.FrameRelSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(6, BytesUsed());

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
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 5:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 6:
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
