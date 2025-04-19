using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVBClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfVBClass
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVBClass* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public CV_typ_t index => value->index;

        public CV_typ_t vbptr => value->vbptr;

        internal LfVBClass(lfVBClass* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read vbpoff");
        }
    }
}
