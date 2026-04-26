using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCobol0_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfCobol016t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCobol0_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //type

        internal LfCobol016t(lfCobol0_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        public static implicit operator LfEasy(LfCobol016t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfCobol016t, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
