using System;
using System.ComponentModel;
using System.IO;
using PESpy.Native;
using PESpy.View;
using PESpy.View.Builder;

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

        public int Length => globalBlock.Length;

        private ImageDosHeader dosHeader;

        /// <summary>
        /// Gets the MS-DOS 2.0 compatible <see cref="IMAGE_DOS_HEADER"/>.
        /// </summary>
        public ref readonly ImageDosHeader DosHeader => ref dosHeader;

        public int SizeOfHeaders { get; private set; }

        public int EntryPoint { get; private set; }

        public int StartOfOverlay { get; private set; }

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

        public Span<DosRelocation> Relocations =>
            chunk.PeekSpan<DosRelocation>(dosHeader.FileAddressOfRelocationTable, dosHeader.Relocations);

        private readonly MemoryChunk chunk;

        internal unsafe DOSFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);
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

        public unsafe FileView GetView()
        {
            var writer = new DOSViewWriter(this, CreateByteViewProvider(null));
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        internal unsafe ByteViewProvider CreateByteViewProvider(IViewDisassembler? viewDisassembler)
        {
            viewDisassembler?.Initialize(this);

            return new LocalByteViewProvider(mmf.Address, (int) mmf.Length, viewDisassembler);
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

            writer.WriteGlobal((IViewable?) CodeViewData);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren => throw new NotSupportedException();

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

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
