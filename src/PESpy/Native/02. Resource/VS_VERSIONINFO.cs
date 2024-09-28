using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //VsVersionInfo
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct VS_VERSIONINFO
    {
        public short wLength;
        public short wValueLength;
        public short wType;
        public fixed ushort szKey[16]; //"VS_VERSION_INFO" + \0
        public short Wrte;
        public VS_FIXEDFILEINFO Value;
        public short Padding2;
        public short Children;
    }
}
