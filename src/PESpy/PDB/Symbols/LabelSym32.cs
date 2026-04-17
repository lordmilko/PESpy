using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LABELSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct LabelSym32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int segOffset = 8;
        private const int flagsOffset = 10;
        private const int nameOffset = 11;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LABELSYM32* value;

        public static implicit operator SymType(LabelSym32 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="LABELSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="LABELSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="LABELSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="LABELSYM32.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="LABELSYM32.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="LABELSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

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
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            1;               //flags

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal LabelSym32(LABELSYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.LABELSYM32, this, ViewKind.LabelSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(seg), segOffset, seg);
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
