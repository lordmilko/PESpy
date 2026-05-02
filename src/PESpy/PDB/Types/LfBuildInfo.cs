using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBuildInfo"/> structure.
    /// </summary>
    public readonly unsafe struct LfBuildInfo : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;
        private const int argOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBuildInfo* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumTypeList<CV_ItemId> arg => new TypOrEnumTypeList<CV_ItemId>(new NativeSpan<CV_ItemId>(value->arg, count)); //You can index into this using CV_BuildInfo_e

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        private int BytesUsed() => FixedStructSize + (count * sizeof(int));

        internal LfBuildInfo(lfBuildInfo* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfBuildInfo easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfBuildInfo, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumPaddedChildren(4, typlen, BytesUsed());

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
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                case 3:
                    var arg = new NativeSpan<CV_ItemId>(value->arg, count);
                    structWriter.WriteField(nameof(arg), argOffset, arg);
                    break;

                case 4:
                    //Possible padding
                    structWriter.PadTypOrThrow(typlen, BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
