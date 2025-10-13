using System;
using System.IO;
using PESpy.OMF;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy
{
    //Represents a file encoded using the Object Module Format (OMF).
    //Microsoft C 4.0 era *.obj files use this format, rather than COFF
    public class OMFFile : IFile
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

        public int Length => globalBlock.Length;

        public OMFRecord[] Records { get; }

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private bool disposed;

        internal unsafe OMFFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            try
            {
                using var results = new PooledList<OMFRecord>();

                var end = mmf.Address + mmf.Length;

                var ptr = mmf.Address;

                //https://www.azillionmonkeys.com/qed/Omfg.pdf
                while (ptr < end)
                {
                    var record = new OMFRecord(ptr);

                    var recordType = (byte) record.RecordType;

                    if (recordType < 0xCA && (recordType & 1) == 1)
                        throw new InvalidOperationException("32-bit record types are not yet supported");

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

        public FileView GetView()
        {
            throw new NotImplementedException();
        }

        internal unsafe ByteViewProvider CreateByteViewProvider() => new LocalByteViewProvider(mmf.Address, (int) mmf.Length);

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
