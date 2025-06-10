using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("off = {off}, cb = {cb}")]
    public struct OffCb
    {
        public int off;
        public int cb;
    }
}
