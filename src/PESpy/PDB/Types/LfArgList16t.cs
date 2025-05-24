using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArgList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfArgList16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArgList_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public Span<CV_typ16_t> arg => new Span<CV_typ16_t>(value->arg, count);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfArgList16t(lfArgList_16t* value)
        {
            this.value = value;
        }
    }
}
