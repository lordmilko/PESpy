using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBArray_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBArray16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int utypeOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBArray_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->utype);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //utype

        internal LfBArray16t(lfBArray_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfBArray_16t, this, ViewKind.LfBArray16t, typlen + sizeof(short));

        int IViewable.NumChildren => 3;

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
