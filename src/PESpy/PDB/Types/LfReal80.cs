using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal80"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal80 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal80* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public NativeSpan<byte> val => new NativeSpan<byte>(value->val, 10);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfReal80(lfReal80* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfReal80, this, ViewKind.LfReal80, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(val), val);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
