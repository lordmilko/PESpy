using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEasy"/> structure.
    /// </summary>
    public readonly unsafe struct LfEasy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEasy* value;

        public ushort typlen
        {
            get
            {
                //Leaves >= 0x200 but less than 0x1000 and >= 0x1200 and < 1500 are only referenced from other type records,
                //and therefore don't have lengths (cvinfo.h). We store all leaves as TypType, which causes an issue
                //when it comes to asking for their lengths, so we need to do this check

                var leaf = (ushort) value->leaf;

                if (leaf >= 0x200 && leaf < 0x1000 || leaf >= 0x1200 && leaf < 0x1500)
                    return 0;

                return *(ushort*) ((byte*) value - 2);
            }
        }

        public LEAF_ENUM_e leaf => value->leaf;

        internal const int StructSize =
            sizeof(ushort);  //leaf

        internal LfEasy(lfEasy* value)
        {
            this.value = value;
        }
    }
}
