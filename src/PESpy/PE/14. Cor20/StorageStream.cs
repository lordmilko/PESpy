using System;
using System.Diagnostics;
using PESpy.Ecma335;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="STORAGESTREAM"/> type that describes a stream header and is included in a CLR <see cref="STORAGEHEADER"/>.
    /// </summary>
    public struct StorageStream : IValue, IViewable
    {
        //Names based on mdcommon.h
        public const string SchemaStream = "#Schema";
        public const string StringPoolStream = "#Strings";
        public const string BlobPoolStream = "#Blob";
        public const string USBlobPoolStream = "#US";
        public const string GuidPoolStream = "#GUID";
        public const string CompressedModelStream = "#~";
        public const string EnCModelStream = "#-";
        public const string MinimalMDStream = "#JTD"; //"Minimal Delta"
        public const string PdbStream = "#Pdb";
        public const string HotModelStream = "#!"; //mdcommon.h in older versions of coreclr has this

        public int iOffset => chunk.PeekInt32(0);

        public int Size => chunk.PeekInt32(4);

        public string Name { get; }

        private object? data;

        public object? Data
        {
            get
            {
                if (data == null)
                {
                    var peFile = chunk.PEFile();

                    int metadataRootOffset;

                    if (peFile != null!)
                        metadataRootOffset = peFile.EcmaMetadata!.Offset;
                    else
                        metadataRootOffset = chunk.PortablePDBFile().EcmaMetadata.Offset;

                    var offset = metadataRootOffset + iOffset;

                    var valueChunk = new MemoryChunk(chunk.block, offset - chunk.block.RemoteStartOffset);

                    switch (Name)
                    {
                        //II.24.2.1
                        case CompressedModelStream: //#~
                            data = new CompressedModelHeap(valueChunk, Size);
                            break;

                        //II.24.2.3
                        case StringPoolStream: //#Strings
                            data = new StringHeap(valueChunk, Size);
                            break;

                        //II.24.4
                        case USBlobPoolStream: //#US
                            data = new UserStringHeap(valueChunk, Size);
                            break;

                        //II.24.2.4
                        case BlobPoolStream: //#Blob
                            data = new BlobHeap(valueChunk, Size);
                            break;

                        //II.24.2.5
                        case GuidPoolStream: //#GUID
                            data = new GuidHeap(valueChunk, Size);
                            break;

                        case PdbStream: //#Pdb
                            data = new PdbHeap(valueChunk, Size);
                            break;

                        case HotModelStream:
                            //Hot Model Stream is specific to NGEN images. Older versions of coreclr explicitly state that the Hot Model Stream
                            //should be forbidden in non-NGEN images. liteweightstgdb.cpp shows how to handle this. We have a tracking issue on GitHub
                            //for implementing this
                            data = null;
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to parse stream '{Name}'");
                    }
                }

                return data;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //iOffset
            sizeof(int); //Size

        internal int StructSize =>
            (FixedStructSize +
            Name.Length + 1 + 3) & ~3; //32-bit aligned

        private readonly MemoryChunk chunk;

        internal StorageStream(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            //We want to be able to switch on the name so we need to allocate
            Name = chunk.PeekUtf8NullTerminatedString(8).ToString();
            data = null;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            switch (Name)
            {
                case CompressedModelStream:
                {
                    var data = (CompressedModelHeap) Data!;

                    using var r = writer.CreateRegion(
                        data.Offset + CompressedModelHeader.FixedStructSize + (data.Header.RowCounts.Length * 4),
                        Name,
                        ViewKind.CompressedModelHeap,
                        global: true
                    );

                    r.WriteValue(data);
                    break;
                }

                case StringPoolStream:
                {
                    var data = (StringHeap) Data!;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.StringPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteUTF8NullTerminatedValue(value.Offset, value.Value, ViewKind.Metadata_String);
                    break;
                }

                case USBlobPoolStream:
                {
                    var data = (UserStringHeap) Data!;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.USBlobPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteValue(value);

                    break;
                }

                case BlobPoolStream:
                {
                    var data = (BlobHeap) Data!;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.BlobPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteValue(value);
                    break;
                }

                case GuidPoolStream:
                {
                    var data = (GuidHeap) Data!;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.GuidPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteValue(value.Offset, value.Value, ViewKind.Metadata_Guid);

                    break;
                }

                case HotModelStream:
                {
                    Debug.Assert(data == null); //When we implement support for the Hot Model Stream, we need to writeit
                    break;
                }

                default:
                    throw new NotImplementedException($"Don't know how to handle serializing stream '{Name}'");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.STORAGESTREAM, this, ViewKind.StorageStream, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("iOffset", iOffset);
            s.WriteField("iSize", Size);
            s.WriteAnsiNullTerminatedField("rcName", Name);

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
