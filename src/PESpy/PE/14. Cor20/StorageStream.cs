using System;
using PESpy.Ecma335;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="STORAGESTREAM"/> type that describes a stream header and is included in a CLR <see cref="STORAGEHEADER"/>.
    /// </summary>
    public readonly struct StorageStream : IValue, IViewable
    {
        public const string SchemaStream = "#Schema";
        public const string StringPoolStream = "#Strings";
        public const string BlobPoolStream = "#Blob";
        public const string USBlobPoolStream = "#US";
        public const string GuidPoolStream = "#GUID";
        public const string CompressedModelStream = "#~";
        public const string EnCModelStream = "#-";
        public const string PdbStream = "#Pdb";

        public int iOffset { get; }

        public int Size { get; }

        public string Name { get; }

        public object Data { get; }

        public RawOffset Offset { get; }

        internal StorageStream(IFileReader reader, IMetadataCallback callback, RawOffset metadataRootOffset)
        {
            Offset = (RawOffset) reader.Position;

            iOffset = reader.ReadInt32();
            Size = reader.ReadInt32();

            Name = reader.ReadAnsiNullTerminatedString();

            //Align to next 4 byte boundary. Because it was an ANSI string, there could be 1-3 bytes
            var alignmentTarget = (reader.Position + 3) & ~3;

            while (reader.Position < alignmentTarget)
                reader.ReadByte();

            var startOffset = metadataRootOffset + iOffset;

            var oldPosition = reader.Position;

            reader.Seek(startOffset);

            switch (Name)
            {
                //II.24.2.1
                case CompressedModelStream: //#~
                    Data = new CompressedModelHeap(reader, Size);
                    callback.NotifyCompressedModel((CompressedModelHeap) Data);
                    break;

                //II.24.2.3
                case StringPoolStream: //#Strings
                    Data = new StringHeap(reader, Size);
                    callback.NotifyStringPool((StringHeap) Data);
                    break;

                //II.24.4
                case USBlobPoolStream: //#US
                    Data = new UserStringHeap(reader, Size);
                    callback.NotifyUserStringPool((UserStringHeap) Data);
                    break;

                //II.24.2.4
                case BlobPoolStream: //#Blob
                    Data = new BlobHeap(reader, Size);
                    callback.NotifyBlobPool((BlobHeap) Data);
                    break;

                //II.24.2.5
                case GuidPoolStream: //#GUID
                    Data = new GuidHeap(reader, Size);
                    callback.NotifyGuidPool((GuidHeap) Data);
                    break;

                case PdbStream: //#Pdb
                    Data = new PdbHeap(reader, Size);
                    callback.NotifyPdb((PdbHeap) Data);
                    break;

                case "#!": //I've seen this header in mscorlib.ni but nobody knows how to handle it
                    throw new NotImplementedException("Need to parse #1 stream as bytes");

                default:
                    throw new NotImplementedException($"Don't know how to parse stream '{Name}'");
            }

            reader.Seek(oldPosition);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(STORAGESTREAM), this, ViewKind.StorageStream);

            s.WriteField("iOffset", iOffset);
            s.WriteField("iSize", Size);
            s.WriteAnsiNullTerminatedField("rcName", Name);

            s.Align(4);

            switch (Name)
            {
                case CompressedModelStream:
                {
                    var data = (CompressedModelHeap) Data;

                    using var r = writer.CreateRegion(data.Offset + CompressedModelHeader.FixedStructSize + (data.Header.RowCounts.Length * 4), Name, ViewKind.CompressedModelHeap, global: true);

                    r.WriteValue(data);
                    break;
                }

                case StringPoolStream:
                {
                    var data = (StringHeap) Data;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.StringPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteUTF8NullTerminatedValue(value.Offset, value.Value, ViewKind.Metadata_String);
                    break;
                }

                case USBlobPoolStream:
                {
                    var data = (UserStringHeap) Data;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.USBlobPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteValue(value);

                    break;
                }

                case BlobPoolStream:
                {
                    var data = (BlobHeap) Data;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.BlobPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteValue(value);
                    break;
                }

                case GuidPoolStream:
                {
                    var data = (GuidHeap) Data;

                    using var r = writer.CreateRegion(data.Offset, Name, ViewKind.GuidPoolHeap, global: true);

                    foreach (var value in data)
                        r.WriteValue(value.Offset, value.Value, ViewKind.Metadata_Guid);

                    break;
                }

                case "#!":
                {
                    var data = (ByteBlob) Data;

                    throw new NotImplementedException($"Serializing stream '{Name}' is not implemented");
                }

                default:
                    throw new NotImplementedException($"Don't know how to handle serializing stream '{Name}'");
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
