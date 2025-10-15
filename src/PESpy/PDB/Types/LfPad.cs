using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPad"/> structure.
    /// </summary>
    public readonly unsafe struct LfPad : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPad* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        //This type only occupies 1 byte. e.g. it can be pointed to by a lfFieldList.
        //I wouldn't expect it to actually have a TYPTYPE behind it
        public LEAF_ENUM_e leaf => (LEAF_ENUM_e) value->leaf;

        internal const int StructSize =
            sizeof(byte);  //leaf

        internal LfPad(lfPad* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfPad, this, ViewKind.LfPad, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
