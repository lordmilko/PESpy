using System;
using System.Diagnostics;

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
        public int Offset { get; }

        private string? name;

        public string Name
        {
            get
            {
                if (name == null)
                {
                    var diff = range.StartRVA - range.FunctionRVA;

                    if (diff == 0)
                        name = range.Name;
                    else if (diff > 0)
                        name = $"{range.Name}+0x{diff:X}";
                    else
                        name = $"{range.Name}-0x{Math.Abs(diff):X}";
                }

                return name;
            }
        }

        public int Size => range.Length;
        public ViewKind Kind { get; }

        public byte Bitness { get; }

        public int Count => Instructions.Length;

        private AsmRange<T> range;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Instructions => range.Instructions;

        public AsmView(RawOffset offset, byte bitness, in AsmRange<T> range, ViewKind kind = ViewKind.Assembly)
        public AsmView(int offset, byte bitness, in AsmRange<T> range, ViewKind kind = ViewKind.Assembly)
        {
            Offset = offset;
            Kind = kind;
            Bitness = bitness;
            this.range = range;
        }

        public TResult Accept<TResult>(ViewVisitor<TResult> visitor) => visitor.VisitAsm(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitAsm(this);
    }
}
