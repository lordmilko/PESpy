#if PEFAST
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PESpy.View;

namespace PESpy
{
    class DBGFile : IFile, IViewable, IDisposable
    {
        public static DBGFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new DBGFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Close();

                throw;
            }
        }

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;

        private bool disposed;

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.DBG;

        internal unsafe DBGFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            //Read the DBG Headers
            ReadDbgHeaders();
        }

        private ImageSeparateDebugHeader debugHeader;

        public ref readonly ImageSeparateDebugHeader DebugHeader => ref debugHeader;

        #region SectionHeaders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageSectionHeader[]? sectionHeaders;

        public ImageSectionHeader[] SectionHeaders
        {
            get
            {
                if (sectionHeaders == null)
                {
                    var numberOfSections = debugHeader.NumberOfSections;

                    var list = new ImageSectionHeader[numberOfSections];

                    for (var i = 0; i < numberOfSections; i++)
                        list[i] = new ImageSectionHeader(new MemoryChunk(globalBlock, ImageSeparateDebugHeader.StructSize + (i * ImageSectionHeader.StructSize)));

                    sectionHeaders = list;
                }

                return sectionHeaders;
            }
        }

        #endregion
        #region ExportedNames

        private RawValue<AnsiString>[]? exportedNames;

        public RawValue<AnsiString>[]? ExportedNames
        {
            get
            {
                if (exportedNames == null)
                {
                    if (debugHeader.ExportedNamesSize > 0)
                    {
                        var exportedNames = new List<RawValue<AnsiString>>();

                        var read = 0;

                        var dataChunk = new MemoryChunk(globalBlock, ImageSeparateDebugHeader.StructSize + (debugHeader.NumberOfSections * ImageSectionHeader.StructSize));

                        while (read < debugHeader.ExportedNamesSize)
                        {
                            var str = dataChunk.PeekAnsiNullTerminatedString(read);
                            exportedNames.Add(new RawValue<AnsiString>(dataChunk.AbsoluteOffset + read, str));
                            read += str.Length + 1;
                        }

                        this.exportedNames = exportedNames.ToArray();
                    }
                }

                return exportedNames;
            }
        }

        #endregion
        #region DebugTable

        private ImageDebugDirectory[]? debugTable;

        public ImageDebugDirectory[]? DebugTable
        {
            get
            {
                if (debugHeader.DebugDirectorySize > 0)
                {
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime result = epoch.AddSeconds(0x382368b9);

                    var local = result.ToLocalTime();

                    var v = DateTimeOffset.FromUnixTimeSeconds(0x36D7BCF8);

                    var dataChunk = new MemoryChunk(globalBlock, ImageSeparateDebugHeader.StructSize + (debugHeader.NumberOfSections * ImageSectionHeader.StructSize) + debugHeader.ExportedNamesSize);

                    var numDirectories = debugHeader.DebugDirectorySize / ImageDebugDirectory.StructSize;

                    var results = new ImageDebugDirectory[numDirectories];

                    for (var i = 0; i < numDirectories; i++)
                        results[i] = new ImageDebugDirectory(dataChunk.Slice(i * ImageDebugDirectory.StructSize));

                    debugTable = results;
                }

                return debugTable;
            }
        }

        #endregion

        private void ReadDbgHeaders()
        {
            debugHeader = new ImageSeparateDebugHeader(new MemoryChunk(globalBlock, 0));

#if STRESS_TEST
            _ = ExportedNames;
            _ = DebugTable;
#endif
        }

        ~DBGFile()
        {
            Dispose(false);
        }

        public unsafe FileView GetView()
        {
            var writer = new DBGViewWriter(this, mmf.Address, (int) mmf.Length);
            ((IViewable) this).WriteView(writer);

            return (FileView) writer.Finalize();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(DebugHeader);
            writer.WriteGlobal(SectionHeaders);

            var names = ExportedNames;

            if (names != null && names.Length > 0)
            {
                using var r = writer.CreateRegion(names[0].Offset, "Exported Names", ViewKind.ExportedNames, true);

                foreach (var item in names)
                    r.WriteInlineAnsiNullTerminatedValue(item);
            }

            writer.WriteGlobal(DebugTable);
        }

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

            mmf.Close();

            disposed = true;
        }
    }
}
#endif
