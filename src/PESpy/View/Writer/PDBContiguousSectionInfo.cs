namespace PESpy.View
{
    internal struct PDBContiguousSectionInfo
    {
        public string Name;

        public int GlobalStartIndex;
        public int GlobalEndIndex;

        public int LocalStartIndex;
        public int LocalEndIndex;
        public int TotalPagesInStream;

        public int NumPages => (LocalEndIndex - LocalStartIndex) + 1;

        internal PDBContiguousSectionInfo(string name, int localStartIndex, int globalStartIndex, int totalPagesInStream)
        {
            Name = name;
            LocalStartIndex = localStartIndex;
            LocalEndIndex = localStartIndex;
            GlobalStartIndex = globalStartIndex;
            GlobalEndIndex = globalStartIndex;
            TotalPagesInStream = totalPagesInStream;
        }

        public override string ToString()
        {
            using var builder = new ValueStringBuilder();

            builder.Append(GlobalStartIndex);
            builder.Append('-');
            builder.Append(GlobalEndIndex);

            builder.Append(" | ");

            builder.Append(Name);
            builder.Append(' ');

            builder.Append('(');

            if (NumPages == TotalPagesInStream)
            {
                builder.Append(TotalPagesInStream);
            }
            else
            {
                builder.Append(LocalStartIndex + 1);
                builder.Append('-');
                builder.Append(LocalEndIndex + 1);
                builder.Append('/');
                builder.Append(TotalPagesInStream);
            }

            builder.Append(')');

            return builder.ToString();
        }
    }
}
