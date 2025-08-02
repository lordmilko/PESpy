using System.Diagnostics;

namespace PESpy.View.Builder
{
    [DebuggerDisplay("Name = {Name}, Start = 0x{Start.ToString(\"X\"),nq}, End = 0x{End.ToString(\"X\"),nq}")]
    struct DirectoryInfo
    {
        public string Name { get; }
        public int Start { get; }
        public int End { get; }

        public DirectoryInfo(string name, int start, int size)
        {
            Name = name;
            Start = start;
            End = start + size;
        }
    }
}
