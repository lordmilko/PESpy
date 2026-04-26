using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct LfHLSL : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int subtypeOffset = 4;
        private const int kindOffset = 8;
        private const int propdataOffset = 10;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfHLSL* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType subtype => new TypOrEnumType((byte*) value, value->subtype);

        public short kind => value->kind;

        //Bitfield
        public short numprops => value->numprops;
        public short unused => value->unused;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //subtype
            sizeof(short)  + //kind
            sizeof(short);   //numprops / unused

        internal LfHLSL(lfHLSL* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        public static implicit operator LfEasy(LfHLSL easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfHLSL, typlen + sizeof(short));

        int IViewable.NumChildren() => 6;

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
                    structWriter.WriteField(nameof(subtype), subtypeOffset, value->subtype);
                    break;

                case 3:
                    structWriter.WriteField(nameof(kind), kindOffset, kind);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(numprops), propdataOffset, numprops, sizeof(short), 4);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(unused), propdataOffset, unused, sizeof(short), 12);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
