using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using PESpy.OMF;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy
{
    /* https://www.azillionmonkeys.com/qed/Omfg.pdf
     * 
     * THEADR: p8
     * 
     * 80 09 00 07 68 65 6C 6C 6F 2E 63 CB
     * 
     * COMENT: p10
     * 
     * 88 07 00 00 00 4D 53 20 43 6E 
     * 88 09 00 00 9F 53 4C 49 42 46 50 10
     * 88 06 00 00 A1 01 43 56 37
     * 
     * MODEND: p27
     * 
     * 8A 07 00 C1 00 01 01 00 00
     * 
     * EXTDEF: p29
     * 
     * 8C 25 00 0A 5F 5F 61 63 72 74 75 73 65 64 00 05 5F 6D 61 69 6E 00 05 5F 70 75 74 73 00 08 5F 5F 63 68 6B 73 74 6B 00 A5
     * 
     * PUBDEF: p31
     * 
     * 90 0C 00 00 01 05 47 41 4D 4D 41 02 00 00 F9
     * 90 0E 00 00 00 00 00 05 41 4C 50 48 41 34 12 00 B1
     * 
     * LINNUM: p34
     * 
     * 94 0F 00 00 01 02 00 00 00 03 00 08 00 04 00 0F 00 3C
     * 
     * LNAMES: p36
     * 
     * 96 25 00 00 04 43 4F 44 45 04 44 41 54 41 05 53 54 41 43 4B 05 5F 44 41 54 41 06 5F 53 54 41 43 4B 05 5F 54 45 58 54 8B
     * 
     * SEGDEF: p38
     * 
     * 98 07 00 28 11 00 07 02 01 1E
     * 98 07 00 48 0F 00 05 03 01 01
     * 
     * GRPDEF: p42
     * 
     * 9A 08 00 06 FF 01 FF 02 FF 03 55
     * 
     * FIXUPP: p44
     * 
     * They say to consult the The MS-DOS Encyclopedia, which is here
     * 
     * https://www.pcjs.org/documents/books/mspl13/msdos/encyclopedia/section2/
     * 
     * LEDATA: p49
     * 
     * A0 13 00 02 00 00 48 65 6C 6C 6F 2C 20 77 6F 72 6C 64 0D 0A 24 A8
     * 
     * LIDATA: p52
     * 
     * A2 1B 00 01 00 00 0A 00 02 00 01 00 00 00 05 41 4C 50 48 41 01 00 00 00 04 42 45 54 41 A9
     * 
     * COMDEF: p54
     * 
     * B0 20 00 04 5F 66 6F 6F 00 62 02 05 5F 66 6F 6F 32 00 62 81 00 80 05 5F 66 6F 6F 33 00 61 81 90 01 01 99 
     */

    //Represents a file encoded using the Object Module Format (OMF).
    //Microsoft C 4.0 era *.obj files use this format, rather than COFF
    public class OMFFile : IFile, IViewable
    {
        //An OMFFile should begin with either a THEADR or an LHEADR and then end with MODEND
        //If it's a lib file, it'll start with LIBHDR. So the file could really start with any of these 3.
        //If it starts with LHEADR, it should only do so if it's inside a library file, which means that
        //at the file level there's only two kinds that a file could possibly start with: THEADR or LIBHDR

        //Lib file format: https://www.azillionmonkeys.com/qed/Omfg.pdf (PDF page 74)
        //Starts with a LIBHDR record. OMF entries are aligned to page sizes

        public static OMFFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new OMFFile(fs.Name, mmf);
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
        public FileKind Kind => FileKind.OMF;

        public long Length => globalBlock.Length;

        public OMFRecord[] Records { get; }

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private ISymbolAccessor symbolAccessor;
        private bool disposed;

        internal unsafe OMFFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, mmf.Length, this);

            try
            {
                using var results = new ValueList<OMFRecord>();

                var end = mmf.Address + mmf.Length;

                var ptr = mmf.Address;

                //https://www.azillionmonkeys.com/qed/Omfg.pdf
                while (ptr < end)
                {
                    var record = new OMFRecord(ptr);

#if DEBUG
                    //Force resolve the symbol to its actual type so that we can trigger any asserts for un-implemented properties
                    ObjectOMFRecordDispatcher.Instance.Dispatch(record);
#endif

                    results.Add(record);
                    ptr += record.RecordLength + sizeof(byte) + sizeof(short);
                }

                Records = results.ToArray();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        ~OMFFile()
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

        public FileView GetViewOld()
        {
            var writer = new ViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => symbolAccessor ??= new OMFFileSymbolAccessor(this);

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
            var dispatcher = new ViewOMFRecordDispatcher(writer);

            var records = Records;

            foreach (var record in records)
            {
                dispatcher.Dispatch(record);

                writer.UnmanagedOffset += record.RecordLength + sizeof(byte) + sizeof(short);
            }
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
