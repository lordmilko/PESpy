using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct LfHLSL : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfHLSL* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType subtype => new TypOrEnumType((byte*) value, value->subtype);

        public short kind => value->kind;

        public short numprops => value->numprops;

        public short unused => value->unused;

        public short propdata => value->propdata;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //subtype
            sizeof(short)  + //kind
            sizeof(short);   //propdata

        internal LfHLSL(lfHLSL* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfHLSL, this, ViewKind.LfHLSL, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(subtype), subtype);
            s.WriteField(nameof(kind), kind);
            s.WriteField(nameof(numprops), numprops);
            s.WriteField(nameof(unused), unused);
            s.WriteField(nameof(propdata), propdata);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
