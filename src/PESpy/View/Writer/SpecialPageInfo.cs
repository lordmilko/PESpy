namespace PESpy.View
{
    internal struct SpecialPageInfo
    {
        public string Name;
        public int SpecialIndex;
        public int LocalIndex;
        public int TotalPagesInStream;
        public string SpecialStatus;

        internal SpecialPageInfo(string name, int specialIndex, int localIndex, int totalPagesInStream, string specialStatus = null)
        {
            Name = name;
            SpecialIndex = specialIndex;
            LocalIndex = localIndex;
            TotalPagesInStream = totalPagesInStream;
            SpecialStatus = specialStatus;
        }

        public void ToString(ref ValueStringBuilder builder)
        {
            builder.Append(Name);
            builder.Append(" (");

            builder.Append(LocalIndex + 1);
            builder.Append('/');
            builder.Append(TotalPagesInStream);

            if (SpecialStatus != null)
            {
                builder.Append(") (");
                builder.Append(SpecialStatus);
            }

            builder.Append(")");
        }

        public override string ToString()
        {
            var builder = new ValueStringBuilder();

            try
            {
                ToString(ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
