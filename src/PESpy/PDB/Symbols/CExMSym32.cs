using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CEXMSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct CExMSym32 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CEXMSYM32* value;

        /// <inheritdoc cref="CEXMSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CEXMSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CEXMSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="CEXMSYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="CEXMSYM32.model"/>
        public short model => value->model;

        /// <inheritdoc cref="CEXMSYM32.pcdtable"/>
        public CV_uoff32_t pcdtable => value->pcdtable;

        /// <inheritdoc cref="CEXMSYM32.pcdspi"/>
        public CV_uoff32_t pcdspi => value->pcdspi;

        /// <inheritdoc cref="CEXMSYM32.subtype"/>
        public CV_COBOL_e subtype => value->subtype;

        /// <inheritdoc cref="CEXMSYM32.flag"/>
        public short flag => value->flag;

        /// <inheritdoc cref="CEXMSYM32.calltableOff"/>
        public CV_uoff32_t calltableOff => value->calltableOff;

        /// <inheritdoc cref="CEXMSYM32.calltableSeg"/>
        public short calltableSeg => value->calltableSeg;

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //model
            sizeof(uint)   + //pcdtable
            sizeof(uint)   + //pcdspi
            sizeof(ushort) + //subtype
            sizeof(short)  + //flag
            sizeof(uint)   + //calltableOff
            sizeof(short);   //calltableSeg

        internal CExMSym32(CEXMSYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.CEXMSYM32, this, ViewKind.CExMSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
            s.WriteField(nameof(calltableOff), calltableOff);
            s.WriteField(nameof(calltableSeg), calltableSeg);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
