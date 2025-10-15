using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CEXMSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct CExMSym16 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CEXMSYM16* value;

        /// <inheritdoc cref="CEXMSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CEXMSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CEXMSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="CEXMSYM16.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="CEXMSYM16.model"/>
        public short model => value->model;

        /// <inheritdoc cref="CEXMSYM16.pcdtable"/>
        public CV_uoff16_t pcdtable => value->pcdtable;

        /// <inheritdoc cref="CEXMSYM16.pcdspi"/>
        public CV_uoff16_t pcdspi => value->pcdspi;

        /// <inheritdoc cref="CEXMSYM16.subtype"/>
        public CV_COBOL_e subtype => value->subtype;

        /// <inheritdoc cref="CEXMSYM16.flag"/>
        public short flag => value->flag;

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            sizeof(short)  + //model
            sizeof(ushort) + //pcdtable
            sizeof(ushort) + //pcdspi
            sizeof(ushort) + //subtype
            sizeof(short);   //flag

        internal CExMSym16(CEXMSYM16* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.CEXMSYM16, this, ViewKind.CExMSym16, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(model), model);
            s.WriteField(nameof(pcdtable), pcdtable);
            s.WriteField(nameof(pcdspi), pcdspi);
            s.WriteField(nameof(subtype), subtype, sizeof(short));
            s.WriteField(nameof(flag), flag);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
