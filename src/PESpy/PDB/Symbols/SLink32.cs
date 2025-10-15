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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(framesize), framesize);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(reg), reg, sizeof(ushort));

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
