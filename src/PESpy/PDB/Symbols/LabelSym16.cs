using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LABELSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct LabelSym16 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int segOffset = 6;
        private const int flagsOffset = 8;
        private const int nameOffset = 9;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LABELSYM16* value;

        /// <inheritdoc cref="LABELSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="LABELSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="LABELSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="LABELSYM16.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="LABELSYM16.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="LABELSYM16.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            1;               //flags

        private int BytesUsed => FixedStructSize + name.Length + 1;

        internal LabelSym16(LABELSYM16* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.LABELSYM16, this, ViewKind.LabelSym16, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren => StructWriter.GetNumChildrenAlign4(6, BytesUsed);

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
                    structWriter.AlignOrThrow(BytesUsed);
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
