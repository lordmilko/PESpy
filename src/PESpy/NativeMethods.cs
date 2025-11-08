using System;
using System.Runtime.InteropServices;

namespace PESpy
{
    internal struct MODULEINFO
    {
        public IntPtr lpBaseOfDll;
        public int SizeOfImage;
        public IntPtr EntryPoint;
    }

    internal class NativeMethods
    {
        private const string kernel32 = "kernel32.dll";

        [DllImport(kernel32)]
        internal static extern int GetProcessId(IntPtr Process);

        [DllImport(kernel32)]
        internal static extern int GetCurrentProcessId();

        [DllImport(kernel32, EntryPoint = "K32GetModuleInformation")]
        internal static unsafe extern int GetModuleInformation(
            IntPtr hProcess,
            IntPtr hModule,
            MODULEINFO* lpmodinfo,
            int cb);
    }
}
