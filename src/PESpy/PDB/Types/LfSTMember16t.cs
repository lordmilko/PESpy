using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfSTMember_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfSTMember16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfSTMember_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t index => value->index;

        public CV_fldattr_t attr => value->attr;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfSTMember16t(lfSTMember_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
