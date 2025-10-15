using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CALLSITEINFO"/> structure.
    /// </summary>
    public readonly unsafe struct CallSiteInfo : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int sectOffset = 8;
        private const int __reserved_0Offset = 10;
        private const int typindOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CALLSITEINFO* value;

        /// <inheritdoc cref="CALLSITEINFO.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CALLSITEINFO.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CALLSITEINFO.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="CALLSITEINFO.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="CALLSITEINFO.__reserved_0"/>
        public short __reserved_0 => value->__reserved_0;

        /// <inheritdoc cref="CALLSITEINFO.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(short)  + //sect
            sizeof(short)  + //__reserved_0
            sizeof(int);     //typind

        internal CallSiteInfo(CALLSITEINFO* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.CALLSITEINFO, this, ViewKind.CallSiteInfo, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren => 6;

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
                    structWriter.WriteField(nameof(sect), sectOffset, sect);
                    break;

                case 4:
                    structWriter.WriteField(nameof(__reserved_0), __reserved_0Offset, __reserved_0);
                    break;

                case 5:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
