using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfSkip"/> structure.
    /// </summary>
    public readonly unsafe struct LfSkip : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfSkip* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //type

        internal LfSkip(lfSkip* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfSkip, this, ViewKind.LfSkip, typlen + sizeof(short));

        int IViewable.NumChildren => 2;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
