using System;
using System.Diagnostics;
using System.IO;
using PESpy.View;

namespace PESpy
{
    //http://www.textfiles.com/programming/FORMATS/lxexe.txt

    //VXD files use the Linear Executable (LE) file format

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
                    var end = DosHeader.FileAddressOfNewExeHeader;

                    var length = (int) (end - start);

                    dosStub = new ByteBlob(new MemoryChunk(globalBlock, start), length);
                }

                return ref dosStub;
            }
        }

        #endregion
        #region VXDHeader

        private ImageVXDHeader vxdHeader;

        public ref readonly ImageVXDHeader VXDHeader => ref vxdHeader;

        #endregion

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.LE;

        public int Length => globalBlock.Length;

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;

        private bool disposed;

        private unsafe LEFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            ReadVXDHeaders();
        }

        ~LEFile()
        {
            Dispose(false);
        }

        private void ReadVXDHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(globalBlock, 0));

            vxdHeader = new ImageVXDHeader(new MemoryChunk(globalBlock, dosHeader.FileAddressOfNewExeHeader));
        }

        public unsafe FileView GetView()
        {
            var writer = new LEViewWriter(this, mmf.Address, (int) mmf.Length, null);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(DosHeader);
            writer.WriteDosStub(DosStub);
            writer.WriteGlobal(VXDHeader);
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
    }
}
