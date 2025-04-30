#if DEBUG
using System;
using System.Runtime.InteropServices;
using ClrDebug;
using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Provides fast access to the raw data contained in a PE File,
    /// without as rich an API as <see cref="PEFile"/><para/>
    /// This type is only usable on Windows.
    /// </summary>
    public unsafe class FastPEFile : IDisposable
    {
        #region Static

        public static bool TryOpen(string filePath, out FastPEFile peFile)
        {
            peFile = null;

            var hFile = CreateFileW(
                filePath,
                GENERIC_READ,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero
            );

            if (hFile == INVALID_HANDLE_VALUE)
                return false;

            var lo = GetFileSize(hFile, out var hi);

            if (lo == INVALID_FILE_SIZE)
            {
                CloseHandle(hFile);
                return false;
            }

            var hMap = CreateFileMappingA(
                hFile,
                IntPtr.Zero,
                PAGE_READONLY,
                hi,
                lo,
                null
            );

            if (hMap == IntPtr.Zero)
            {
                CloseHandle(hFile);
                return false;
            }

            var hView = MapViewOfFile(
                hMap,
                FILE_MAP_READ,
                0,
                0,
                IntPtr.Zero
            );

            if (hView == IntPtr.Zero)
            {
                CloseHandle(hMap);
                CloseHandle(hFile);
                return false;
            }

            var file = new FastPEFile(hFile, hMap, hView);

            if (!file.HasValidNtHeaders())
            {
                file.Dispose();
                return false;
            }

            peFile = file;

            return true;
        }

        #endregion

        private IntPtr hFile;
        private IntPtr hMap;
        private IntPtr hView;
        private bool disposed;

        private FastPEFile(IntPtr hFile, IntPtr hMap, IntPtr hView)
        {
            this.hFile = hFile;
            this.hMap = hMap;
            this.hView = hView;
        }

        //May either be IMAGE_NT_HEADERS or IMAGE_NT_HEADERS64
        private byte* NtHeaders =>
            (byte*) (hView + ((IMAGE_DOS_HEADER*) hView)->e_lfanew);

        private IMAGE_DATA_DIRECTORY* GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY kind)
        {
            var optionalHeader = &((IMAGE_NT_HEADERS*) NtHeaders)->OptionalHeader;

            if (optionalHeader->Magic == PEMagic.PE32Plus)
                return ((IMAGE_DATA_DIRECTORY*) ((IMAGE_OPTIONAL_HEADER64*) optionalHeader)->DataDirectory) + (int) kind;

            return ((IMAGE_DATA_DIRECTORY*) optionalHeader->DataDirectory) + (int) kind;
        }

        public void GetOptionalHeader(out long imageBase, out int sizeOfImage)
        {
            var optionalHeader = &((IMAGE_NT_HEADERS*) NtHeaders)->OptionalHeader;

            if (optionalHeader->Magic == PEMagic.PE32Plus)
            {
                var optionalHeader64 = (IMAGE_OPTIONAL_HEADER64*) optionalHeader;

                imageBase = optionalHeader64->ImageBase;
                sizeOfImage = optionalHeader64->SizeOfImage;
            }
            else
            {
                imageBase = optionalHeader->ImageBase;
                sizeOfImage = optionalHeader->SizeOfImage;
            }
        }

        private bool HasValidNtHeaders()
        {
            var dosHeader = (IMAGE_DOS_HEADER*) hView;

            if (dosHeader->e_magic != ImageDosHeader.IMAGE_DOS_SIGNATURE)
                return false;

            var ntHeaders = (IMAGE_NT_HEADERS*) (hView + dosHeader->e_lfanew);

            if (ntHeaders->Signature != ImageNtHeaders.IMAGE_NT_SIGNATURE)
                return false;

            return true;
        }

        private IMAGE_SECTION_HEADER* lastUsedSection;

        private IntPtr ResolveRva(int rva)
        {
            //Given an RVA, identify its physical offset based on the section its in, and then return a pointer to that relative
            //to our hView

            if (rva == 0)
                return IntPtr.Zero;

            var section = GetSectionContainingRva(rva);

            if (section == null)
                return IntPtr.Zero;

            var offset = section->PointerToRawData + (rva - section->VirtualAddress);

            return hView + offset;
        }

        private IMAGE_SECTION_HEADER* GetSectionContainingRva(int rva)
        {
            if (lastUsedSection != null)
            {
                if (rva >= lastUsedSection->VirtualAddress && rva <= lastUsedSection->VirtualAddress + lastUsedSection->VirtualSize)
                    return lastUsedSection;
            }

            var ntHeaders = (IMAGE_NT_HEADERS*) NtHeaders;
            var optionalHeader = &ntHeaders->OptionalHeader;
            var size = ntHeaders->FileHeader.SizeOfOptionalHeader;

            IMAGE_SECTION_HEADER* firstSection = (IMAGE_SECTION_HEADER*) (((byte*) optionalHeader) + size);
            IMAGE_SECTION_HEADER* lastSection = firstSection + ntHeaders->FileHeader.NumberOfSections; //This will be 1 section after the last one (since the first one is at 0)

            var section = firstSection;

            IMAGE_SECTION_HEADER* match = null;

            while (section < lastSection)
            {
                if (rva >= section->VirtualAddress && rva <= section->VirtualAddress + section->VirtualSize)
                {
                    lastUsedSection = section;
                    return section;
                }

                section++;
            }

            return null;
        }

        public bool TryGetExport(string name, out int rva, out int ordinalPlusBase)
        {
            rva = default;
            ordinalPlusBase = default;

            var pDirectoryEntry = GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY.EXPORT);

            if (pDirectoryEntry->VirtualAddress == 0 || pDirectoryEntry->Size == 0)
                return false;

            IMAGE_EXPORT_DIRECTORY* pExportDirectory = (IMAGE_EXPORT_DIRECTORY*) ResolveRva(pDirectoryEntry->VirtualAddress);

            if (pExportDirectory->AddressOfNames == 0 || pExportDirectory->AddressOfNameOrdinals == 0 || pExportDirectory->AddressOfFunctions == 0)
                return false;

            var numberOfNames = pExportDirectory->NumberOfNames;
            var nameTable = (int*) ResolveRva(pExportDirectory->AddressOfNames);

            for (var i = 0; i < numberOfNames; i++, nameTable++)
            {
                var nameRVA = *nameTable;

                if (nameRVA != 0)
                {
                    var pName = ResolveRva(nameRVA);

                    fixed (char* c = name)
                    {
                        if (strcmp(c, (byte*) pName) == 0)
                        {
                            //It's a match!
                            var ordinal = *(short*) ResolveRva(pExportDirectory->AddressOfNameOrdinals + (sizeof(short) * i));
                            rva = *(int*) ResolveRva(pExportDirectory->AddressOfFunctions + (sizeof(int) * ordinal));
                            ordinalPlusBase = ordinal + pExportDirectory->Base;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void EnumerateResources(ResourceType resourceType, Func<ResourceNameContext, bool> resourceName, Func<ResourceFoundContext, bool> resourceFound)
        {
            var pDataDirectory = GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY.RESOURCE);

            if (pDataDirectory->VirtualAddress == 0)
                return;

            EnumerateDirectory(
                0,
                ResolveRva(pDataDirectory->VirtualAddress),
                resourceType,
                resourceName,
                resourceFound,
                ResourceLevel.ResourceType
            );
        }

        //Returns false if we should stop enumerating
        private bool EnumerateDirectory(
            int offset,
            IntPtr rootAddress,
            ResourceType resourceType,
            Func<ResourceNameContext, bool> resourceName,
            Func<ResourceFoundContext, bool> resourceFound,
            ResourceLevel level)
        {
            var pDirectory = (IMAGE_RESOURCE_DIRECTORY*) (rootAddress + offset);
            var entries = (IMAGE_RESOURCE_DIRECTORY_ENTRY*) pDirectory->DirectoryEntries;

            var numEntries = pDirectory->NumberOfNamedEntries + pDirectory->NumberOfIdEntries;

            for (var i = 0; i < numEntries; i++)
            {
                var entry = &entries[i];

                var nameIsString = ((entry->NameOrId >> 31) & 1) == 1;
                var nameOffset = entry->NameOrId & 0x7FFFFFFF; //Remove the top bit

                var dataIsDirectory = ((entry->OffsetToData >> 31) & 1) == 1;
                var offsetToDirectory = entry->OffsetToData & 0x7FFFFFFF; //Remove the top bit

                if (dataIsDirectory)
                {
                    switch (level)
                    {
                        case ResourceLevel.ResourceType:
                            if (nameIsString)
                                continue;

                            var actualResourceType = (ResourceType) nameOffset;

                            if (resourceType != actualResourceType)
                                continue;

                            break;

                        case ResourceLevel.ResourceName:
                            if (!nameIsString)
                                continue;

                            var pName = (IMAGE_RESOURCE_DIR_STRING_U*) (rootAddress + nameOffset);

                            var ctx = new ResourceNameContext(pName);

                            if (!resourceName(ctx))
                                continue;

                            break;

                        case ResourceLevel.ResourceLanguage:
                            //Don't care what language it is
                            break;

                        default:
                            continue;
                    }

                    if (!EnumerateDirectory(offsetToDirectory, rootAddress, resourceType, resourceName, resourceFound, level + 1))
                        return false;
                }
                else
                {
                    if (level != ResourceLevel.ResourceLanguage)
                        continue;

                    var dataEntry = (IMAGE_RESOURCE_DATA_ENTRY*) (rootAddress + entry->OffsetToData);

                    var ctx = new ResourceFoundContext(dataEntry);

                    //todo: if resourcefound callback returns false, stop iterating? how do we tell the caller to stop iterating though?
                    if (!resourceFound(ctx))
                        return false;
                }
            }

            //Keep iterating
            return true;
        }

        private readonly byte[] runtimeInfoSignature = new byte[]
        {
            (byte)'D', (byte)'o', (byte)'t', (byte)'N', (byte)'e', (byte)'t',
            (byte)'R', (byte)'u', (byte)'n', (byte)'t', (byte)'i', (byte)'m',
            (byte)'e', (byte)'I', (byte)'n', (byte)'f', (byte)'o'
        };

        public bool TryGetRuntimeInfo(out Version? runtimeVersion)
        {
            runtimeVersion = default;

            if (!TryGetExport("DotNetRuntimeInfo", out var rva, out _))
                return false;

            var runtimeInfo = (PESpy.Native.RuntimeInfo*) ResolveRva(rva);

            if (runtimeInfo == (PESpy.Native.RuntimeInfo*) 0)
                return false;

            var sig = runtimeInfo->Signature;

            for (var i = 0; i < runtimeInfoSignature.Length; i++)
            {
                if (sig[i] != runtimeInfoSignature[i])
                    return false;
            }

            //We have Runtime Info

            if (runtimeInfo->Version >= 2)
            {
                //And we also have a runtime version
                var v = runtimeInfo->RuntimeVersion;
                runtimeVersion = new Version(v[0], v[1], v[2], v[3]);
            }

            return true;
        }

        public bool TryGetClrEngineMetrics(out long continueStartupEvent)
        {
            continueStartupEvent = default;

            if (!TryGetExport("g_CLREngineMetrics", out var rva, out var ordinal) || ordinal != 2)
                return false;

            var metrics = (CLR_ENGINE_METRICS*) ResolveRva(rva);

            if (metrics == (CLR_ENGINE_METRICS*) 0)
                return false;

            continueStartupEvent = (long) (void*) metrics->phContinueStartupEvent;

            return true;
        }

        public class ResourceNameContext
        {
            private IMAGE_RESOURCE_DIR_STRING_U* str;

            internal ResourceNameContext(IMAGE_RESOURCE_DIR_STRING_U* str)
            {
                this.str = str;
            }

            public bool Equals(string value)
            {
                fixed (char* c = value)
                {
                    return wcsncmp((ushort*) c, str->NameString, str->Length) == 0;
                }
            }

            public bool StartsWith(string value)
            {
                if (value.Length > str->Length)
                    return false;

                fixed (char* c = value)
                {
                    return wcsncmp((ushort*) c, str->NameString, value.Length) == 0;
                }
            }

            private int wcsncmp(ushort* a, ushort* b, int count)
            {
                for (var i = 0; i < count; i++)
                {
                    var diff = *a - *b;

                    if (diff != 0)
                        return diff;

                    a++;
                    b++;
                }

                return 0;
            }
        }

        public class ResourceFoundContext
        {
            private IMAGE_RESOURCE_DATA_ENTRY* dataEntry;

            internal ResourceFoundContext(IMAGE_RESOURCE_DATA_ENTRY* dataEntry)
            {
                this.dataEntry = dataEntry;
            }
        }

        enum ResourceLevel
        {
            ResourceType,
            ResourceName,
            ResourceLanguage
        }

        private int strcmp(char* a, byte* b)
        {
            while (*a != '\0' && *b != 0)
            {
                var diff = *a - *b;

                if (diff != 0)
                    return diff;

                a++;
                b++;
            }

            return *a - *b;
        }

        ~FastPEFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (hView != IntPtr.Zero)
            {
                UnmapViewOfFile(hView);
                hView = IntPtr.Zero;
            }

            if (hMap != IntPtr.Zero)
            {
                CloseHandle(hMap);
                hMap = IntPtr.Zero;
            }

            if (hFile != IntPtr.Zero)
            {
                CloseHandle(hFile);
                hFile = IntPtr.Zero;
            }

            if (disposing)
            {
                //This is a bit of a gotcha! If you declare a finalizer, it won't be GC'd until the finalizer thread processes it.
                //Which means if you're generating a lot of objects, the finalizer thread might not be able to keep up
                GC.SuppressFinalize(this);
            }

            disposed = true;
        }


        #region Native

        private const string kernel32 = "kernel32.dll";

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private const int FILE_ATTRIBUTE_NORMAL = 0x80;
        private const int FILE_MAP_READ = 4;
        private const int FILE_SHARE_READ = 1;
        private const uint GENERIC_READ = 0x80000000;
        private const int INVALID_FILE_SIZE = -1;
        private const int OPEN_EXISTING = 3;
        private const int PAGE_READONLY = 2;

        [DllImport(kernel32, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport(kernel32, SetLastError = true)]
        private static extern IntPtr CreateFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [In] uint dwDesiredAccess,
            [In] int dwShareMode,
            [In] IntPtr lpSecurityAttributes,
            [In] int dwCreationDisposition,
            [In] int dwFlagsAndAttributes,
            [In] IntPtr hTemplateFile);

        [DllImport(kernel32, SetLastError = true)]
        private static extern IntPtr CreateFileMappingA( //Doesn't seem like there's a W version
            [In] IntPtr hFile,
            [In] IntPtr lpFileMappingAttributes,
            [In] int flProtect,
            [In] int dwMaximumSizeHigh,
            [In] int dwMaximumSizeLow,
            [In, MarshalAs(UnmanagedType.LPStr)] string lpName);

        [DllImport(kernel32, SetLastError = true)]
        private static extern int GetFileSize(
            [In] IntPtr hFile,
            [Out] out int lpFileSizeHigh);

        [DllImport(kernel32, SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            [In] IntPtr hFileMappingObject,
            [In] int dwDesiredAccess,
            [In] int dwFileOffsetHigh,
            [In] int dwFileOffsetLow,
            [In] IntPtr dwNumberOfBytesToMap);

        [DllImport(kernel32, SetLastError = true)]
        private static extern bool UnmapViewOfFile(
            [In] IntPtr lpBaseAddress);

        #endregion
    }
}
#endif
