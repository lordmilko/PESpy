using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx64"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx64 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCmplx64* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public double val_real => value->val_real;

        public double val_imag => value->val_imag;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(double) + //val_real
            sizeof(double);  //val_imag

        internal LfCmplx64(lfCmplx64* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfCmplx64, this, ViewKind.LfCmplx64, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(val_real), val_real);
            s.WriteField(nameof(val_imag), val_imag);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
