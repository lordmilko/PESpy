using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVftable"/> structure.
    /// </summary>
    public readonly unsafe struct LfVftable : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;
        private const int baseVftableOffset = 8;
        private const int offsetInObjectLayoutOffset = 12;
        private const int lenOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVftable* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public TypOrEnumType baseVftable => new TypOrEnumType((byte*) value, value->baseVftable);

        public int offsetInObjectLayout => value->offsetInObjectLayout;

        public int len => value->len;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(int)    + //baseVftable
            sizeof(int)    + //offsetInObjectLayout
            sizeof(int);     //len

        internal LfVftable(lfVftable* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read Names");
        }

        public static implicit operator LfEasy(LfVftable easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVftable, this, ViewKind.LfVftable, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                case 3:
                    structWriter.WriteField(nameof(baseVftable), baseVftableOffset, value->baseVftable);
                    break;

                case 4:
                    structWriter.WriteField(nameof(offsetInObjectLayout), offsetInObjectLayoutOffset, offsetInObjectLayout);
                    break;

                case 5:
                    structWriter.WriteField(nameof(len), lenOffset, len);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
