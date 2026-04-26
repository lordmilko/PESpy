using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfList"/> structure.
    /// </summary>
    public readonly unsafe struct LfList : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfList* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfList(lfList* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        public static implicit operator LfEasy(LfList easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfList, typlen + sizeof(short));

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, typlen, sizeof(ushort));
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
