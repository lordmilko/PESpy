using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DISCARDEDSYM"/> structure.
    /// </summary>
    public readonly unsafe struct DiscardedSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DISCARDEDSYM* value;

        /// <inheritdoc cref="DISCARDEDSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DISCARDEDSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DISCARDEDSYM.discarded"/>
        public CV_DISCARDED_e discarded => value->discarded;

        /// <inheritdoc cref="DISCARDEDSYM.reserved"/>
        public int reserved => value->reserved;

        /// <inheritdoc cref="DISCARDEDSYM.fileid"/>
        public int fileid => value->fileid;

        /// <inheritdoc cref="DISCARDEDSYM.linenum"/>
        public int linenum => value->linenum;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //discardedData
            sizeof(int)    + //fileid
            sizeof(int);     //linenum

        internal DiscardedSym(DISCARDEDSYM* value)
        {
            this.value = value;
            Debug.Assert(false, "Read data");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DISCARDEDSYM, this, ViewKind.DiscardedSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(discarded), discarded, sizeof(byte));
            s.WriteField(nameof(reserved), reserved);
            s.WriteField(nameof(fileid), fileid);
            s.WriteField(nameof(linenum), linenum);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
