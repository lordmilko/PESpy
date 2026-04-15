using System;
using System.Runtime.InteropServices;
using ClrDebug;

namespace PESpy
{
    internal unsafe class NativeMethods
    {
        private const string kernel32 = "kernel32.dll";
        private const string ntdll = "ntdll.dll";

        #region kernel32

        [DllImport(kernel32, ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern int CloseHandle(IntPtr hObject);

        [DllImport(kernel32, ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern int DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            IntPtr* lpTargetHandle,
            int dwDesiredAccess,
            int bInheritHandle,
            int dwOptions);

        //In PSAPI_VERSION 2 this function is exported from Kernel32 with a different name
        [DllImport(kernel32, EntryPoint = "K32EnumProcessModulesEx", SetLastError = true)]
        public static extern int EnumProcessModulesEx(
            [In] IntPtr hProcess,
            [Out] IntPtr* lphModule,
            [In] int cb,
            [Out] int* lpcbNeeded,
            [In] LIST_MODULES dwFilterFlag);

        [DllImport(kernel32)]
        internal static extern int GetProcessId(IntPtr Process);

        [DllImport(kernel32)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport(kernel32)]
        internal static extern int GetCurrentProcessId();

        [DllImport(kernel32, EntryPoint = "K32GetModuleInformation")]
        internal static extern int GetModuleInformation(
            IntPtr hProcess,
            IntPtr hModule,
            MODULEINFO* lpmodinfo,
            int cb);

        [DllImport(kernel32, SetLastError = true)]
        internal static extern int IsWow64Process(
            IntPtr hProcess,
            int* Wow64Process);

        [DllImport(kernel32, SetLastError = true)]
        public static extern IntPtr OpenProcess(
            int dwDesiredAccess,
            int bInheritHandle,
            int dwProcessId);

        [DllImport(kernel32, SetLastError = true)]
        internal static extern int ReadProcessMemory(
            [In] IntPtr hProcess,
            [In] IntPtr lpBaseAddress,
            [Out] IntPtr lpBuffer,
            [In] IntPtr dwSize,
            [Out] IntPtr* lpNumberOfBytesRead);

        #endregion
        #region ntdll

        [DllImport(ntdll)]
        internal static extern RTL_DEBUG_INFORMATION* RtlCreateQueryDebugBuffer(
            [In] int MaximumCommit,
            [In] byte UseEventPair);

        [DllImport(ntdll)]
        internal static extern int RtlDestroyQueryDebugBuffer(
            [In] RTL_DEBUG_INFORMATION* Buffer);

        [DllImport(ntdll)]
        internal static extern int RtlQueryProcessDebugInformation(
            [In] IntPtr UniqueProcessId,
            [In] RTL_QUERY_PROCESS Flags,
            [Out] RTL_DEBUG_INFORMATION* Buffer);

        #endregion
        #region Helpers

        public static IntPtr[] EnumProcessModulesEx(IntPtr hProcess, LIST_MODULES dwFilterFlag)
        {
            TryEnumProcessModulesEx(hProcess, dwFilterFlag, out var modules).ThrowOnNotOK();
            return modules;
        }

        public static HRESULT TryEnumProcessModulesEx(IntPtr hProcess, LIST_MODULES dwFilterFlag, out IntPtr[] modules)
        {
            int lpcbNeeded;
            var result = EnumProcessModulesEx(hProcess, default, 0, &lpcbNeeded, dwFilterFlag) != 0;

            if (result)
            {
                if (lpcbNeeded == 0)
                {
                    modules = null;
                    return HRESULT.S_FALSE;
                }

                var buffer = Marshal.AllocHGlobal(lpcbNeeded);

                try
                {
                    result = EnumProcessModulesEx(hProcess, (IntPtr*) buffer, lpcbNeeded, &lpcbNeeded, dwFilterFlag) != 0;

                    if (result)
                    {
                        var length = lpcbNeeded / IntPtr.Size;

#if NET9_0_OR_GREATER
                        var span = new Span<IntPtr>((IntPtr*) buffer, length);

                        modules = span.ToArray();
#else
                        var results = new IntPtr[length];

                        for (var i = 0; i < length; i++)
                            results[i] = *(IntPtr*) (buffer + i * IntPtr.Size);

                        modules = results;
#endif
                        return HRESULT.S_OK;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            modules = null;
            return (HRESULT) Marshal.GetHRForLastWin32Error();
        }

        internal static bool IsWow64ProcessOrDefault(IntPtr hProcess)
        {
            int isWow64;
            if (IsWow64Process(hProcess, &isWow64) == 0)
                return false;

            return isWow64 != 0;
        }

        internal static RTL_DEBUG_INFORMATION* RtlCreateQueryDebugBuffer()
        {
            var result = RtlCreateQueryDebugBuffer(0, 0);

            if ((IntPtr) result == IntPtr.Zero)
                throw new InvalidOperationException($"{nameof(RtlCreateQueryDebugBuffer)} failed.");

            return result;
        }

        #endregion
        #region Types

        //These are inside NativeMethods because we don't want libraries that PESpy shows its internals to
        //to get upset that there's a conflict between our types and the types defined in PInvoke

        public enum LIST_MODULES
        {
            LIST_MODULES_DEFAULT = 0,
            LIST_MODULES_32BIT = 1,
            LIST_MODULES_64BIT = 2,
            LIST_MODULES_ALL = 3
        }

        internal struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public int SizeOfImage;
            public IntPtr EntryPoint;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_DEBUG_INFORMATION
        {
            public IntPtr SectionHandleClient;
            public IntPtr ViewBaseClient;
            public IntPtr ViewBaseTarget;
            public IntPtr ViewBaseDelta;
            public IntPtr EventPairClient;
            public IntPtr EventPairTarget;
            public IntPtr TargetProcessId;
            public IntPtr TargetThreadHandle;
            public int Flags;
            public IntPtr OffsetFree;
            public IntPtr CommitSize;
            public IntPtr ViewSize;
            public RTL_PROCESS_MODULES* Modules;
            public IntPtr BackTraces; //RTL_PROCESS_BACKTRACES*
            public IntPtr Heaps; //RTL_PROCESS_HEAPS*
            public IntPtr Locks; //RTL_PROCESS_LOCKS*
            public IntPtr SpecificHeap;
            public IntPtr TargetProcessHandle;

            public IntPtr Reserved1;
            public IntPtr Reserved2;
            public IntPtr Reserved3;
            public IntPtr Reserved4;
            public IntPtr Reserved5;
            public IntPtr Reserved6;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_PROCESS_MODULES
        {
            public int NumberOfModules;
            public IntPtr Modules; //This is a fixed array of RTL_PROCESS_MODULE_INFORMATION. Access a module by doing ((RTL_PROCESS_MODULE_INFORMATION*) &Modules)[i]
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_PROCESS_MODULE_INFORMATION
        {
            public IntPtr Section;
            public IntPtr MappedBase;
            public IntPtr ImageBase;
            public int ImageSize;
            public int Flags;
            public short LoadOrderIndex;
            public short InitOrderIndex;
            public short LoadCount;
            public short OffsetToFileName;
            public fixed byte FullPathName[256];
        }

        [Flags]
        internal enum RTL_QUERY_PROCESS : uint
        {
            MODULES = 0x00000001,
            BACKTRACES = 0x00000002,
            HEAP_SUMMARY = 0x00000004,
            HEAP_TAGS = 0x00000008,
            HEAP_ENTRIES = 0x00000010,
            LOCKS = 0x00000020,
            MODULES32 = 0x00000040,
            NONINVASIVE = 0x80000000
        }

        #endregion
    }
}
