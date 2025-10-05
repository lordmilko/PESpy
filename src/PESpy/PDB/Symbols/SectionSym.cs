using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SECTIONSYM"/> structure.
    /// </summary>
    public readonly unsafe struct SectionSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SECTIONSYM* value;

        /// <inheritdoc cref="SECTIONSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SECTIONSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SECTIONSYM.isec"/>
        public short isec => value->isec;

        /// <inheritdoc cref="SECTIONSYM.align"/>
        public byte align => value->align;

        /// <inheritdoc cref="SECTIONSYM.bReserved"/>
        public byte bReserved => value->bReserved;

        /// <inheritdoc cref="SECTIONSYM.rva"/>
        public int rva => value->rva;

        /// <inheritdoc cref="SECTIONSYM.cb"/>
        public int cb => value->cb;

        /// <inheritdoc cref="SECTIONSYM.characteristics"/>
        public IMAGE_SCN characteristics => value->characteristics;

        /// <inheritdoc cref="SECTIONSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //isec
            sizeof(byte)   + //align
            sizeof(byte)   + //bReserved
            sizeof(int)    + //rva
            sizeof(int)    + //cb
            sizeof(int);     //characteristics

        internal SectionSym(SECTIONSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

