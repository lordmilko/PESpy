using System;
using System.Diagnostics;

namespace PESpy
{
    public static partial class Bundle
    {
        //Analagous to file_entry_t, which wraps file_entry_fixed_t and the relative path
        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct FileEntry
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"[{Header.Type}] {RelativePath.Value}";

            public FileEntryFixed Header { get; }

            public BundleEncodedString RelativePath { get; }

            public RawValue<object> Data { get; }

#if !PEFAST
            internal FileEntry(IFileReader reader, int majorVersion)
            {
                Header = new FileEntryFixed(reader, majorVersion);
                RelativePath = new BundleEncodedString(reader);

                if (Header.CompressedSize != 0)
                    throw new System.NotImplementedException();

                var oldPosition = reader.Position;

                reader.Seek(Header.Offset);

                switch (Header.Type)
                {
                    case file_type_t.native_binary:
                    case file_type_t.assembly:
                        Data = new RawValue<object>((int) Header.Offset, new PEFile(reader));
                        break;

                    case file_type_t.deps_json:
                    case file_type_t.runtime_config_json:
                        Data = new RawValue<object>((int) Header.Offset, reader.ReadUTF8String((int) Header.Size));
                        break;

                    case file_type_t.symbols:
                        throw new NotImplementedException($"Don't know how to handle file entry of type '{Header.Type}'");

                    default:
                        Data = default;
                        Debug.Assert(false);
                        break;
                }

                reader.Seek(oldPosition);
            }
#endif
        }
    }    
}
