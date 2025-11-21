using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    [DebuggerDisplay("ti = {ti}, off = {off}")]
    public struct TI_OFF_16t
    {
        public CV_typ16_t ti;
        public int off;
    }
}
