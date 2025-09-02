using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBuildInfo"/> structure.
    /// </summary>
    public readonly unsafe struct LfBuildInfo
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBuildInfo* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumTypeList<CV_ItemId> arg => new TypOrEnumTypeList<CV_ItemId>(new NativeSpan<CV_ItemId>(value->arg, count)); //You can index into this using CV_BuildInfo_e

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfBuildInfo(lfBuildInfo* value)
        {
            this.value = value;
        }
    }
}
