using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethodList"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethodList : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethodList* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public MlMethod[] mList
        {
            get
            {
                using var results = new PooledList<MlMethod>();

                var ptr = value->mList;

                var end = ptr + (typlen - sizeof(ushort));

                while (ptr < end)
                {
                    var item = new MlMethod((mlMethod*) ptr);
                    results.Add(item);
                    ptr += item.StructSize;
                }

                return results.ToArray();
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfMethodList(lfMethodList* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMethodList, this, ViewKind.LfMethodList, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));

            s.WriteStructField(nameof(mList), mList);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
