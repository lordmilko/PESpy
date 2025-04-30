#if PEFAST
using System;
using System.Diagnostics;
using System.IO;

namespace PESpy.VXD
{
    //VXD files use the Linear Executable (LE) file format
    internal class LEFile : IFile
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
                mmf.Close();

                throw;
            }
        }

        #region DosHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDosHeader dosHeader;

        public ref readonly ImageDosHeader DosHeader => ref dosHeader;

        #endregion

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.LE;

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;

        private bool disposed;

        private unsafe LEFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length);

            ReadVXDHeaders();
        }

        ~LEFile()
        {
            Dispose(false);
        }

        private void ReadVXDHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(globalBlock, 0));

            //IMAGE_VXD_HEADER follows
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
