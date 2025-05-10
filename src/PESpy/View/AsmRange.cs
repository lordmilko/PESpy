using System.Diagnostics;

namespace PESpy.View
{
    [DebuggerDisplay("0x{StartOffset.ToString(\"X\"),nq} - 0x{EndOffset.ToString(\"X\"),nq}")]
    public struct AsmRange<T>
    {
        public bool HasInstructions => instructionCount != 0;

        public readonly int StartRVA;
        public int EndRVA => StartRVA + Length;

        public readonly int StartOffset;
        public int EndOffset;

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

        private readonly IViewDisassembler viewDisassembler;
        internal int instructionCount;

        public AsmRange(int startOffset, int startRVA, IViewDisassembler viewDisassembler)
        {
            StartOffset = startOffset;
            StartRVA = startRVA;
            EndOffset = startOffset;
            this.viewDisassembler = viewDisassembler;
            instructionCount = 0;
        }

        public void AddInstruction(int length)
        {
            EndOffset += length;
            instructionCount++;
        }
    }
}
