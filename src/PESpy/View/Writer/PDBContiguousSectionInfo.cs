namespace PESpy.View
{
    internal struct PDBContiguousSectionInfo
    {
        public FullNameInfo NameInfo;
        public int StreamIndex;

        public int GlobalStartIndex;
        public int GlobalEndIndex;

        public int LocalStartIndex;
        public int LocalEndIndex;
        public int TotalPagesInStream;

        public int NumPages => (LocalEndIndex - LocalStartIndex) + 1;

        internal PDBContiguousSectionInfo(FullNameInfo nameInfo, int streamIndex, int localStartIndex, int globalStartIndex, int totalPagesInStream)
        {
            NameInfo = nameInfo;
            StreamIndex = streamIndex;
            LocalStartIndex = localStartIndex;
            LocalEndIndex = localStartIndex;
            GlobalStartIndex = globalStartIndex;
            GlobalEndIndex = globalStartIndex;
            TotalPagesInStream = totalPagesInStream;
        }

        public override string ToString()
        {
            var builder = new ValueStringBuilder();

            try
            {
                builder.Append(GlobalStartIndex);
                builder.Append('-');
                builder.Append(GlobalEndIndex);

                builder.Append(" | ");

                NameInfo.ToString(ref builder, false);
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
            finally
            {
                builder.Dispose();
            }
        }
    }
}
