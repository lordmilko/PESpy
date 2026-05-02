using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using PESpy.View;

namespace PESpy
{
    public class ResourceFile : IFile
    {
        internal const uint MagicNumber = 0xBEEFCACE;

        //Standalone resource files aren't actually in the resource format; they're PE files with nothing much else but the Cor20 resource directory
        private static ResourceFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new ResourceFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        //Assembly.GetManifestResourceStream()
        //The stream passed in must be at the position of the resource file
        public unsafe static ResourceFile FromStream(UnmanagedMemoryStream stream)
        {
            var mmf = new MemoryMappedFileHolder(stream.PositionPointer, stream.Length);

            try
            {
                return new ResourceFile(null, mmf, stream);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        public unsafe static ResourceFile FromAssembly(Assembly assembly, string name)
        {
            var stream = (UnmanagedMemoryStream) assembly.GetManifestResourceStream(name);

            var mmf = new MemoryMappedFileHolder(stream.PositionPointer, stream.Length);

            try
            {
                return new ResourceFile(name, mmf, stream);
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
        public FileKind Kind => FileKind.LE;

        public int Length => globalBlock.Length;

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private readonly UnmanagedMemoryStream? stream;

        private bool disposed;

        internal unsafe ResourceFile(string fileName, in MemoryMappedFileHolder mmf, UnmanagedMemoryStream? stream = null, string name = null)
        {
            this.mmf = mmf;
            this.stream = stream;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            try
            {
                ReadHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void ReadHeaders()
        {
            var chunk = new MemoryChunk(globalBlock, 0);

            //Resource Manager Header

            var sig = chunk.PeekUInt32(0);

            if (sig != MagicNumber)
                throw new InvalidOperationException("File is not a resource file: magic number was not correct");

            var version = chunk.PeekInt32(4);

            var numBytesToSkip = chunk.PeekInt32(8);

            //In V2, .NET uses the bytes to skip to skip past the end of the header. But we want to read everything.
            //I think V2 migth skip because it might be implied you always use DeserializingResourceReader?

            var read = 12;

            var readerTypeName = chunk.Peek7BitEncodedUtf8(read, out var bytesRead);
            read += bytesRead;

            var resourceSetName = chunk.Peek7BitEncodedUtf8(read, out bytesRead);
            read += bytesRead;

            //+12 because that's how many bytes there were prior to the strings
            Debug.Assert(numBytesToSkip + 12 == read);

            //Runtime Resource Header
            var resourceHeaderVersion = chunk.PeekInt32(read);

            var numResources = chunk.PeekInt32(read + 4);
            var numTypes = chunk.PeekInt32(read + 8);

            read += 12;

            var types = new FixedUtf8String[numTypes];

            for (var i = 0; i < types.Length; i++)
            {
                var typeName = chunk.Peek7BitEncodedUtf8(read, out bytesRead);
                read += bytesRead;

                types[i] = typeName;
            }

            //Position should be 8 byte aligned
            var aligned = (read + 7) & ~7;

            //These bytes are not 0, that's expected
            var pad = chunk.PeekNativeSpan<byte>(read, aligned - read);
            read = aligned;

            //The hashes are sorted by their interpretations as _signed_ integers, which means
            //negative hashes will be first
            var nameHashes = chunk.PeekNativeSpan<int>(read, numResources);
            read += numResources * sizeof(int);

            var namePositions = chunk.PeekNativeSpan<int>(read, numResources);
            read += numResources * sizeof(int);

            var dataSectionOffset = chunk.PeekInt32(read);
            read += sizeof(int);

            //Runtime Resource Reader Name Section

            var nameChunk = chunk.Slice(read);

            var entries = new (FixedUtf16String name, int offset)[numResources];

            for (var i = 0; i < entries.Length; i++)
            {
                var offset = namePositions[i];

                var name = nameChunk.Peek7BitEncodedUtf16(offset, out bytesRead);
                var off = nameChunk.PeekInt32(offset + bytesRead);

                entries[i] = (name, off);
            }

            var dataChunk = chunk.Slice(dataSectionOffset);

            if (resourceHeaderVersion == 1)
            {
                //The data starts with a type index
                throw new NotImplementedException($"Don't know how to handle resource header version '{resourceHeaderVersion}'");
            }
            else
            {
                //The data starts with a ResourceTypeCode

                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];

                    var data = dataChunk.Slice(entry.offset);

                    var typeCode = (ResourceTypeCode) data.Peek7BitEncodedInt32(0, out bytesRead);

                    read = bytesRead;

                    if (typeCode >= ResourceTypeCode.StartOfUserTypes)
                    {
                        var typeIndex = typeCode - ResourceTypeCode.StartOfUserTypes;
                        var format = (SerializationFormat) data.Peek7BitEncodedInt32(read, out bytesRead);
                        read += bytesRead;

                        if (format != SerializationFormat.ActivatorStream)
                            throw new NotImplementedException($"Don't know how to handle format '{format}'");

                        var length = data.Peek7BitEncodedInt32(read, out bytesRead);
                        read += bytesRead;

                        var value = data.Slice(read);

                        //Note: we aren't actually doing anything with the data we're reading
                    }
                    else
                    {
                        throw new NotImplementedException($"Don't know how to handle type code '{typeCode}'");
                    }
                }
            }

            throw new NotImplementedException("Reading resource files properly is not implemented");
        }

        public FileView GetView(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
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
            stream?.Dispose();

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
