using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArgList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfArgList16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;
        private const int argOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArgList_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType[] arg
        {
            get
            {
                var raw = new Span<CV_typ16_t>(value->arg, count);
                var arr = new TypOrEnumType[raw.Length];

                for (var i = 0; i < arr.Length; i++)
                    arr[i] = new TypOrEnumType((byte*) value, raw[i]);

                return arr;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        private int BytesUsed => FixedStructSize + (count * sizeof(short));

        internal LfArgList16t(lfArgList_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfArgList_16t, this, ViewKind.LfArgList16t, typlen + sizeof(short));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                case 2:
                    var arg = new NativeSpan<CV_typ16_t>(value->arg, count);
                    structWriter.WriteField(nameof(arg), argOffset, arg);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
