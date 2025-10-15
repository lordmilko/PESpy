using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethodList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethodList16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int mListOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethodList_16t* value;

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

        internal LfMethodList16t(lfMethodList_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMethodList_16t, this, ViewKind.LfMethodList16t, typlen + sizeof(short));

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, leaf, sizeof(ushort));
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
