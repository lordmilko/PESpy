using System;
using System.Diagnostics;

namespace PESpy
{
    public static partial class Bundle
    {
        //Analagous to file_entry_t, which wraps file_entry_fixed_t and the relative path
        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public struct FileEntry
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"[{Header.Type}] {RelativePath.Value}";

            public FileEntryFixed Header { get; }

            public BundleEncodedString RelativePath { get; }

            private RawValue<object> data;

            public unsafe RawValue<object> Data
            {
                get
                {
                    var peFile = chunk.PEFile();

                    var header = Header;

                    if (data.Offset == 0 && peFile.TryGetValueChunkFromPhysicalOffset((int) header.Offset, out var valueChunk))
                    {
                        switch (Header.Type)
                        {
                            case file_type_t.native_binary:
                            case file_type_t.assembly:
                                Debug.Assert(header.CompressedSize == 0);
                                data = new RawValue<object>((int) header.Offset, new PEFile(RelativePath.ToString(), new MemoryMappedFileHolder(valueChunk.Pointer, header.Size)));
                                break;

                            case file_type_t.deps_json:
                            case file_type_t.runtime_config_json:
                                data = new RawValue<object>((int) header.Offset, valueChunk.PeekUtf8FixedLength(0, (int) header.Size));
                                break;

                            case file_type_t.symbols:
                                throw new NotImplementedException($"Don't know how to handle file entry of type '{header.Type}'");

                            default:
                                data = default;
                                Debug.Assert(false);
                                break;
                        }
                    }

                    return data;
                }
            }

            public int Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                FileEntryFixed.FixedStructSize; //Header

            private readonly MemoryChunk chunk;

            internal FileEntry(in MemoryChunk chunk, bool hasCompressedSize, out int read)
            {
                this.chunk = chunk;

                Header = new FileEntryFixed(chunk, hasCompressedSize);
                read = FileEntryFixed.FixedStructSize + (hasCompressedSize ? sizeof(long) : 0);

                if (Header.CompressedSize != 0)
                    throw new NotImplementedException("Handling having a compressed size is not implemented");

                RelativePath = new BundleEncodedString(chunk.Slice(read), out var relativePathRead);
                read += relativePathRead;

                data = default;
            }
        }
    }    
}
