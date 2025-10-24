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
            public byte usVerPdbDllMin
            {
                get => (byte) (value & 0xFF);
                set => this.value = (ushort) ((this.value & 0xFF00) | (value & 0xFF));
            }

            public byte usVerPdbDllMaj
            {
                get => (byte) ((value >> 8) & 0x7F);
                set => this.value = (ushort) ((this.value & 0x80FF) | ((value & 0x7F) << 8));
            }

            public bool fNewVerFmt
            {
                get => (value & 0x8000) != 0;
                set
                {
                    if (value)
                        this.value |= 0x8000;
                    else
                        this.value &= 0x7FFF;
                }
            }

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
            public byte usVerPdbDllRBld
            {
                get => (byte) (value & 0x000F);
                set => this.value = (ushort) ((this.value & ~0x000F) | (value & 0x0F));
            }

            public byte usVerPdbDllMin
            {
                get => (byte) ((value >> 4) & 0x007F);
                set => this.value = (ushort) ((this.value & ~0x07F0) | ((value & 0x7F) << 4));
            }

            public byte usVerPdbDllMaj
            {
                get => (byte) ((value >> 11) & 0x001F);
                set => this.value = (ushort) ((this.value & ~0xF800) | ((value & 0x1F) << 11));
            }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#pragma warning disable CS0649
            private ushort value;
#pragma warning restore CS0649
        }

        public static DbiHdrVersion FromNew(byte usVerPdbDllMaj, byte usVerPdbDllMin)
        {
            return new DbiHdrVersion
            {
                vernew = new New
                {
                    usVerPdbDllMaj = usVerPdbDllMaj,
                    usVerPdbDllMin = usVerPdbDllMin,
                    fNewVerFmt = true
                }
            };
        }

        public static DbiHdrVersion FromOld(byte usVerPdbDllMaj, byte usVerPdbDllMin, byte usVerPdbDllRBld)
        {
            return new DbiHdrVersion
            {
                verold = new Old
                {
                    usVerPdbDllRBld = usVerPdbDllRBld,
                    usVerPdbDllMin = usVerPdbDllMin,
                    usVerPdbDllMaj = usVerPdbDllMaj
                }
            };
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
