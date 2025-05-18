using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PESpy.PDB
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DbiHdrVersion
    {
        [FieldOffset(0)]
        public New vernew;

        [FieldOffset(0)]
        public ReallyScrewedUp verreallyscrewedup;

        [FieldOffset(0)]
        public Old verold;

        public struct New
        {
            public byte usVerPdbDllMin => (byte) (value & 0xFF);

            public byte usVerPdbDllMaj => (byte) ((value >> 8) & 0x7F);

            public bool fNewVerFmt => (value & 0x8000) != 0;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private ushort value;

            public static implicit operator New(ushort value) => new New {value = value};
        }

        public struct ReallyScrewedUp
        {
            public bool fNewVerFmt => (value & 0x0001) != 0;

            public byte usVerPdbDllMaj => (byte) ((value >> 1) & 0x7F);

            public byte usVerPdbDllMin => (byte) ((value >> 8) & 0xFF);

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#pragma warning disable CS0649
            private ushort value;
#pragma warning restore CS0649
        }

        public struct Old
        {
            public byte usVerPdbDllRBld => (byte) (value & 0x000F);

            public byte usVerPdbDllMin => (byte) ((value >> 4) & 0x007F);

            public byte usVerPdbDllMaj => (byte) ((value >> 11) & 0x001F);

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#pragma warning disable CS0649
            private ushort value;
#pragma warning restore CS0649
        }

        public static implicit operator DbiHdrVersion(ushort value) => new DbiHdrVersion {vernew = value};
        public static unsafe implicit operator ushort(DbiHdrVersion value) => *(ushort*) (&value);

        public override string ToString()
        {
            if (vernew.fNewVerFmt)
                return $"{vernew.usVerPdbDllMaj}.{vernew.usVerPdbDllMin}";

            return $"{verold.usVerPdbDllMaj}.{verold.usVerPdbDllMin}.{verold.usVerPdbDllRBld}";
        }
    }
}
