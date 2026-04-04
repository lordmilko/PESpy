using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FILESTATICSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FileStaticSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int modOffsetOffset = 8;
        private const int flagsOffset = 12;
        private const int nameOffset = 14;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FILESTATICSYM* value;

        public static implicit operator SymType(FileStaticSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="FILESTATICSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FILESTATICSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FILESTATICSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="FILESTATICSYM.modOffset"/>
        public CV_uoff32_t modOffset => value->modOffset;

        /// <inheritdoc cref="FILESTATICSYM.flags"/>
        public CV_LVARFLAGS flags => value->flags;

        /// <inheritdoc cref="FILESTATICSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(uint)   + //modOffset
            2;               //flags

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal FileStaticSym(FILESTATICSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.FILESTATICSYM, this, ViewKind.FileStaticSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 3:
                    structWriter.WriteField(nameof(modOffset), modOffsetOffset, modOffset);
                    break;

                case 4:
                    structWriter.WriteField(nameof(flags), flagsOffset, flags);
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
