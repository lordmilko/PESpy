using System;
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
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int mListOffset = 4;

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

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, typlen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteStructField(nameof(mList), mListOffset, mList);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
