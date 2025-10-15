using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SLINK32"/> structure.
    /// </summary>
    public readonly unsafe struct SLink32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int framesizeOffset = 4;
        private const int offOffset = 8;
        private const int regOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SLINK32* value;

        /// <inheritdoc cref="SLINK32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SLINK32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SLINK32.framesize"/>
        public int framesize => value->framesize;

        /// <inheritdoc cref="SLINK32.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="SLINK32.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //framesize
            sizeof(int)    + //off
            sizeof(short);   //reg

        internal SLink32(SLINK32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.SLINK32, this, ViewKind.SLink32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren => 5;

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
                    structWriter.WriteField(nameof(framesize), framesizeOffset, framesize);
                    break;

                case 3:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 4:
                    structWriter.WriteField(nameof(reg), regOffset, reg, sizeof(ushort));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
