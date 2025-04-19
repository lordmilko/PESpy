using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPad"/> structure.
    /// </summary>
    public readonly unsafe struct LfPad
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPad* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        //This type only occupies 1 byte. e.g. it can be pointed to by a lfFieldList.
        //I wouldn't expect it to actually have a TYPTYPE behind it
        public LEAF_ENUM_e leaf => (LEAF_ENUM_e) value->leaf;

        internal LfPad(lfPad* value)
        {
            this.value = value;
        }
    }
}
