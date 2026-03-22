using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfModifier"/> structure.
    /// </summary>
    public readonly unsafe struct LfModifier : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;
        private const int attrOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfModifier* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public CV_modifier_t attr => value->attr;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            2;               //attr

        internal LfModifier(lfModifier* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfModifier easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfModifier, this, ViewKind.LfModifier, typlen + sizeof(short));

        int IViewable.NumChildren() => 4;

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
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
