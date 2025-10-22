using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfModifier_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfModifier16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int attrOffset = 4;
        private const int typeOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfModifier_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_modifier_t attr => value->attr;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(short);   //type

        internal LfModifier16t(lfModifier_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfModifier_16t, this, ViewKind.LfModifier16t, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
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
