using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBClass_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBClass16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBClass_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public CV_fldattr_t attr => value->attr;

        public ulong offset
        {
            get
            {
                TypType.ExtractNumericData(value->offset, out var offset, out _);

                return offset;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //index
            2;               //attr

        internal LfBClass16t(lfBClass_16t* value)
        {
            this.value = value;
        }

        internal int StructSize
        {
            get
            {
                TypType.ExtractNumericData(value->offset, out _, out var bytesRead);

                return FixedStructSize + bytesRead;
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfBClass_16t, this, ViewKind.LfBClass16t, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(index), index);
            s.WriteField(nameof(attr), attr);
            s.WriteNumericData(nameof(offset), value->offset);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return index.ToString();
        }
    }
}
