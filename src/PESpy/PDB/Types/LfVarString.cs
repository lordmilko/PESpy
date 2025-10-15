using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVarString"/> structure.
    /// </summary>
    public readonly unsafe struct LfVarString : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVarString* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short len => value->len;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //len

        internal LfVarString(lfVarString* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read value");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVarString, this, ViewKind.LfVarString, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(len), len);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
