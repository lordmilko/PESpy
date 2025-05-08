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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_PUBSYMFLAGS pubsymflags => value->pubsymflags;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        /* There is no way to get the "underlying" symbol of a PubSym32. The public symbol specifies a section
         * and offset, which can be used to calculate its RVA. It does _not_ behave similarly to a RefSym.
         * You cannot use the section or offset to lookup the "underlying" symbol from a module. You _can_
         * get the module that is associated with a given section and offset (based on the section contribs),
         * but that's as far as you can get */

        #endregion

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

