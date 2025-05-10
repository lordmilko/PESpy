using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    public interface IAsmView : IView
    {
        string Name { get; }

        byte Bitness { get; }

        int Count { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.Asm(this),nq}")]
    public class AsmView<T> : IAsmView
    {
        public RawOffset Offset { get; }
        public string Name { get; }
        public int Size => range.Length;
        public ViewKind Kind { get; }

        public byte Bitness { get; }

        public int Count => Instructions.Length;

        private AsmRange<T> range;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Instructions => range.Instructions;

        public AsmView(RawOffset offset, string name, byte bitness, in AsmRange<T> range, ViewKind kind = ViewKind.Assembly)
        {
            Offset = offset;
            Name = name;
            Kind = kind;
            Bitness = bitness;
            this.range = range;
        }

        public TResult Accept<TResult>(ViewVisitor<TResult> visitor) => visitor.VisitAsm(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitAsm(this);
    }
}
