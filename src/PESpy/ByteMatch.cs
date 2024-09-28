using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("Position = {Position.ToString(\"X\")}, Sequence = {(Sequence?.ToString() ?? \"null\"),nq}")]
    readonly struct ByteMatch
    {
        public int Position { get; }

        public ByteSequence Sequence { get; }

        public ByteMatch(int position, ByteSequence sequence)
        {
            Position = position + sequence.Mark;
            Sequence = sequence;
        }
    }
}
