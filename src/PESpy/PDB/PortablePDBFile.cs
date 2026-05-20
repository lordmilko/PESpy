using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using PESpy.View;

namespace PESpy
{
    public class PortablePDBFile : IFileInternal, IViewable, IDisposable //Not a PDBFile, as the format is not similar in any way what-so-ever
    {
        public static PortablePDBFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new PortablePDBFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        public static unsafe PortablePDBFile FromEmbeddedFile(EmbeddedPortablePdb embeddedPortablePdb)
        {
            var input = new UnmanagedMemoryStream((byte*) embeddedPortablePdb.PortablePdbImage, embeddedPortablePdb.PortablePdbImage.Length);

            using var deflate = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);

            if (embeddedPortablePdb.UncompressedSize == 0)
                throw new InvalidOperationException("Cannot open Embedded Portable PDB: uncompressed size is 0");

            //We want to create an MMF around the data

            var mmf = new MemoryMappedFileHolder(embeddedPortablePdb.UncompressedSize);

            try
            {
#if NET
                var actualLength = deflate.ReadAtLeast(new Span<byte>(mmf.Address, (int) mmf.Length), embeddedPortablePdb.UncompressedSize, throwOnEndOfStream: false);
#else
                using var output = new UnmanagedMemoryStream(mmf.Address, mmf.Length, mmf.Length, FileAccess.Write);
                deflate.CopyTo(output);
                var actualLength = (int) output.Position;
#endif

                if (actualLength != embeddedPortablePdb.UncompressedSize)
                    throw new BadImageFormatException();

                if (deflate.ReadByte() != -1)
                    throw new BadImageFormatException(); //We should have read to the end

                return new PortablePDBFile(null, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Locates a file on the symbol server and opens it as a <see cref="PortablePDBFile"/>.
        /// </summary>
        /// <param name="symStoreKey">The <see cref="SymStoreKey"/> describing the file that should be located and opened.</param>
        /// <returns>A <see cref="PortablePDBFile"/> that provides access to the contents of the specified file.</returns>
        /// <exception cref="ArgumentException">The specified <see cref="SymStoreKey"/> cannot be opened as a <see cref="PortablePDBFile"/>.</exception>
        public static PortablePDBFile FromKey(SymStoreKey symStoreKey)
        {
            switch (symStoreKey.Kind)
            {
                case SymStoreKeyKind.PDB:
                case SymStoreKeyKind.PortablePDB:
                    var path = Locator.Locate(symStoreKey);

                    return FromFile(path);

                default:
                    throw new ArgumentException($"{nameof(SymStoreKey)} '{symStoreKey}' of type '{symStoreKey.Kind}' cannot be opened as a {nameof(PortablePDBFile)}");
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.PortablePDB;

        public long Length => globalBlock.Length;

        public EcmaMetadata EcmaMetadata { get; }

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;
        private PortablePDBFileSymbolAccessor symbolAccessor;

        private bool disposed;

        internal unsafe PortablePDBFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, mmf.Length, this);

            EcmaMetadata = new EcmaMetadata(new MemoryChunk(globalBlock, 0));
        }

        ~PortablePDBFile()
        {
            Dispose(false);
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

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => symbolAccessor ??= new PortablePDBFileSymbolAccessor(this);

        ByteViewProvider IFileInternal.CreateByteViewProvider(FileAccessor fileAccessor) => CreateByteViewProvider(fileAccessor);

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, mmf.Length, fileAccessor);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void GetRawHeaderData(out byte* pointer, out int length)
        {
            pointer = mmf.Address;
            length = (int) mmf.Length;
        }

        bool IFileInternal.TryGetValueChunkFromPhysicalOffset(int offset, out MemoryChunk chunk) =>
            TryGetValueChunkFromPhysicalOffset(offset, out chunk);

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
            writer.WriteGlobal(EcmaMetadata);
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
