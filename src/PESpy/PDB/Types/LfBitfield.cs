using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBitfield"/> structure.
    /// </summary>
    public readonly unsafe struct LfBitfield : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;
        private const int lengthOffset = 8;
        private const int positionOffset = 9;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBitfield* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public byte length => value->length;

        public byte position => value->position;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(byte)   + //length
            sizeof(byte);    //position

        internal LfBitfield(lfBitfield* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfBitfield easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfBitfield, typlen + sizeof(short));

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                case 2:
                    structWriter.WriteField(nameof(length), lengthOffset, length);
                    break;

                case 3:
                    structWriter.WriteField(nameof(position), positionOffset, position);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
