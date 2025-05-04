using System;
using System.IO;
using PESpy.View;

namespace PESpy
{
    public class PortablePDBFile : IFile, IViewable, IDisposable //Not a PDBFile, as the format is not similar in any way what-so-ever
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
                mmf.Close();

                throw;
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.PortablePDB;

        public EcmaMetadata EcmaMetadata { get; }

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;

        private bool disposed;

        internal unsafe PortablePDBFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            EcmaMetadata = new EcmaMetadata(new MemoryChunk(globalBlock, 0));
        }

        ~PortablePDBFile()
        {
            Dispose(false);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            throw new NotImplementedException();
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
