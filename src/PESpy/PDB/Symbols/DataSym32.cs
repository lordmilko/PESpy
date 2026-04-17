using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM32"/> structure.
    /// </summary>
    public readonly unsafe struct DataSym32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int offOffset = 8;
        private const int segOffset = 12;
        private const int nameOffset = 14;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM32* value;

        public static implicit operator SymType(DataSym32 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DATASYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYM32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="DATASYM32.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="DATASYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        //If seg is 0, there's no RVA
        /// <inheritdoc cref="AnnotationSym.RelativeVirtualAddress"/>
        public int? RelativeVirtualAddress => SymType.GetOmapRelativeVirtualAddress(value, seg, off);

        /// <inheritdoc cref="AnnotationSym.RawRelativeVirtualAddress"/>
        public int? RawRelativeVirtualAddress => SymType.GetRawRelativeVirtualAddress(value, seg, off);

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(uint)   + //off
            sizeof(short);   //seg

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal DataSym32(DATASYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYM32, this, ViewKind.DataSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 4:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
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
