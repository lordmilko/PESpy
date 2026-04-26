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

        public TypOrEnumTypeList<CV_typ16_t> arg => new TypOrEnumTypeList<CV_typ16_t>(new NativeSpan<CV_typ16_t>(value->arg, count));

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        private int BytesUsed() => FixedStructSize + (count * sizeof(short));

        internal LfArgList16t(lfArgList_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfArgList16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfArgList16t, typlen + sizeof(short));

        int IViewable.NumChildren() => count == 0 ? 2 : 3;

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
                    //WriteField will throw IndexOutOfRangeException if this is empty
                    var arg = new NativeSpan<CV_typ16_t>(value->arg, count);
                    structWriter.WriteField(nameof(arg), argOffset, arg);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
