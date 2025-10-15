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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfUdtModSrcLine, this, ViewKind.LfUdtModSrcLine, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(type), type);
            s.WriteField(nameof(src), src);
            s.WriteField(nameof(line), line);
            s.WriteField(nameof(imod), imod);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
