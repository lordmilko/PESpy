using System;
using System.IO;
using System.Threading;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    //The whole file is just NB02 data
    internal class OMFDBGFile : IFile
    {
        public static OMFDBGFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new OMFDBGFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.OMFDBG;

        public int Length => globalBlock.Length;

        #region NB02Data

        public int LfoDir => data.LfoDir;

        public CodeViewSig Signature => data.Signature;

        public int LfoBase => data.LfoBase;

        public ushort cDir => data.cDir;

        public dnt[] DirEntries => data.DirEntries;

        #endregion

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private ISymbolAccessor symbolAccessor;
        private bool disposed;

        private NB02Data data;

        internal unsafe OMFDBGFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            try
            {
                if (!OMFReader.TryReadTrailingOMF(mmf.Address, (int) mmf.Length, IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386, globalBlock, out var omfData))
                    throw new BadImageFormatException("File does not contain OMF debug data");

                data = (NB02Data) omfData;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        ~OMFDBGFile()
        {
            Dispose(false);
        }

        public FileView GetView(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => symbolAccessor ??= new NB02SymbolAccessor(data);

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
