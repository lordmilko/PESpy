using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    public interface IAsmView : IView
    {
        byte Bitness { get; }

        int Count { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.Asm(this),nq}")]
    public class AsmView<T> : IAsmView
    {
        public RawOffset Offset { get; }
        public int Size { get; }
        public ViewKind Kind { get; }

        public byte Bitness { get; }

        public int Count => Instructions.Length;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Instructions { get; }

        public AsmView(RawOffset offset, int size, byte bitness, T[] instructions, ViewKind? kind)
        {
            Offset = offset;
            Size = size;
            Kind = kind ?? ViewKind.Assembly;
            Bitness = bitness;
            Instructions = instructions;
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitAsm(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitAsm(this);
    }
}
