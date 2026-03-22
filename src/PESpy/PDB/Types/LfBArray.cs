using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfBArray : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int utypeOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->utype);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //utype

        internal LfBArray(lfBArray* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfBArray easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfBArray, this, ViewKind.LfBArray, typlen + sizeof(short));

        int IViewable.NumChildren() => 3;

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
                    structWriter.WriteField(nameof(utype), utypeOffset, value->utype);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
