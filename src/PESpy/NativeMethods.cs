using System;
using System.Runtime.InteropServices;
using ClrDebug;

namespace PESpy
{
    internal unsafe class NativeMethods
    {
        private const string kernel32 = "kernel32.dll";
        private const string ntdll = "ntdll.dll";
        private const string winhttp = "winhttp.dll";

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
        #region WinHttp

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpAddRequestHeaders(
            void* hRequest,
            IntPtr lpszHeaders,
            int dwHeadersLength,
            int dwModifiers);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpCloseHandle(
            void* hInternet);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe void* WinHttpConnect(
            void* hSession,
            IntPtr pswzServerName,
            short nServerPort,
            int dwReserved);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpCrackUrl(
            IntPtr pwszUrl,
            int dwUrlLength,
            int dwFlags,
            URL_COMPONENTS* lpUrlComponents);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpReadData(
            void* hRequest,
            void* lpBuffer,
            int dwNumberOfBytesToRead,
            int* lpdwNumberOfBytesRead);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe void* WinHttpOpen(
            IntPtr pszAgentW,
            WINHTTP_ACCESS_TYPE dwAccessType,
            IntPtr pszProxyW,
            IntPtr pszProxyBypassW,
            int dwFlags);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe void* WinHttpOpenRequest(
            void* hConnect,
            IntPtr pwszVerb,
            IntPtr pwszObjectName,
            IntPtr pwszVersion,
            IntPtr pwszReferrer,
            IntPtr* ppwszAcceptTypes,
            WINHTTP_OPEN_REQUEST_FLAGS dwFlags);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpQueryDataAvailable(
            void* hRequest,
            int* lpdwNumberOfBytesAvailable);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpQueryHeaders(
            void* hRequest,
            int dwInfoLevel,
            IntPtr pwszName,
            [Optional] void* lpBuffer,
            int* lpdwBufferLength,
            int* lpdwIndex);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpReceiveResponse(
            void* hRequest,
            void* lpReserved);

        [DllImport(winhttp, SetLastError = true)]
        public static extern unsafe int WinHttpSendRequest(
            void* hRequest,
            IntPtr lpszHeaders,
            int dwHeadersLength,
            [Optional] void* lpOptional,
            int dwOptionalLength,
            int dwTotalLength,
            nuint dwContext);

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

        public struct URL_COMPONENTS
        {
            public int dwStructSize;
            public IntPtr lpszScheme;
            public int dwSchemeLength;
            public WINHTTP_INTERNET_SCHEME nScheme;
            public IntPtr lpszHostName;
            public int dwHostNameLength;
            public short nPort;
            public IntPtr lpszUserName;
            public int dwUserNameLength;
            public IntPtr lpszPassword;
            public int dwPasswordLength;
            public IntPtr lpszUrlPath;
            public int dwUrlPathLength;
            public IntPtr lpszExtraInfo;
            public int dwExtraInfoLength;
        }

        public enum WINHTTP_ACCESS_TYPE : uint
        {
            WINHTTP_ACCESS_TYPE_NO_PROXY = 1U,
            WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0U,
            WINHTTP_ACCESS_TYPE_NAMED_PROXY = 3U,
            WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY = 4U,
        }

        public enum WINHTTP_INTERNET_SCHEME
        {
            WINHTTP_INTERNET_SCHEME_HTTP = 1,
            WINHTTP_INTERNET_SCHEME_HTTPS = 2,
            WINHTTP_INTERNET_SCHEME_FTP = 3,
            WINHTTP_INTERNET_SCHEME_SOCKS = 4,
        }

        [Flags]
        public enum WINHTTP_OPEN_REQUEST_FLAGS : uint
        {
            WINHTTP_FLAG_BYPASS_PROXY_CACHE = 0x00000100,
            WINHTTP_FLAG_ESCAPE_DISABLE = 0x00000040,
            WINHTTP_FLAG_ESCAPE_DISABLE_QUERY = 0x00000080,
            WINHTTP_FLAG_ESCAPE_PERCENT = 0x00000004,
            WINHTTP_FLAG_NULL_CODEPAGE = 0x00000008,
            WINHTTP_FLAG_REFRESH = 0x00000100,
            WINHTTP_FLAG_SECURE = 0x00800000,
        }

        #endregion
    }
}
