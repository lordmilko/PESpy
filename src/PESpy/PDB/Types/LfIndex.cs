using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfIndex"/> structure.
    /// </summary>
    public readonly unsafe struct LfIndex : IViewable
    {
        private const int leafOffset = 0;
        private const int pad0Offset = 2;
        private const int indexOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfIndex* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //pad0
            sizeof(int);     //index

        internal LfIndex(lfIndex* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfIndex easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfIndex, StructSize); //Non-primary, should not have a TYPTYPE.len

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(pad0), pad0Offset, pad0);
                    break;

                case 2:
                    structWriter.WriteField(nameof(index), indexOffset, value->index);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
