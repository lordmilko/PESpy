using PESpy.PDB;

namespace PESpy.View
{
    internal struct PageInfo
    {
        public SI si;
        public int siIndex;
        public int pageIndex;
        public string name;

        public PageInfo(SI si, int siIndex, int pageIndex, string name)
        {
            this.si = si;
            this.siIndex = siIndex;
            this.pageIndex = pageIndex;
            this.name = name;
        }
    }
}
