using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVftable"/> structure.
    /// </summary>
    public readonly unsafe struct LfVftable : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVftable* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public TypOrEnumType baseVftable => new TypOrEnumType((byte*) value, value->baseVftable);

        public int offsetInObjectLayout => value->offsetInObjectLayout;

        public int len => value->len;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(int)    + //baseVftable
            sizeof(int)    + //offsetInObjectLayout
            sizeof(int);     //len

        internal LfVftable(lfVftable* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read Names");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVftable, this, ViewKind.LfVftable, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(type), type);
            s.WriteField(nameof(baseVftable), baseVftable);
            s.WriteField(nameof(offsetInObjectLayout), offsetInObjectLayout);
            s.WriteField(nameof(len), len);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
