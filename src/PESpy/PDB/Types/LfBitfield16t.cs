using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBitfield_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBitfield16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int lengthOffset = 4;
        private const int positionOffset = 5;
        private const int typeOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBitfield_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public byte length => value->length;

        public byte position => value->position;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(byte)   + //length
            sizeof(byte)   + //position
            sizeof(short);   //type

        internal LfBitfield16t(lfBitfield_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfBitfield_16t, this, ViewKind.LfBitfield16t, typlen + sizeof(short));

        int IViewable.NumChildren => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(length), lengthOffset, length);
                    break;

                case 2:
                    structWriter.WriteField(nameof(position), positionOffset, position);
                    break;

                case 3:
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
