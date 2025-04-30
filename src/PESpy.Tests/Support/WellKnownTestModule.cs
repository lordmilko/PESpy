using System.IO;
using System.Threading;
using PESpy.Tests.SymStore;

namespace PESpy.Tests
{
    public static class WellKnownTestModule
    {
        public static SymbolStoreKey AuthExt = new SymbolStoreKey("authext.dll/350DD84419000/authext.dll", "C:\\Windows\\System32\\AuthExt.dll");
        public static SymbolStoreKey AzureAttest = new SymbolStoreKey("azureattest.dll/6250867848000/azureattest.dll", "C:\\Windows\\System32\\AzureAttest.dll");
        public static SymbolStoreKey cdd = new SymbolStoreKey("cdd.dll/76CEFE3B47000/cdd.dll", "C:\\Windows\\System32\\cdd.dll");
        public static SymbolStoreKey coreclr = new SymbolStoreKey("coreclr.dll/674F3D104e7000/coreclr.dll", "C:\\symbols\\coreclr.dll\\674F3D104e7000\\coreclr.dll");
        public static SymbolStoreKey crtdll = new SymbolStoreKey("crtdll.dll/30C91E2D27000/crtdll.dll", "C:\\Windows\\SysWOW64\\crtdll.dll");
        public static SymbolStoreKey ctl3d32 = new SymbolStoreKey("ctl3d32.dll/3005CC7211000/ctl3d32.dll", "C:\\Windows\\SysWow64\\ctl3d32.dll");
        public static SymbolStoreKey d3dx9_24 = new SymbolStoreKey("d3dx9_24.dll/4205627437c000/d3dx9_24.dll", "C:\\Windows\\System32\\d3dx9_24.dll");
        public static SymbolStoreKey kdstub = new SymbolStoreKey("kdstub.dll/0EA20E6D13000/kdstub.dll", "C:\\Windows\\Boot\\EFI\\kdstub.dll");
        public static SymbolStoreKey kd_02_15b3 = new SymbolStoreKey("kd_02_15b3.dll/972BFA0319000/kd_02_15b3.dll", "C:\\Windows\\Boot\\EFI\\kd_02_15b3.dll");
        public static SymbolStoreKey notepad = new SymbolStoreKey("notepad.exe/A8673AF85a000/notepad.exe", "C:\\Windows\\system32\\notepad.exe");
        public static SymbolStoreKey Ntdll = new SymbolStoreKey("ntdll.dll/BCED4B82217000/ntdll.dll", "C:\\windows\\system32\\ntdll.dll");
        public static SymbolStoreKey ntoskrnl = new SymbolStoreKey("ntoskrnl.exe/2C33C5081047000/ntoskrnl.exe", "C:\\windows\\system32\\ntoskrnl.exe");
        public static SymbolStoreKey mfc40 = new SymbolStoreKey("mfc40.dll/31E55C32e7000/mfc40.dll", "C:\\Windows\\SysWOW64\\mfc40.dll"); //The 64-bit one also has ImageDebugMisc
        public static SymbolStoreKey mfc40u = new SymbolStoreKey("mfc40u.dll/3B7DFE9Ae9000/mfc40u.dll", "C:\\Windows\\System32\\mfc40u.dll");
        public static SymbolStoreKey mscorlib = new SymbolStoreKey("mscorlib.dll/66D13820576000/mscorlib.dll", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\mscorlib.dll");
        public static SymbolStoreKey ShoulderTapView = new SymbolStoreKey("shouldertapview.dll/1456661987000/shouldertapview.dll", "C:\\Windows\\ShellExperiences\\ShoulderTapView.dll");

        public static string CreateKey(string modulePath)
        {
            var symbolClient = new SymbolClient(new NullSymStoreLogger());

            var key = symbolClient.GetKey(modulePath);

            return $"public static {nameof(SymbolStoreKey)} {Path.GetFileNameWithoutExtension(key.FullPathName)} = new {nameof(SymbolStoreKey)}(\"{key.Index}\", \"{key.FullPathName.Replace("\\", "\\\\")}\");";
        }

        public static string GetStoreFile(SymbolStoreKey key)
        {
            var symbolClient = new SymbolClient(new NullSymStoreLogger());

            var path = symbolClient.GetStoreFile(key, CancellationToken.None);

            return path;
        }

        public static string GetStoreFile(SymStoreKey key)
        {
            var symbolClient = new SymbolClient(new NullSymStoreLogger());

            var path = symbolClient.GetStoreFile(new SymbolStoreKey(key.Value, Path.GetFileName(key.Value)), CancellationToken.None);

            return path;
        }
    }
}
