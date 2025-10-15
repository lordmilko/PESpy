using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimCon"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimCon : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimCon* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType typ => new TypOrEnumType((byte*) value, value->typ);

        public short rank => value->rank;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //typ
            sizeof(short);   //rank

        internal LfDimCon(lfDimCon* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfDimCon, this, ViewKind.LfDimCon, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(typ), typ);
            s.WriteField(nameof(rank), rank);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
