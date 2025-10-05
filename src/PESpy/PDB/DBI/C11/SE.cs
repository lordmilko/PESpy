using System.Diagnostics;

namespace PESpy.PDB
{
    //SE is a simple type that encapsulates a "start and end". It's used both for the section headers,
    //but also within each file
    [DebuggerDisplay("start = {start}, end = {end}")]
    public struct SE
    {
        public int start;
        public int end;

        //The Visual Studio debugger doesn't like it when I have properties that do sizeof(SE)
        internal const int StructSize =
            sizeof(int) +
            sizeof(int);
    }
}
