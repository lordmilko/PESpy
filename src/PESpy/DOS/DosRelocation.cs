using System.Diagnostics;

namespace PESpy
{
    //Name is made up. Not sure if there's a proper name
    [DebuggerDisplay("Offset = {Offset}, Segment = {Segment}")]
    public readonly struct DosRelocation
    {
        //http://www.textfiles.com/programming/FORMATS/exefs.pro

        public ushort Offset { get; }

        public ushort Segment { get; }

        internal const int StructSize =
            sizeof(short) +
            sizeof(short);
    }
}
