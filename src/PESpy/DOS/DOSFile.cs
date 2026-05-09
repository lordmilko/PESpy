using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using PESpy.Native;
using PESpy.View;
using PESpy.View.Builder;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy
{
    public class DOSFile : IFile, IFileWithCodeViewData, IViewable, IDisposable
    {
        internal const int ParagraphSize = 16;
        internal const int PageSize = 512;

        public static DOSFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new DOSFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

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
        public FileKind Kind => FileKind.DOS;

        public long Length => globalBlock.Length;

        private ImageDosHeader dosHeader;

        /// <summary>
        /// Gets the MS-DOS 2.0 compatible <see cref="IMAGE_DOS_HEADER"/>.
        /// </summary>
        public ImageDosHeader DosHeader => dosHeader;

        public int SizeOfHeaders { get; private set; }

        public int EntryPoint { get; private set; }

        public int StartOfOverlay { get; private set; }

        private ICodeViewData? codeViewData;
        private bool hasTriedCodeViewData;

        public unsafe ICodeViewData? CodeViewData
        {
            get
            {
                if (codeViewData == null && !hasTriedCodeViewData)
                {
                    OMFReader.TryReadTrailingOMF(globalBlock.LocalPointer, (int) globalBlock.Length, IMAGE_FILE_MACHINE_I386, globalBlock, out codeViewData);
                    hasTriedCodeViewData = true;
                }

                return codeViewData;
            }
        }

        public Span<DosRelocation> Relocations =>
            chunk.PeekSpan<DosRelocation>(dosHeader.FileAddressOfRelocationTable, dosHeader.Relocations);

        private readonly MemoryChunk chunk;

        internal unsafe DOSFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, mmf.Length, this);
            chunk = new MemoryChunk(globalBlock, 0);

            try
            {
                ReadDosHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private unsafe void ReadDosHeaders()
        {
            dosHeader = new ImageDosHeader(chunk);

            //e_lfarlc + 4 * e_crlc
            var endOfRelocationTable = dosHeader.FileAddressOfRelocationTable + DosRelocation.StructSize * dosHeader.Relocations;

            //16 * e_cparhdr
            SizeOfHeaders = ParagraphSize * dosHeader.SizeOfHeaderInParagraphs;

            //16 * e_cparhdr + 16 * e_cs + e_ip
            EntryPoint = ParagraphSize * dosHeader.SizeOfHeaderInParagraphs +
                             ParagraphSize * dosHeader.InitialRelativeCSValue +
                             dosHeader.InitialIPValue;

            //e_cblp == 0 ? 512 * e_cp : 512 * (e_cp - 1) + e_cblp;
            StartOfOverlay = dosHeader.BytesOnLastPageOfFile == 0
                ? PageSize * dosHeader.PagesInFile
                : PageSize * (dosHeader.PagesInFile - 1) + dosHeader.BytesOnLastPageOfFile;
        }

        ~DOSFile()
        {
            Dispose(false);
        }

        private FileAccessor? _viewAccessor;

        public FileView GetView(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default)
        {
            if (_viewAccessor == null)
            {
                var accessor = FileAccessor.Create(this);
                FileAnalyzer.Analyze(accessor, httpPolicy: httpPolicy, trackXRefs: trackXRefs, cancellationToken: cancellationToken);
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
            {
                switch (CodeViewData.Signature)
                {
                    case CodeViewSig.DNRB:
                    case CodeViewSig.NB00:
                    case CodeViewSig.NB01:
                    case CodeViewSig.NB02:
                        return (ISymbolAccessor) CodeViewData;

                    default:
                        return (ISymbolAccessor) ((NB05Data) CodeViewData).GetCodeViewAccessor();
                }
            }

            return NullSymbolAccessor.Instance;
        }

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, mmf.Length, fileAccessor);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void GetRawHeaderData(out byte* pointer, out int length)
        {
            pointer = mmf.Address;
            length = (int) mmf.Length;
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(DosHeader);

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
