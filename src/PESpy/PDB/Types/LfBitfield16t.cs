using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBitfield_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBitfield16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBitfield_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public byte length => value->length;

        public byte position => value->position;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(byte)   + //length
            sizeof(byte)   + //position
            sizeof(short);   //type

        internal LfBitfield16t(lfBitfield_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfBitfield_16t, this, ViewKind.LfBitfield16t, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(length), length);
            s.WriteField(nameof(position), position);
            s.WriteField(nameof(type), type);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
