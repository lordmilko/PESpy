using System.Collections.Generic;

namespace PESpy.View
{
    internal class RegionBuilder
    {
        public string Name;
        public ViewKind Kind;
        public int Start;
        public int End;
        public int Depth;
        public bool IsGlobal;
        public int NestedFileDepth;

        public List<RegionBuilder> Children;

        public int Length => End - Start;

        public override string ToString()
        {
            return Name;
        }
    }
}
