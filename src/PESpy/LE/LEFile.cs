using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PESpy.View;
using PESpy.View.Builder;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy
{
    //http://www.textfiles.com/programming/FORMATS/lxexe.txt

    //VXD files use the Linear Executable (LE) file format

    internal class LEFileDebugView
    {
        private LEFile leFile;

        public LEFileDebugView(LEFile leFile)
        {
            this.leFile = leFile;
        }

        public ImageDosHeader DosHeader => leFile.DosHeader;

        public ByteBlob DosStub => leFile.DosStub;

        public ImageVXDHeader VXDHeader => leFile.VXDHeader;

        public string Name => leFile.Name;

        public string FileName => leFile.FileName;

        public FileKind Kind => leFile.Kind;

        public int Length => leFile.Length;

        public ICodeViewData CodeViewData => leFile.CodeViewData;
    }

    /// <summary>
    /// Represents a Linear Executable (LE) file.
    /// </summary>
    [DebuggerTypeProxy(typeof(LEFileDebugView))]
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

                    dosStub = new ByteBlob(new MemoryChunk(globalBlock, start), length, ViewKind.DosStub);
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

        private bool disposed;

        internal unsafe LEFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

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

        ~LEFile()
        {
            Dispose(false);
        }

        private unsafe void ReadVXDHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(globalBlock, 0));
            vxdHeader = new ImageVXDHeader(new MemoryChunk(globalBlock, dosHeader.FileAddressOfNewExeHeader));
        }

        public unsafe FileView GetView(LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None, CancellationToken cancellationToken = default)
        {
            var writer = new LEViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        //We need to update this if we ever find OMF data inside a LE file
        public ISymbolAccessor GetSymbolAccessor(LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All, ILocatorProgress? progress = null) => symbolAccessor ??= NullSymbolAccessor.Instance;

        internal unsafe ByteViewProvider CreateByteViewProvider(IViewDisassembler? viewDisassembler)
        {
            viewDisassembler?.Initialize(this);

            return new LocalByteViewProvider(mmf.Address, (int) mmf.Length);
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

            globalBlock.Dispose();
            mmf.Dispose();

            disposed = true;
        }
    }
}
