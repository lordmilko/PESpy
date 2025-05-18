using System.Collections.Generic;

namespace PESpy.PDB
{
    public struct StreamInfoBuilder
    {
        public SN sn { get; init; }

        public int ByteCount { get; set; }

        public List<PN> PageList { get; init; }
    }
}
