using System;
using System.IO;
using PESpy.SYM;
using PESpy.View;

namespace PESpy
{
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
        public FileKind Kind => FileKind.LE;

        public int Length => globalBlock.Length;

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;

        private bool disposed;

        internal unsafe SYMFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

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

            Footer = new endmap_s(chunk.Slice(chunk.Remaining - endmap_s.StructSize));

            //Apparently there's two versions of sym files: one stores offsets in bytes (MapSym 2.08 - 3.00)
            //and one that stores offsets in paragraphs (3.10). https://win-archaeology.fandom.com/wiki/.SYM_Format

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
            }

            Segments = segments;
        }

        public FileView GetView()
        {
            throw new NotImplementedException();
        }

        public ISymbolAccessor GetSymbolAccessor(ILocatorProgress? progress = null)
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

            globalBlock.Dispose();
            mmf.Dispose();

            disposed = true;
        }
    }
}
