using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy
{
    public class DBGFile : IFile, IViewable, IDisposable
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
                mmf.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Locates a file on the symbol server and opens it as a <see cref="DBGFile"/>.
        /// </summary>
        /// <param name="symStoreKey">The <see cref="SymStoreKey"/> describing the file that should be located and opened.</param>
        /// <returns>A <see cref="DBGFile"/> that provides access to the contents of the specified file.</returns>
        /// <exception cref="ArgumentException">The specified <see cref="SymStoreKey"/> cannot be opened as a <see cref="DBGFile"/>.</exception>
        public static DBGFile FromKey(SymStoreKey symStoreKey)
        {
            switch (symStoreKey.Kind)
            {
                case SymStoreKeyKind.PDB:
                    var path = Locator.Locate(symStoreKey);

                    return FromFile(path);

                default:
                    throw new ArgumentException($"{nameof(SymStoreKey)} '{symStoreKey}' of type '{symStoreKey.Kind}' cannot be opened as a {nameof(DBGFile)}");
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

        public int Length => globalBlock.Length;

        private ISymbolAccessor symbolAccessor;

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
                        using var exportedNames = new PooledList<RawValue<AnsiString>>();

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

        public FileView GetView(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default)
        {
            var writer = new DBGViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (symbolAccessor != null)
                return symbolAccessor;

            if (!ImageDebugDirectory.TryGetSymbolAccessor(this, DebugTable, out symbolAccessor))
                symbolAccessor = NullSymbolAccessor.Instance;

            return symbolAccessor;
        }

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, (int) mmf.Length, fileAccessor);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(DebugHeader);
            writer.WriteGlobal(SectionHeaders);

            var names = ExportedNames;

            if (names != null && names.Length > 0)
            {
                var offset = names[0].Offset;

                using var r = writer.CreateRegion(offset, "Exported Names", ViewKind.ExportedNames, true);

                foreach (var item in names)
                    r.WriteInlineAnsiNullTerminatedValue(item, ViewKind.ExportedNames_Entry);
            }

            writer.WriteGlobal(DebugTable);
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
