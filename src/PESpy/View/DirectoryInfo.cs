using System.Diagnostics;

namespace PESpy.View
{
    [DebuggerDisplay("Name = {Name}, Start = 0x{Start.ToString(\"X\"),nq}, End = 0x{End.ToString(\"X\"),nq}")]
    struct DirectoryInfo
    {
        private string name;

        public string Name => name ?? NameInfo.ToString();
        public FullNameInfo NameInfo { get; }
        public long Start { get; }
        public long End;

        public int Length => (int) (End - Start);

        public DirectoryInfo(string name, long start, int size)
        {
            this.name = name;
            Start = start;
            End = start + size;
        }

        public DirectoryInfo(FullNameInfo nameInfo, long start, int size)
        {
            NameInfo = nameInfo;
            Start = start;
            End = start + size;
        }
    }
}
