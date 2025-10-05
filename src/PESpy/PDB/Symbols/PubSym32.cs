using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PUBSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct PubSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PUBSYM32* value;

        /// <inheritdoc cref="PUBSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PUBSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PUBSYM32.pubsymflags"/>
        public CV_PUBSYMFLAGS pubsymflags => value->pubsymflags;

        /// <inheritdoc cref="PUBSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PUBSYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PUBSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        /* There is no way to get the "underlying" symbol of a PubSym32. The public symbol specifies a section
         * and offset, which can be used to calculate its RVA. It does _not_ behave similarly to a RefSym.
         * You cannot use the section or offset to lookup the "underlying" symbol from a module. You _can_
         * get the module that is associated with a given section and offset (based on the section contribs),
         * but that's as far as you can get */

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            4              + //pubsymflags
            sizeof(uint)   + //off
            sizeof(short);   //seg

        internal PubSym32(PUBSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

