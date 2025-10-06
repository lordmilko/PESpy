using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using PESpy.NE;
using PESpy.View;

namespace PESpy
{
    //http://benoit.papillault.free.fr/c/disc2/exefmt.txt

    /// <summary>
    /// Represents a New Executable (NE) file.
    /// </summary>
    public class NEFile : IFile, IFileWithCodeViewData, IViewable, IDisposable
    {
        public static NEFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
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

        public ref readonly ImageDosHeader DosHeader => ref dosHeader;

        #endregion
        #region DosStub

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ByteBlob dosStub;

        public ref readonly ByteBlob DosStub
        {
            get
            {
                if (dosStub.Offset == 0)
                {
                    var start = ImageDosHeader.StructSize;
                    var end =  DosHeader.FileAddressOfNewExeHeader;

                    var length = (int) (end - start);

                    dosStub = new ByteBlob(new MemoryChunk(globalBlock, start), length);
                }

                return ref dosStub;
            }
        }

        #endregion
        #region OS2Header

        private ImageOS2Header os2Header;

        public ref readonly ImageOS2Header OS2Header => ref os2Header;

        #endregion
        #region SegmentTable

        private NewSeg[]? segmentTable;

        public NewSeg[] SegmentTable
        {
            get
            {
                if (segmentTable == null)
                {
                    var segments = new NewSeg[os2Header.CountOfFileSegments];

                    var segmentsChunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.OffsetOfSegmentTable);

                    //The location of the segment table is relative to the start of the IMAGE_OS2_HEADER. So if
                    //the location is 0x40 (64) it immediately follows the IMAGE_OS2_HEADER
                    for (var i = 0; i < segments.Length; i++)
                        segments[i] = new NewSeg(segmentsChunk.Slice(i * NewSeg.StructSize));

                    segmentTable = segments;
                }

                return segmentTable;
            }
        }

        #endregion
        #region ResourceTable

        private NewRsrc? resourceTable;

        public NewRsrc? ResourceTable
        {
            get
            {
                if (resourceTable == null)
                {
                    //The tables in a NE file are sequentially ordered. I believe each entry's size can be computed by
                    //looking at the different between that entry and the one next to it. This is a technique that Windows
                    //does use in some scenarios (e.g. definitely in the case of resources)

                    if (os2Header.OffsetOfResourceTable == os2Header.OffsetOfResidentNameTable)
                        return null; //Size of resource table is therefore 0

                    var resourcesChunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.OffsetOfResourceTable);

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
                    if (os2Header.OffsetOfModuleReferenceTable != os2Header.OffsetOfImportedNamesTable)
                    {
                        var chunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.OffsetOfModuleReferenceTable);

                        moduleReferenceTable = chunk.PeekSpan<ushort>(0, os2Header.EntriesInModuleReferenceTable).ToArray();
                    }
                }

                return moduleReferenceTable;
            }
        }

        #endregion
        #region Imported Names Table

        private FixedAnsiString[]? importedNamesTable;

        public unsafe FixedAnsiString[]? ImportedNamesTable
        {
            get
            {
                if (importedNamesTable == null)
                {
                    if (os2Header.OffsetOfImportedNamesTable != os2Header.OffsetOfEntryTable)
                    {
                        var chunk = new MemoryChunk(globalBlock, os2Header.Offset + os2Header.OffsetOfImportedNamesTable);

                        var length = os2Header.OffsetOfEntryTable - os2Header.OffsetOfImportedNamesTable;

                        var read = 0;

                        using var results = new PooledList<FixedAnsiString>();

                        while (read < length)
                        {
                            var strLen = chunk.PeekByte(read);

                            results.Add(new FixedAnsiString(chunk.Pointer + read + 1, strLen));

                            read += strLen + 1;
                        }

                        importedNamesTable = results.ToArray();
                    }
                }

                return importedNamesTable;
            }
        }

        #endregion
        #region Entry Table

        #endregion
        #region Non-Resident Name Table

        #endregion
        #region OMFData

        private ICodeView? codeViewData;
        private bool hasTriedCodeViewData;

        public unsafe ICodeView? CodeViewData
        {
            get
            {
                if (codeViewData == null && !hasTriedCodeViewData)
                {
                    OMFReader.TryReadTrailingOMF(globalBlock.LocalPointer, globalBlock.Length, globalBlock, out codeViewData);
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

        public int Length => globalBlock.Length;

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;

        private bool disposed;

        internal unsafe NEFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            ReadNEHeaders();
        }

        ~NEFile()
        {
            Dispose(false);
        }

        private unsafe void ReadNEHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(globalBlock, 0));

            os2Header = new ImageOS2Header(new MemoryChunk(globalBlock, dosHeader.FileAddressOfNewExeHeader));
        }

        public unsafe FileView GetView()
        {
            var writer = new NEViewWriter(this, mmf.Address, (int) mmf.Length, null);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void GetRawPointer(out byte* pointer, out int length)
        {
            pointer = mmf.Address;
            length = (int) mmf.Length;
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
            if (os2Header.OffsetOfModuleReferenceTable != os2Header.OffsetOfImportedNamesTable)
            {
                var moduleReferences = ModuleReferenceTable;
                var offset = os2Header.Offset + os2Header.OffsetOfModuleReferenceTable;
                
                for (var i = 0; i < moduleReferences!.Length; i++)
                {
                    writer.WriteGlobal(offset + (i * sizeof(short)), moduleReferences[i], sizeof(ushort), ViewKind.NE_ModuleReference);
                }
            }

            //Imported Names Table
            if (os2Header.OffsetOfImportedNamesTable != os2Header.OffsetOfEntryTable)
            {
                var importedNames = ImportedNamesTable;
                var offset = os2Header.Offset + os2Header.OffsetOfImportedNamesTable;

                foreach (var name in importedNames!)
                {
                    writer.WriteGlobal(offset, (byte) name.Length, sizeof(byte), ViewKind.NE_ImportedName_Length);

                    if (name.Length > 0)
                        writer.WriteGlobal(offset + 1, name, name.Length, ViewKind.NE_ImportedName_String);

                    offset += name.Length + 1;
                }
            }

            //Entry Table
            //Non-Resident Name Table

            writer.WriteGlobal((IViewable?) CodeViewData);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();

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
