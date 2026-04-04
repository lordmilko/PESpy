using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BLOCKSYM"/> structure, which
    /// represents the common fields between all block symbols.
    /// </summary>
    public readonly unsafe struct BlockSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BLOCKSYM* value;

        public static implicit operator SymType(BlockSym value) => new SymType((SYMTYPE*) value.value);
        public static implicit operator BlockSym(BLOCKSYM* value) => new BlockSym(value);

        /// <inheritdoc cref="BLOCKSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BLOCKSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BLOCKSYM16.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="BLOCKSYM16.pEnd"/>
        public int pEnd => value->pEnd;

        #region PESpy

        public SymTypeChildList Children => GetChildren(null);

        public SymTypeChildList GetChildren(ICodeViewModuleAccessor? codeViewModuleAccessor) => new SymTypeChildList((BLOCKSYM*) value, codeViewModuleAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        public bool Contains(SymType symType, ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            var end = (long) codeViewModuleAccessor.Symbols.start + pEnd;

            var addr = (long) (SYMTYPE*) symType;

            return addr > (long) value && addr < end;
        }

        #endregion

        internal BlockSym(BLOCKSYM* value)
        {
            this.value = value;
        }
    }
}
