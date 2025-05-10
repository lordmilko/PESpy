using System.Diagnostics;
using System.Text;

namespace PESpy.View
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct AsmRange<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append(StartOffset.ToString("X"));
                builder.Append(" - ");
                builder.Append(EndOffset.ToString("X"));

                if (Name != null)
                    builder.Append(" ").Append(Name);

                return builder.ToString();
            }
        }

        public bool HasInstructions => instructionCount != 0;

        public readonly int StartRVA;
        public int EndRVA => StartRVA + Length;

        public readonly int StartOffset;
        public int EndOffset;
        public string? Name;

        public int Length => EndOffset - StartOffset;

        public int NextOffset
        {
            get
            {
                var start = StartOffset;
                var end = EndOffset;

                if (start == end)
                    return start;

                return end + 1;
            }
        }

        public int NextRVA
        {
            get
            {
                var start = StartRVA;
                var end = start + Length; ;

                if (start == end)
                    return start;

                return end + 1;
            }
        }

        public T[] Instructions => viewDisassembler.Disassemble(this, instructionCount);

        private readonly ViewDisassembler<T> viewDisassembler;
        internal int instructionCount;

        public AsmRange(int startOffset, int startRVA, ViewDisassembler<T> viewDisassembler, string? name)
        {
            StartOffset = startOffset;
            StartRVA = startRVA;
            EndOffset = startOffset;
            this.viewDisassembler = viewDisassembler;
            instructionCount = 0;
            Name = name;
        }

        public void AddInstruction(int length)
        {
            EndOffset += length;
            instructionCount++;
        }
    }
}
