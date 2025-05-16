using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="UNAMESPACE"/> structure.
    /// </summary>
    public readonly unsafe struct UNameSpace
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UNAMESPACE* value;

        /// <inheritdoc cref="UNAMESPACE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="UNAMESPACE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="UNAMESPACE.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        internal UNameSpace(UNAMESPACE* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

