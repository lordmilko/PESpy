using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCobol0"/> structure.
    /// </summary>
    public readonly unsafe struct LfCobol0 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCobol0* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //type

        internal LfCobol0(lfCobol0* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfCobol0, this, ViewKind.LfCobol0, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
