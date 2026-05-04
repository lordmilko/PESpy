using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PESpy.LE;
using PESpy.View;
using PESpy.View.Builder;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy
{
    //http://www.textfiles.com/programming/FORMATS/lxexe.txt

    //VXD files use the Linear Executable (LE) file format

    internal enum LETableKind
    {
        ObjectTable,
        ObjectPageMap,
        ResourceTable,
        ResidentNameTable,
        EntryTable,
        ModuleDirectiveTable,
        PerPageChecksum,
        FixupPageTable,
        FixupRecordTable,
        ImportModuleNameTable,
        EnumeratedDataPages,
        IteratedDataMap,
        NonResidentNamesTable,
        DebugInfo,
    }

    /// <summary>
    /// Represents a Linear Executable (LE) file.
    /// </summary>
    public class LEFile : IFile, IViewable, IDisposable
    {
        public static LEFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new LEFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        #region DosHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDosHeader dosHeader;

        public ImageDosHeader DosHeader => dosHeader;

        #endregion
        #region DosStub

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ByteBlob dosStub;

        public ByteBlob DosStub
        {
            get
            {
                if (dosStub.Offset == 0)
                {
                    var start = ImageDosHeader.StructSize;
                    var end = DosHeader.FileAddressOfNewExeHeader;

                    var length = (int) (end - start);

                    dosStub = new ByteBlob(new MemoryChunk(globalBlock, start), length, ViewKind.DosStub);
                }

                return dosStub;
            }
        }

        #endregion
        #region VXDHeader

        private ImageVXDHeader vxdHeader;

        public ImageVXDHeader VXDHeader => vxdHeader;

        #endregion
        #region ObjectTable

        private o32_obj[]? objectTable;

        public o32_obj[]? ObjectTable
        {
            get
            {
                if (objectTable == null && TryGetTableChunk(LETableKind.ObjectTable, out var valueChunk, out var length))
                {
                    var results = new o32_obj[vxdHeader.e32_objcnt];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new o32_obj(valueChunk.Slice(i * o32_obj.StructSize));

                    objectTable = results;
                }

                return objectTable;
            }
        }

        #endregion
        #region ObjectPageMap

        private o32_map[]? objectPageMap;

        public o32_map[]? ObjectPageMap
        {
            get
            {
                if (objectPageMap == null)
                {
                    if (TryGetTableChunk(LETableKind.ObjectPageMap, out var valueChunk, out _))
                    {
                        var results = new o32_map[vxdHeader.e32_mpages];

                        for (var i = 0; i < results.Length; i++)
                            results[i] = new o32_map(valueChunk.Slice(i * o32_map.StructSize));

                        objectPageMap = results;
                    }
                }

                return objectPageMap;
            }
        }

        #endregion
        #region ResourceTable

        #endregion
        #region ResidentNameTable

        private FixedAnsiString[] residentNameTable;

        //These are actually SymStrings, but the first one has 3 trailing \0's
        public unsafe FixedAnsiString[] ResidentNameTable
        {
            get
            {
                if (residentNameTable == null)
                {
                    if (TryGetTableChunk(LETableKind.ResidentNameTable, out var valueChunk, out var length))
                    {
                        using var results = new PooledList<FixedAnsiString>();

                        var read = 0;

                        while (read < length)
                        {
                            var isFirst = read == 0;

                            //NT 4 just writes the name of the image in the resident names table; furthermore,
                            //they write 3 extra null terminators after the string

                            var strLen = valueChunk.PeekByte(read);
                            read++;

                            if (isFirst)
                                strLen += 3;

                            var str = valueChunk.PeekAnsiFixedLength(read, strLen);
                            results.Add(str);
                            read += strLen;
                        }

                        residentNameTable = results.ToArray();
                    }
                }

                return residentNameTable;
            }
        }

        #endregion
        #region EntryTable

        private e32_bundle[]? entryTable;

        public e32_bundle[]? EntryTable
        {
            get
            {
                if (entryTable == null && TryGetTableChunk(LETableKind.EntryTable, out var valueChunk, out var length))
                {
                    using var results = new PooledList<e32_bundle>();

                    var read = 0;

                    //Apparently the EntryTable is terminated by a zero byte, which means we're
                    //done when we read up to length -1
                    while (read < length - 1)
                    {
                        var entry = new e32_bundle(valueChunk.Slice(read));

                        results.Add(entry);

                        read += entry.StructSize;
                    }

                    entryTable = results.ToArray();
                }

                return entryTable;
            }
        }

        #endregion
        #region ModuleDirectiveTable

        #endregion
        #region PerPageChecksum

        #endregion
        #region FixupPageTable

        #endregion
        #region FixupRecordTable

        #endregion
        #region ImportModuleNameTable

        #endregion
        #region EnumeratedDataPages

        #endregion
        #region IteratedDataMap

        #endregion
        #region NonResidentNamesTable

        private NameAndOrdinal[] nonResidentNamesTable;

        public NameAndOrdinal[] NonResidentNamesTable
        {
            get
            {
                if (nonResidentNamesTable == null)
                {
                    if (TryGetTableChunk(LETableKind.NonResidentNamesTable, out var valueChunk, out var length))
                    {
                        using var results = new PooledList<NameAndOrdinal>();

                        var read = 0;

                        //Per NT 4, the non-resident names table is terminated by a null byte, which means we're done
                        //once we read up to length - 1
                        while (read < length - 1)
                        {
                            var str = valueChunk.PeekSymString(read, isLengthPrefixed: true);
                            read += str.Length + 1;

                            var ordinal = valueChunk.PeekInt16(read);
                            read += sizeof(short);

                            results.Add(new NameAndOrdinal(str, ordinal));
                        }

                        nonResidentNamesTable = results.ToArray();
                    }
                }

                return nonResidentNamesTable;
            }
        }

        #endregion
        #region DebugInfo

        //According to NT 4 this should be an IMAGE_DEBUG_DIRECTORY

        #endregion

        private VxD_Desc_Block? deviceDescriptorBlock;

        //In a VXD, we assert that this must be present
        public VxD_Desc_Block DeviceDescriptorBlock
        {
            get
            {
                if (deviceDescriptorBlock == null)
                {
                    //VXDs have an export at ordinal 1 in the form "<name>_DDB" which points to the VXD's
                    //Device Descriptor Block (DDB). Apparently a VXD should only really have a single export.

                    foreach (var item in NonResidentNamesTable)
                    {
                        if (item.Ordinal == 1)
                        {
                            Debug.Assert(item.Name.EndsWith("_DDB"));

                            //NT 4 emits a bundle for each section number that has E32EXPORT | E32SHARED
                            //with a single entry in it
                            var bundle = EntryTable[item.Ordinal - 1];
                            var obj = ObjectTable[bundle.b32_obj - 1];

                            if (bundle.b32_cnt != 1)
                                throw new InvalidOperationException($"Expected the export containing the DDB to contain exactly 1 entry. Actual count: {bundle.b32_cnt}");

                            if (bundle.b32_type != E32BundleType.ENTRY32)
                                throw new InvalidOperationException($"Expected the export containing the DDB to be of type ENTRY32. Actual type: {bundle.b32_type}");

                            var entry = bundle.Entries[0];

                            var offset = entry.e32_variant.offset.offset32;

                            if (TryGetObjectChunk(obj, offset, out var valueChunk))
                            {
                                deviceDescriptorBlock = new VxD_Desc_Block(valueChunk);
                            }

                            break;
                        }
                    }
                }

                return deviceDescriptorBlock.Value;
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.LE;

        public int Length => globalBlock.Length;

        private ICodeViewData? codeViewData;
        private bool hasTriedCodeViewData;

        public unsafe ICodeViewData? CodeViewData
        {
            get
            {
                //I wasn't able to trick a VXD sample in the Windows 95 DDK into including OMF symbols, but our NB00
                //Windows 3.1 VXD sample does include symbols, so we know these can exist
                if (codeViewData == null && !hasTriedCodeViewData)
                {
                    //We don't seem to have an IMAGE_FILE_MACHINE anywhere, so assume x86
                    OMFReader.TryReadTrailingOMF(globalBlock.LocalPointer, globalBlock.Length, IMAGE_FILE_MACHINE_I386, globalBlock, out codeViewData);
                    hasTriedCodeViewData = true;
                }

                return codeViewData;
            }
        }

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private ISymbolAccessor symbolAccessor;
        internal TableBounds[] tableBounds;

        private bool disposed;

        internal unsafe LEFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            try
            {
                ReadVXDHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the physical offset of the entry point.
        /// </summary>
        public int EntryPoint => GetPhysicalOffset(VXDHeader.e32_startobj, VXDHeader.e32_eip);

        public int GetPhysicalOffset(o32_obj obj, int relativeOffset)
        {
            var pageSize = vxdHeader.e32_pagesize;

            var pageIndex = relativeOffset / pageSize;
            var relativeOffsetInPage = relativeOffset % pageSize;

            var page = ObjectPageMap[obj.o32_pagemap + pageIndex - 1];

            var offset = vxdHeader.e32_datapage + (page.o32_pageidx - 1) * pageSize;

            offset += relativeOffsetInPage;

            return offset;
        }

        public int GetPhysicalOffset(int objectNo, int relativeOffset)
        {
            var obj = ObjectTable[objectNo - 1];

            return GetPhysicalOffset(obj, relativeOffset);
        }

        internal unsafe bool TryGetObjectChunk(o32_obj obj, int relativeOffset, out MemoryChunk chunk)
        {
            var bytes = GetBytes(obj);

            if (relativeOffset < bytes.Length)
            {
                chunk = new MemoryChunk(globalBlock, (int) ((byte*) bytes - globalBlock.LocalPointer) + relativeOffset);
                return true;
            }

            chunk = default;
            return false;
        }

        //Gets all bytes associated with the specified object, with the assumption
        //that all pages will be valid and have contiguous indices.
        //Anything with OBJEXEC is code
        public unsafe NativeSpan<byte> GetBytes(int objectNo)
        {
            var obj = ObjectTable[objectNo - 1];

            return GetBytes(objectNo);
        }

        public unsafe NativeSpan<byte> GetBytes(o32_obj obj)
        {
            var allPages = ObjectPageMap;

            var firstPage = allPages[obj.o32_pagemap - 1];

            var lastIndex = firstPage.o32_pageidx;

            for (var i = 1; i < obj.o32_mapsize; i++)
            {
                var page = allPages[obj.o32_pagemap + i - 1];

                if (page.o32_pageidx != lastIndex + 1)
                    throw new InvalidOperationException("Cannot read object bytes; only sequential streams are supported");

                lastIndex = page.o32_pageidx;
            }

            var offset = vxdHeader.e32_datapage + (firstPage.o32_pageidx - 1) * vxdHeader.e32_pagesize;

            var bytes = new NativeSpan<byte>(globalBlock.LocalPointer + offset, obj.o32_size);

            return bytes;
        }

        public IEnumerable<NativeSpan<byte>> EnumeratePageBytes(int objectNo)
        {
            var obj = ObjectTable[objectNo - 1];

            //The behavior of the NT 4 linker is that o32_pageflags is always VALID and
            //entries in the pagemap always contain sequential page numbers. Therefore,
            //you can always expect all bytes to be sequential. But I'm not sure if maybe
            //other linkers might have things out of order.
            for (var i = 0; i < obj.o32_mapsize; i++)
            {
                if (TryGetPageBytes(obj.o32_pagemap + i, out var bytes))
                    yield return bytes;
            }
        }

        //Page numbers are 1-based
        public unsafe bool TryGetPageBytes(int pageNo, out NativeSpan<byte> bytes)
        {
            var page = ObjectPageMap[pageNo - 1];

            //Page index is also 1-based, so if it's 0 that means it's not valid
            if (page.o32_pageidx == 0 || page.o32_pageflags != PageMapAttributes.VALID)
            {
                bytes = default;
                return false;
            }

            var offset = vxdHeader.e32_datapage + (page.o32_pageidx - 1) * vxdHeader.e32_pagesize;

            var pageSize = vxdHeader.e32_pagesize;

            //Once again, this is true because Page Index is 1-based
            if (page.o32_pageidx == vxdHeader.e32_mpages)
                pageSize = vxdHeader.e32_lastpagesize;

            bytes = new NativeSpan<byte>(globalBlock.LocalPointer + offset, pageSize);
            return true;
        }

        ~LEFile()
        {
            Dispose(false);
        }

        private unsafe void ReadVXDHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(globalBlock, 0));
            vxdHeader = new ImageVXDHeader(new MemoryChunk(globalBlock, dosHeader.FileAddressOfNewExeHeader));

            tableBounds = ComputeTableBounds();
        }

        private TableBounds[] ComputeTableBounds()
        {
            //The end of each table is relative to the start of the section after it, which may or may not have been present;
            //as such we need to build up a complete picture of all present tables so we can easily lookup the contents of each table later on

            var sizeOfHeaders = DosHeader.FileAddressOfNewExeHeader + ImageVXDHeader.StructSize;

            var lastSectionEnd = sizeOfHeaders;

            //Some offsets are relative to the beginning of the file, while others are relative to the beginning of the LE header
            var offsets = new[]
            {
                vxdHeader.e32_objtab,
                vxdHeader.e32_objmap,
                vxdHeader.e32_rsrctab,
                vxdHeader.e32_restab,
                vxdHeader.e32_enttab,
                vxdHeader.e32_dirtab,
                //Resident Directives Data?
                vxdHeader.e32_pagesum,
                vxdHeader.e32_fpagetab,
                vxdHeader.e32_frectab,
                vxdHeader.e32_impmod,
                vxdHeader.e32_datapage  != 0 ? vxdHeader.e32_datapage  - vxdHeader.Offset : 0, //Preload pages? Demand load pages too?
                vxdHeader.e32_itermap   != 0 ? vxdHeader.e32_itermap   - vxdHeader.Offset : 0,
                vxdHeader.e32_nrestab   != 0 ? vxdHeader.e32_nrestab   - vxdHeader.Offset : 0,
                vxdHeader.e32_debuginfo != 0 ? vxdHeader.e32_debuginfo - vxdHeader.Offset : 0
            };

#if DEBUG
            for (var i = 1; i < offsets.Length; i++)
            {
                var current = offsets[i];
                var previous = offsets[i - 1];

                Debug.Assert(current == 0 || current >= previous);
            }
#endif

            int index = 0;

            var fileLength = Length;

            var tableBounds = new TableBounds[14];

            ReadTable("Object Table",             offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_ObjectTable);
            ReadTable("Object Page Map",          offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_ObjectPageMap);
            ReadTable("Resource Table",           offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_ResourceTable);
            ReadTable("Resident Name Table",      offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_ResidentNameTable);
            ReadTable("Entry Table",              offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_EntryTable);
            ReadTable("Module Directive Table",   offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_ModuleDirectiveTable);
            ReadTable("Per-Page Checksum",        offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_PerPageChecksum);
            ReadTable("Fixup Page Table",         offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_FixupPageTable);
            ReadTable("Fixup Record Table",       offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_FixupRecordTable);
            ReadTable("Import Module Name Table", offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_ImportModuleNameTable);
            ReadTable("Enumerated Data Pages",    offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_EnumeratedDataPages);
            ReadTable("Iterated Data Map",        offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_IteratedDataMap);
            ReadTable("Non-Resident Names Table", offsets, vxdHeader, ref index, ref lastSectionEnd, tableBounds, fileLength, ViewKind.LE_NonResidentNamesTable);
            ReadLastTable("Debug Info",           offsets, vxdHeader, ref index, ref lastSectionEnd, vxdHeader.e32_debuglen, tableBounds, fileLength, ViewKind.LE_DebugInfo);

            Debug.Assert(index == offsets.Length);

            return tableBounds;
        }

        internal static void ReadTable(
            string name,
            int[] offsets,
            in ImageVXDHeader vxdHeader,
            ref int index,
            ref int lastSectionEnd,
            TableBounds[] tableBounds,
            int fileLength,
            ViewKind kind)
        {
            var current = offsets[index];

            if (current == 0)
            {
                tableBounds[index++] = new TableBounds(name);
                return;
            }

            var j = index + 1;

            //Our length goes up to the next item that exists. So if an item has a length of 0 we need to skip over it. This assumes that the last item
            //does not have an offset of 0 (which would mess things up, if an earlier one is looking for its end and none was found)
            for (; j < offsets.Length; j++)
            {
                if (offsets[j] != 0)
                    break;
            }

            int next;

            if (j != offsets.Length)
            {
                next = offsets[j];

                if (current == next)
                {
                    //current == next, which means current is empty
                    tableBounds[index++] = new TableBounds(name);
                    return;
                }
            }
            else
                next = fileLength - vxdHeader.Offset;

            //Some offsets are relative to the start of the EXE file, others are relative to the beginning of the LE header.
            //We account for this by subtracting the vxd header offset from our offsets list, so that the common case of having to add the offset here cancels out
            var start = vxdHeader.Offset + current;
            var length = next - current;
            var end = start + length;

            tableBounds[index++] = new TableBounds(name, start, end, kind);
        }

        internal static void ReadLastTable(
            string name,
            int[] offsets,
            in ImageVXDHeader vxdHeader,
            ref int index,
            ref int lastSectionEnd,
            int length,
            TableBounds[] tableBounds,
            int fileLength,
            ViewKind kind)
        {
            var current = offsets[index];
            Debug.Assert(index == offsets.Length - 1); //This should be the last entry

            if (current == 0)
            {
                tableBounds[index++] = new TableBounds(name);
                return;
            }

            var start = vxdHeader.Offset + current;
            var end = start + length;

            tableBounds[index++] = new TableBounds(name, start, end, kind);

            lastSectionEnd = end;
        }

        private bool TryGetTableChunk(LETableKind kind, out MemoryChunk valueChunk, out int length)
        {
            var tableBound = tableBounds[(int) kind];

            if (tableBound.IsPresent)
            {
                valueChunk = new MemoryChunk(globalBlock, tableBound.StartOffset);
                length = tableBound.EndOffset - tableBound.StartOffset;
                return true;
            }

            valueChunk = default;
            length = default;
            return false;
        }

        private FileAccessor? _viewAccessor;

        public FileView GetView(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default)
        {
            if (_viewAccessor == null)
            {
                var accessor = FileAccessor.Create(this);
                FileAnalyzer.Analyze(accessor, httpPolicy: httpPolicy, trackXRefs: trackXRefs, cancellationToken: cancellationToken);
                _viewAccessor = accessor;
            }

            return _viewAccessor.GetFileView();
        }

        public FileView GetViewOld()
        {
            var writer = new ViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        //We need to update this if we ever find OMF data inside a LE file
        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => symbolAccessor ??= NullSymbolAccessor.Instance;

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, (int) mmf.Length, fileAccessor);

        public unsafe void GetRawHeaderData(out byte* ptr, out int remainingLength)
        {
            ptr = globalBlock.LocalPointer;
            remainingLength = globalBlock.Length;
        }

        internal bool TryGetValueChunkFromPhysicalOffset(int offset, out MemoryChunk chunk)
        {
            if (offset < Length)
            {
                chunk = new MemoryChunk(globalBlock, offset);
                return true;
            }

            chunk = default;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(DosHeader);
            writer.WriteDosStub(DosStub);
            writer.WriteGlobal(VXDHeader);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            _viewAccessor?.Dispose();

            globalBlock.Dispose();
            mmf.Dispose();

            disposed = true;
        }
    }
}
