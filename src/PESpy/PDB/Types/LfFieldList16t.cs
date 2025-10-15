using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFieldList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfFieldList16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFieldList_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public LfEasy[] fields => LfFieldList.GetFields(typlen - sizeof(ushort), value->data, null);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfFieldList16t(lfFieldList_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
