using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPad"/> structure.
    /// </summary>
    public readonly unsafe struct LfPad : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPad* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        //This type only occupies 1 byte. e.g. it can be pointed to by a lfFieldList.
        //I wouldn't expect it to actually have a TYPTYPE behind it
        public LEAF_ENUM_e leaf => (LEAF_ENUM_e) value->leaf;

        internal const int StructSize =
            sizeof(byte);  //leaf

        internal LfPad(lfPad* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfPad easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfPad, typlen + sizeof(short));

        int IViewable.NumChildren() => 2;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
