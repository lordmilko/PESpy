using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //VsFixedFileInfo
    [StructLayout(LayoutKind.Sequential)]
    internal struct VS_FIXEDFILEINFO
    {
        public int dwSignature;
        public int dwStrucVersion;
        public int dwFileVersionMS;
        public int dwFileVersionLS;
        public int dwProductVersionMS;
        public int dwProductVersionLS;
        public int dwFileFlagsMask;
        public VS_FF dwFileFlags;
        public VOS dwFileOS;
        public int dwFileType;
        public int dwFileSubtype;
        public int dwFileDateMS;
        public int dwFileDateLS;
    }
}
