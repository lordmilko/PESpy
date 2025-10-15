using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOEM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfOEM16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOEM_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short cvOEM => value->cvOEM;

        public short recOEM => value->recOEM;

        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //cvOEM
            sizeof(short)  + //recOEM
            sizeof(short);   //count

        internal LfOEM16t(lfOEM_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read index");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfOEM_16t, this, ViewKind.LfOEM16t, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(cvOEM), cvOEM);
            s.WriteField(nameof(recOEM), recOEM);
            s.WriteField(nameof(count), count);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
