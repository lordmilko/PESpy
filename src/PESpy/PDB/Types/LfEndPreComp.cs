using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEndPreComp"/> structure.
    /// </summary>
    public readonly unsafe struct LfEndPreComp : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEndPreComp* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int signature => value->signature;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //signature

        internal LfEndPreComp(lfEndPreComp* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfEndPreComp, this, ViewKind.LfEndPreComp, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(signature), signature);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
