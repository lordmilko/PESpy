using System;
using System.IO;
using System.Threading;
using PESpy.OMF;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy
{
    public class OMFLIBFile : IFile
    {
        public static OMFLIBFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new OMFLIBFile(fs.Name, mmf);
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
        public FileKind Kind => FileKind.OMFLIB;

        public long Length => globalBlock.Length;

        public LIBHDR LibHdr { get; }

        public ObjectModule[] Modules { get; }

        public DICHDR DicHdr { get; }

        public DicRecord[] Dictionary { get; }

        public LIBEXD? ExtDic { get; }

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private bool disposed;

        internal unsafe OMFLIBFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, mmf.Length, this);

            try
            {
                var libHdr = new LIBHDR(mmf.Address);

                if (libHdr.RecordType != OMFRecordType.LIBHDR)
                    throw new InvalidOperationException($"Expected the header to contain a record of type '{nameof(OMFRecordType.LIBHDR)}'. Actual type: '{libHdr.RecordType}'");

                LibHdr = libHdr;

                var pageSize = libHdr.PageSize;

                var ptr = mmf.Address + pageSize;

                var end = mmf.Address + mmf.Length;

                using var modules = new ValueList<ObjectModule>();

                using var results = new ValueList<OMFRecord>();

                var offset = 0;

                //Contrary to popular belief, in Microsoft LIB files each object file may begin with THEADR and not LHEADR
                while (ptr < end)
                {
                    var record = new OMFRecord(ptr + offset);
                    offset += record.RecordLength + sizeof(byte) + sizeof(short); //Note: ptr begins 1 page size in, so this won't match the dictionary offset when we hit the DICHDR

                    if (record.RecordType == OMFRecordType.DICHDR)
                    {
                        Dictionary = ParseDictionary(ref offset);
                        break;
                    }

    #if DEBUG
                    //Force resolve the symbol to its actual type so that we can trigger any asserts for un-implemented properties
                    ObjectOMFRecordDispatcher.Instance.Dispatch(record);
    #endif

                    results.Add(record);

                    if (record.RecordType == OMFRecordType.MODEND)
                    {
                        modules.Add(new ObjectModule(results.ToArray()));
                        results.Clear();

                        //Align to the next page interval
                        offset = (offset + (pageSize - 1)) & (~(pageSize - 1));
                    }
                }

                Modules = modules.ToArray();

                ptr += offset;

                if (ptr < end)
                {
                    var omfRecord = new OMFRecord(ptr);

                    if (omfRecord.RecordType == OMFRecordType.LIBEXD)
                    {
#if DEBUG
                        //Force resolve the symbol to its actual type so that we can trigger any asserts for un-implemented properties
                        ObjectOMFRecordDispatcher.Instance.Dispatch(omfRecord);
#endif

                        ExtDic = (LIBEXD) omfRecord;
                    }
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private unsafe DicRecord[] ParseDictionary(ref int offset)
        {
            using var records = new ValueList<DicRecord>();

            //Following the DICHDR is the actual dictionary. The dictionary is comprised of LIBHDR.DictionaryBlockCount blocks, each 512 bytes
            //in length. Each block stores 37 buckets
            for (var i = 0; i < LibHdr.DictionaryBlockCount; i++)
            {
                var chunk = new MemoryChunk(globalBlock, LibHdr.DictionaryOffset + (i * 512));

                var buckets = chunk.PeekNativeSpan<byte>(0, 37);
                var flag = chunk.PeekByte(37);

                for (var j = 0; j < buckets.Length; j++)
                {
                    var record = new DicRecord(chunk.Pointer + (buckets[j] * 2));

                    records.Add(record);
                }

                offset += 512;
            }

            return records.ToArray();
        }

        ~OMFLIBFile()
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
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

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

        //Type is made up
        public readonly struct ObjectModule
        {
            public readonly OMFRecord[] Records;

            public ObjectModule(OMFRecord[] records)
            {
                Records = records;
            }

            public override string ToString()
            {
                //The first record should always be a THEADR or LHEADR (these both have the same shape)
                return ((THEADR) Records[0]).ToString();
            }
        }

        //Type is made up
        public readonly struct DicRecord
        {
            public readonly FixedAnsiString Name;

            //The number of the page that contains the module pointed to by this record. Since the header occupies page 0,
            //the first module is in page 1...which is at index 0 in our Modules array
            public readonly ushort ModuleNumber;

            public readonly byte? AlignByte;

            public unsafe DicRecord(byte* value)
            {
                var strLen = *value;

                Name = new FixedAnsiString(value + sizeof(byte), strLen);
                ModuleNumber = *(ushort*) (value + sizeof(byte) + strLen);

                //Each dictionary record must be aligned on a word boundary. Since the string length occupies 1 byte,
                //if the length of the string is not also odd then we need to have an align byte at the end
                if (strLen % 2 == 0)
                    AlignByte = *(value + sizeof(byte) + strLen + sizeof(ushort));
                else
                    AlignByte = null;
            }

            public override string ToString()
            {
                return Name.ToString();
            }
        }
    }
}
