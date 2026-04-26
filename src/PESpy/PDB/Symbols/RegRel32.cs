using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGREL32"/> structure.
    /// </summary>
    public readonly unsafe struct RegRel32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int typindOffset = 8;
        private const int regOffset = 12;
        private const int nameOffset = 14;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGREL32* value;

        public static implicit operator SymType(RegRel32 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="REGREL32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REGREL32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REGREL32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="REGREL32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="REGREL32.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="REGREL32.name"/>
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
            sizeof(short);   //reg

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal RegRel32(REGREL32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.RegRel32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(reg), regOffset, reg, sizeof(ushort));
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
