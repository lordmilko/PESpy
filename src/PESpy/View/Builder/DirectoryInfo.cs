using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View.Builder
{
    [DebuggerDisplay("Name = {Name}, Start = 0x{Start.ToString(\"X\"),nq}, End = 0x{End.ToString(\"X\"),nq}")]
    struct DirectoryInfo
    {
        public string Name { get; }
        public RawOffset Start { get; }
        public RawOffset End { get; }

        public DirectoryInfo(string name, RawOffset start, int size)
        {
            Name = name;
            Start = start;
            End = start + size;
        }
    }
}