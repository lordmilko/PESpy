using System;
using System.IO;
using System.Threading;
using PESpy.SYM;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy
{
    //The Windows 1.01 and 1.03 SDK has mapsym 3.10. Can't find mapsym 2.08 to 3.0

    public class SYMFile : IFile
    {
        public static SYMFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new SYMFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        public mapdef_s Header { get; private set; }

        public segdef_s[] Segments { get; private set; }

        public endmap_s Footer { get; private set; }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.SYM;

        public long Length => globalBlock.Length;

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;

        private ISymbolAccessor symbolAccessor; //Separate type so you can dispose the accessor without accidentally disposing the main file

        private bool disposed;

        internal unsafe SYMFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, mmf.Length, this);

            try
            {
                ReadSymHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void ReadSymHeaders()
        {
            var chunk = new MemoryChunk(globalBlock, 0);
            Header = new mapdef_s(chunk);

            Footer = new endmap_s(chunk.Slice((int) chunk.Remaining - endmap_s.StructSize));

            //Apparently there's two versions of sym files: one stores offsets in bytes (MapSym 2.08 - 3.00)
            //and one that stores offsets in paragraphs (3.10). https://win-archaeology.fandom.com/wiki/.SYM_Format
            //Note that the document on this page is not completely accurate; they think that the first field is the length
            //of the file itself; not true - md_spmap * 16 + 4 = the size of the file. But the main point here is that you apparently
            //have multiple maps compressed within a single SYM file

            //Note: if we add support for the earlier version, need to update Detector as well as Detector is using this heuristic
            //to try and detect valid files
            if (Footer.em_ver < 3 || (Footer.em_ver == 3 && Footer.em_rel < 10))
                throw new NotSupportedException($"SYM files produced by mapsym {Footer.em_ver}.{Footer.em_rel} are not currently supported");

            //A paragraph is 16 bytes
            var segmentOffset = Header.md_spseg * 16;

            var segments = new segdef_s[Header.md_cseg];

            for (var i = 0; i < segments.Length; i++)
            {
                var seg = new segdef_s(segmentOffset, chunk);

                segments[i] = seg;

                segmentOffset = seg.gd_spsegnext * 16;
            }

            Segments = segments;
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
            CancellationToken cancellationToken = default) => symbolAccessor ??= new SYMFileSymbolAccessor(this);

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, mmf.Length, fileAccessor);

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
