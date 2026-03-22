namespace PESpy.View
{
    struct FullNameInfo
    {
        public int GlobalPageIndex;
        public string MatchName; //match.Name
        public int StreamIndex;
        public int LocalPageIndex;
        public int TotalPagesInStream;
        public bool IsDelayedFree;
        public bool IsSpecialName;
        public string SpecialStatus;

        public void ToString(ref ValueStringBuilder builder, bool fullName)
        {
            if (fullName)
            {
                builder.Append(GlobalPageIndex);
                builder.Append(" | ");
            }

            if (IsSpecialName)
            {
                builder.Append(MatchName);
                builder.Append(" (");

                builder.Append(LocalPageIndex + 1);
                builder.Append('/');
                builder.Append(TotalPagesInStream);

                if (SpecialStatus != null)
                {
                    builder.Append(") (");
                    builder.Append(SpecialStatus);
                }

                builder.Append(")");
            }
            else
            {
                if (IsDelayedFree)
                    builder.Append("Delayed Free / ");

                if (MatchName != null)
                {
                    builder.Append('\'');
                    builder.Append(MatchName);
                    builder.Append(" (Stream");
                    builder.Append(StreamIndex);
                    builder.Append(")'");
                }
                else
                {
                    builder.Append("Stream");
                    builder.Append(StreamIndex);
                }

                if (fullName)
                {
                    builder.Append(" (Page ");
                    builder.Append(LocalPageIndex + 1);
                    builder.Append("/");
                    builder.Append(TotalPagesInStream);
                    builder.Append(")");
                }
            }
        }

        public override string ToString()
        {
            var builder = new ValueStringBuilder();

            try
            {
                ToString(ref builder, true);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
