using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUdtModSrcLine"/> structure.
    /// </summary>
    public readonly unsafe struct LfUdtModSrcLine : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;
        private const int srcOffset = 8;
        private const int lineOffset = 12;
        private const int imodOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUdtModSrcLine* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public TypOrEnumType src => new TypOrEnumType((byte*) value, value->src);

        public int line => value->line;

        public ushort imod => value->imod;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(int)    + //src
            sizeof(int)    + //line
            sizeof(short);   //imod

        internal LfUdtModSrcLine(lfUdtModSrcLine* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfUdtModSrcLine easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfUdtModSrcLine, this, ViewKind.LfUdtModSrcLine, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(src), srcOffset, value->src);
                    break;

                case 4:
                    structWriter.WriteField(nameof(line), lineOffset, line);
                    break;

                case 5:
                    structWriter.WriteField(nameof(imod), imodOffset, imod);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
