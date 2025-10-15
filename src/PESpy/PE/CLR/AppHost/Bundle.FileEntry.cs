using System;
using System.Diagnostics;
using System.Drawing;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        //Analagous to file_entry_t, which wraps file_entry_fixed_t and the relative path
        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public struct FileEntry : IValue, IViewable
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

                    if (data.Offset == 0 && peFile.TryGetValueChunkFromPhysicalOffset((int)header.Offset, out var valueChunk))
                    {
                        switch (Header.Type)
                        {
                            case file_type_t.native_binary:
                            case file_type_t.assembly:
                                Debug.Assert(header.CompressedSize == 0);
                                data = new RawValue<object>((int)header.Offset, new PEFile(RelativePath.ToString(), new MemoryMappedFileHolder(valueChunk.Pointer, header.Size), valueChunk.AbsoluteOffset));
                                break;

                            case file_type_t.deps_json:
                            case file_type_t.runtime_config_json:
                                data = new RawValue<object>((int)header.Offset, valueChunk.PeekUtf8FixedLength(0, (int)header.Size));
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

            public int StructSize => length;

            private readonly MemoryChunk chunk;
            private readonly int length;

            internal FileEntry(in MemoryChunk chunk, bool hasCompressedSize, out int read)
            {
                this.chunk = chunk;

                Header = new FileEntryFixed(chunk, hasCompressedSize);
                read = FileEntryFixed.FixedStructSize + (hasCompressedSize ? sizeof(long) : 0);

                if (Header.CompressedSize != 0)
                    throw new NotImplementedException("Handling having a compressed size is not implemented");

                RelativePath = new BundleEncodedString(chunk.Slice(read), out var relativePathRead);
                read += relativePathRead;

                this.length = read;
                data = default;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                var data = Data;

                //Note that you can have a file entry for the DepsJson and RuntimeConfigJson that point to the same string as in the outer bundle manifest.
                //However, I don't think this is an issue; the merger doesn't seem to get upset about it, I think because we already have dedup logic
                //to handle duplicate unwind infos

                //todo: you can have a file entry for the depsjson/runtimeconfig json that point to the same string as is in the outer bundle
                //but i think its ok cos the merger has logic to handle multiple RuntimeFunction entries pointing to the same UnwindCode anyway

                if (data.Value is PEFile p)
                {
                    //You can have a file that says it's PE32 inside of a PE32Plus single file app.
                    //This causes a problem, because entities want to know the bitness of their parent PEFile.
                    //As such, we must create a brand new PEFileWriter for this nested PEFile to use
                    writer.WriteNestedFile(p, (int)Header.Size);
                }
                else if (data.Value is IViewable v)
                    writer.WriteGlobal(v);
                else if (data.Value is FixedUtf8String s)
                    writer.WriteGlobal(data.Offset, s, s.Length, ViewKind.Value); //todo: more specific view kind?
                else
                    throw new NotImplementedException();
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.file_entry_t, this, ViewKind.BundleFileEntry, StructSize);

            int IViewable.NumChildren => 2;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteInline(Header);
                        break;

                    case 1:
                        structWriter.WriteInline(RelativePath);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
