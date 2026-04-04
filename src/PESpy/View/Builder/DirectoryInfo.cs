using System.Diagnostics;

namespace PESpy.View.Builder
{
    [DebuggerDisplay("Name = {Name}, Start = 0x{Start.ToString(\"X\"),nq}, End = 0x{End.ToString(\"X\"),nq}")]
    struct DirectoryInfo
    {
        private string name;

        public string Name => name ?? NameInfo.ToString();
        public FullNameInfo NameInfo { get; }
        public int Start { get; }
        public int End;

        public int Length => End - Start;

        public DirectoryInfo(string name, int start, int size)
        {
            this.name = name;
            Start = start;
            End = start + size;
        }

        public DirectoryInfo(FullNameInfo nameInfo, int start, int size)
        {
            NameInfo = nameInfo;
            Start = start;
            End = start + size;
        }
    }
}
