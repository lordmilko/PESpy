using System.Diagnostics;

namespace PESpy.PDB
{
    //There can be a lot of these, so we access them via a span instead of a chunk
    [DebuggerDisplay("off = {off}, cRef = {cRef}")]
    public struct HRFile
    {
        public int off;
        public int cRef;

        internal const int StructSize =
            sizeof(int) + //off
            sizeof(int);  //cRef
    }
}
