using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUdtSrcLine"/> structure.
    /// </summary>
    public readonly unsafe struct LfUdtSrcLine : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int typeOffset = 4;
        private const int srcOffset = 8;
        private const int lineOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUdtSrcLine* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public TypOrEnumType src => new TypOrEnumType((byte*) value, value->src);

        public int line => value->line;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(int)    + //src
            sizeof(int);     //line

        internal LfUdtSrcLine(lfUdtSrcLine* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfUdtSrcLine, this, ViewKind.LfUdtSrcLine, typlen + sizeof(short));

        int IViewable.NumChildren => 5;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
