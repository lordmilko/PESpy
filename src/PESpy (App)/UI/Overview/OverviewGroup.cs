namespace PESpy.Overview
{
    struct OverviewGroup
    {
        public string Name { get; }

        public OverviewRow[] Rows { get; }

        public OverviewGroup(string name, OverviewRow[] rows)
        {
            Name = name;
            Rows = rows;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
