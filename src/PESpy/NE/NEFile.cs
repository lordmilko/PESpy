using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PESpy.NE;
using PESpy.View;
using PESpy.View.Builder;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy
{
    //http://benoit.papillault.free.fr/c/disc2/exefmt.txt

    /// <summary>
    /// Represents a New Executable (NE) file.
    /// </summary>
    public class NEFile : IFile, IFileWithCodeViewData, IViewable, IDisposable
    {
        public static unsafe NEFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                if (mmf.Length >= 2 && *(ushort*) mmf.Address != ImageDosHeader.IMAGE_DOS_SIGNATURE)
                {
                    if (Detector.TryExtract(mmf, out var decompressionInfo))
                    {
                        mmf.Dispose(); //Don't need this anymore!

                        //Replace with our own one
                        mmf = new MemoryMappedFileHolder(decompressionInfo.Bytes);

                        Detector.TryGetUncompressedFileName(path, decompressionInfo.ExtensionChar, out var name);

                        return new NEFile(path, mmf, name: name);
                    }
                }

                return new NEFile(fs.Name, mmf);
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
                    var end =  DosHeader.FileAddressOfNewExeHeader;

                    var length = (int) (end - start);

                    dosStub = new ByteBlob(new MemoryChunk(globalBlock, start), length, ViewKind.DosStub);
                }

                return dosStub;
            }
        }

        #endregion
        #region OS2Header

        private ImageOS2Header os2Header;

        public ImageOS2Header OS2Header => os2Header;

        #endregion
        #region SegmentTable

        private new_seg[]? segmentTable;

        public new_seg[] SegmentTable
        {
            get
            {
                if (segmentTable == null)
                {
                    var segments = new new_seg[os2Header.ne_cseg];

                    var segmentsChunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.ne_segtab);

                    //The location of the segment table is relative to the start of the IMAGE_OS2_HEADER. So if
                    //the location is 0x40 (64) it immediately follows the IMAGE_OS2_HEADER
                    for (var i = 0; i < segments.Length; i++)
                        segments[i] = new new_seg(segmentsChunk.Slice(i * new_seg.StructSize));

                    segmentTable = segments;
                }

                return segmentTable;
            }
        }

        #endregion
        #region ResourceTable

        private new_rsrc? resourceTable;

        public new_rsrc? ResourceTable
        {
            get
            {
                if (resourceTable == null)
                {
                    //The tables in a NE file are sequentially ordered. I believe each entry's size can be computed by
                    //looking at the different between that entry and the one next to it. This is a technique that Windows
                    //does use in some scenarios (e.g. definitely in the case of resources)

                    if (os2Header.ne_rsrctab == os2Header.ne_restab)
                        return null; //Size of resource table is therefore 0

                    var resourcesChunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.ne_rsrctab);

                    resourceTable = default;
                    throw new NotImplementedException();
                }

                return resourceTable;
            }
        }

        #endregion
        #region Resident Name Table

        #endregion
        #region Module Reference Table

        private ushort[]? moduleReferenceTable;

        public ushort[]? ModuleReferenceTable
        {
            get
            {
                if (moduleReferenceTable == null)
                {
                    if (os2Header.ne_modtab != os2Header.ne_imptab)
                    {
                        var chunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.ne_modtab);

                        moduleReferenceTable = chunk.PeekSpan<ushort>(0, os2Header.ne_cmod).ToArray();
                    }
                }

                return moduleReferenceTable;
            }
        }

        #endregion
        #region Imported Names Table

        private SymString[]? importedNamesTable;

        public unsafe SymString[]? ImportedNamesTable
        {
            get
            {
                if (importedNamesTable == null)
                {
                    if (os2Header.ne_imptab != os2Header.ne_enttab)
                    {
                        var chunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.ne_imptab);

                        var length = os2Header.ne_enttab - os2Header.ne_imptab;

                        var read = 0;

                        using var results = new ValueList<SymString>();

                        while (read < length)
                        {
                            var str = chunk.PeekSymString(read, isLengthPrefixed: true);

                            results.Add(str);

                            read += str.Length + 1;
                        }

                        importedNamesTable = results.ToArray();
                    }
                }

                return importedNamesTable;
            }
        }

        #endregion
        #region Entry TableE

        private NEBundle[]? entryTable;

        public NEBundle[]? EntryTable
        {
            get
            {
                if (entryTable == null)
                {
                    if (os2Header.ne_enttab != os2Header.ne_nrestab - os2Header.Offset)
                    {
                        var chunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.ne_enttab);

                        var length = os2Header.ne_nrestab - os2Header.Offset - os2Header.ne_enttab;
                        Debug.Assert(length == OS2Header.ne_cbenttab);

                        using var results = new ValueList<NEBundle>();

                        var read = 0;

                        while (read < length)
                        {
                            var numEntries = chunk.PeekByte(read);

                            if (numEntries == 0)
                                break; //Sometimes there's 1 byte remaining (e.g. when there were no records), sometimes there's 2 bytes remaining. People say when the countis 0, it's time to give up

                            read++;

                            var segmentIndicator = chunk.PeekByte(read);
                            read++;

                            var entries = new NEBundle.Entry[numEntries];

                            if (segmentIndicator == NEBundle.ENT_MOVEABLE)
                            {
                                for (var i = 0; i < numEntries; i++)
                                {
                                    //Movable segment entry
                                    var flags = (EntryFlags) chunk.PeekByte(read);
                                    read++;

                                    var int3f = chunk.PeekNativeSpan<byte>(read, 2); //0xCD, 0x3F
                                    read += 2;

                                    var segmentNumber = chunk.PeekByte(read);
                                    read++;

                                    var relativeOffset = chunk.PeekInt16(read);
                                    read += 2;

                                    entries[i] = new NEBundle.MoveableEntry(flags, int3f, segmentNumber, relativeOffset);
                                }
                            }
                            else
                            {
                                for (var i = 0; i < numEntries; i++)
                                {
                                    var flags = (EntryFlags) chunk.PeekByte(read);
                                    read++;

                                    var relativeOffset = chunk.PeekInt16(read);
                                    read += 2;

                                    entries[i] = new NEBundle.Entry(flags, relativeOffset);
                                }
                            }

                            results.Add(new NEBundle(numEntries, segmentIndicator, entries));
                        }

                        entryTable = results.ToArray();
                    }
                }

                return entryTable;
            }
        }

        #endregion
        #region Non-Resident Name Table

        #endregion
        #region OMFData

        private ICodeViewData? codeViewData;
        private bool hasTriedCodeViewData;

        public unsafe ICodeViewData? CodeViewData
        {
            get
            {
                if (codeViewData == null && !hasTriedCodeViewData)
                {
                    //We don't seem to have an IMAGE_FILE_MACHINE anywhere, so assume x86
                    OMFReader.TryReadTrailingOMF(globalBlock.LocalPointer, (int) globalBlock.Length, IMAGE_FILE_MACHINE_I386, globalBlock, out codeViewData);
                    hasTriedCodeViewData = true;
                }

                return codeViewData;
            }
        }

        #endregion

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.NE;

        public long Length => globalBlock.Length;

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private TableBounds[] tableBounds;

        private bool disposed;

        internal unsafe NEFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, mmf.Length, this);

            try
            {
                ReadNEHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public int EntryPoint
        {
            get
            {
                var csip = OS2Header.ne_csip;

                var seg = csip >> 16;
                var ip = csip & 0xFFFF;

                return GetPhysicalOffset(seg, ip);
            }
        }

        public int GetPhysicalOffset(int segmentNo, int relativeOffset)
        {
            var seg = SegmentTable[segmentNo - 1];

            var segmentStart = seg.ns_sector << OS2Header.ne_align;

            if (relativeOffset > seg.ns_cbseg)
                throw new InvalidOperationException("Relative offset is not within the bounds of the specified segment");

            return segmentStart + relativeOffset;
        }

        //segmentNo is 1-based
        public bool TryGetSegment(int physicalOffset, out int segmentNo, out int relativeOffset)
        {
            var segmentTable = SegmentTable;

            for (var i = 0; i < segmentTable.Length; i++)
            {
                ref var seg = ref segmentTable[i];

                var segmentStart = seg.ns_sector << OS2Header.ne_align;
                var segmentEnd = segmentStart + seg.ns_cbseg;

                if (physicalOffset >= segmentStart && physicalOffset < segmentEnd)
                {
                    segmentNo = i + 1;
                    relativeOffset = physicalOffset - segmentStart;
                    return true;
                }
            }

            segmentNo = default;
            relativeOffset = default;
            return false;
        }

        ~NEFile()
        {
            Dispose(false);
        }

        private unsafe void ReadNEHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(globalBlock, 0));

            os2Header = new ImageOS2Header(new MemoryChunk(globalBlock, dosHeader.FileAddressOfNewExeHeader));

            tableBounds = ComputeTableBounds();
        }

        private TableBounds[] ComputeTableBounds()
        {
            var sizeOfHeaders = DosHeader.FileAddressOfNewExeHeader + ImageOS2Header.StructSize;

            var tableBounds = new TableBounds[7];

            var lastSectionEnd = sizeOfHeaders;

            var index = 0;

            ReadTable("Segment Table",          tableOffset: os2Header.ne_segtab,  os2Header.ne_rsrctab, os2Header, ref index, ref lastSectionEnd, tableBounds, ViewKind.NE_SegmentTable);
            ReadTable("Resource Table",         tableOffset: os2Header.ne_rsrctab, os2Header.ne_restab,  os2Header, ref index, ref lastSectionEnd, tableBounds, ViewKind.NE_ResourceTable);
            ReadTable("Resident Name Table",    tableOffset: os2Header.ne_restab,  os2Header.ne_modtab,  os2Header, ref index, ref lastSectionEnd, tableBounds, ViewKind.NE_ResidentNameTable);
            ReadTable("Module Reference Table", tableOffset: os2Header.ne_modtab,  os2Header.ne_imptab,  os2Header, ref index, ref lastSectionEnd, tableBounds, ViewKind.NE_ModuleReferenceTable);
            ReadTable("Imported Names Table",   tableOffset: os2Header.ne_imptab,  os2Header.ne_enttab,  os2Header, ref index, ref lastSectionEnd, tableBounds, ViewKind.NE_ImportedNamesTable);
            ReadTable("Entry Table",            tableOffset: os2Header.ne_enttab,  os2Header.ne_nrestab - (int) os2Header.Offset, os2Header, ref index, ref lastSectionEnd, tableBounds, ViewKind.NE_EntryTable); //OffsetOfNonResidentNamesTable is relative to the beginning of the file

            //Non-Resident Name Table is last, so its length must be computed using a count, rather than
            //the position of the table after it
            ReadNonResidentNameTable("Non-Resident Name Table", os2Header, ref index, ref lastSectionEnd, tableBounds, ViewKind.NE_NonResidentNameTable);

            return tableBounds;
        }

        private void ReadTable(
            string name,
            int tableOffset,
            int nextTableOffset,
            in ImageOS2Header os2Header,
            ref int index,
            ref int lastSectionEnd,
            TableBounds[] tableBounds,
            ViewKind kind)
        {
            if (tableOffset == nextTableOffset)
            {
                //Size is 0
                tableBounds[index++] = new TableBounds(name);
                return;
            }

            var start = (int) os2Header.Offset + tableOffset;
            var length = nextTableOffset - tableOffset;
            var end = start + length;

            tableBounds[index++] = new TableBounds(name, start, end, kind);

            lastSectionEnd = end;
        }

        private void ReadNonResidentNameTable(
            string name,
            in ImageOS2Header os2Header,
            ref int index,
            ref int lastSectionEnd,
            TableBounds[] tableBounds,
            ViewKind kind)
        {
            if (os2Header.ne_cbnrestab == 0)
            {
                //There's no table after it, hence why there's an explicit size listed for it
                tableBounds[index++] = new TableBounds(name);
                return;
            }

            var start = os2Header.ne_nrestab;
            var length = os2Header.ne_cbnrestab;
            var end = start + length;

            tableBounds[index++] = new TableBounds(name, start, end, kind);

            lastSectionEnd = end;
        }

        private FileAccessor? _viewAccessor;

        public FileView GetView(in FileAnalyzerOptions options = default)
        {
            if (_viewAccessor == null)
            {
                var accessor = FileAccessor.Create(this);
                FileAnalyzer.Analyze(accessor, options);
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

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (CodeViewData != null)
                return (ISymbolAccessor) ((NB05Data) CodeViewData).GetCodeViewAccessor();

            return NullSymbolAccessor.Instance;
        }

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, mmf.Length, fileAccessor);

        public unsafe void GetRawHeaderData(out byte* ptr, out int remainingLength)
        {
            ptr = globalBlock.LocalPointer;
            remainingLength = (int) globalBlock.Length;
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void GetRawPointer(out byte* pointer, out long length)
        {
            pointer = mmf.Address;
            length = mmf.Length;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(DosHeader);
            writer.WriteDosStub(DosStub);
            writer.WriteGlobal(OS2Header);
            writer.WriteGlobal(SegmentTable);

            //Resource Table
            //Resident Name Table

            //Module Reference Table
            if (os2Header.ne_modtab != os2Header.ne_imptab)
            {
                var moduleReferences = ModuleReferenceTable;
                var offset = os2Header.Offset + os2Header.ne_modtab;

                for (var i = 0; i < moduleReferences!.Length; i++)
                {
                    writer.WriteGlobal(offset + (i * sizeof(short)), moduleReferences[i], sizeof(ushort), ViewKind.NE_ModuleReference);
                }
            }

            //Imported Names Table
            if (os2Header.ne_imptab != os2Header.ne_enttab)
            {
                var importedNames = ImportedNamesTable;
                var offset = os2Header.Offset + os2Header.ne_imptab;

                foreach (var name in importedNames!)
                {
                    writer.WriteGlobal(offset, name, name.Length + 1, ViewKind.NE_ImportedName_String);

                    offset += name.Length + 1;
                }
            }

            //Entry Table
            //Non-Resident Name Table

            writer.WriteGlobal((IViewable?) CodeViewData);
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

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
