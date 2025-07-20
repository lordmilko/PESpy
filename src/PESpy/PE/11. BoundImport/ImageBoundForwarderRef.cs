using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_BOUND_FORWARDER_REF"/> structure.
    /// </summary>
    public struct ImageBoundForwarderRef : IValue, IViewable
    {
#if PEFAST
        public uint TimeDateStamp => chunk.PeekUInt32(0);
#else
        public uint TimeDateStamp { get; init; }
#endif

#if PEFAST
        public ushort OffsetModuleName => chunk.PeekUInt16(4);
#else
        public ushort OffsetModuleName { get; init; }
#endif

#if PEFAST
        public ushort Reserved => chunk.PeekUInt16(6);
#else
        public ushort Reserved { get; init; }
#endif

#if PEFAST
        private RVA<AnsiString> name;

        public RVA<AnsiString> Name
        {
            get
            {
                if (name.ListedOffset == 0)
                {
                    var peFile = chunk.PEFile();

                    var rva = peFile.OptionalHeader.BoundImportTableDirectory.VirtualAddress + OffsetModuleName;

                    if (peFile.TryGetValueChunkFromPhysicalOffset(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        name = new RVA<AnsiString>(rva, valueChunk.AbsoluteOffset, str);
                    }
                    else
                        name = new RVA<AnsiString>(rva);
                }

                return name;
            }
        }
#else
        public RVA<string> Name { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) +    //TimeDateStamp
            sizeof(ushort) + //OffsetModuleName
            sizeof(ushort);  //Reserved

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageBoundForwarderRef(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            name = default;
        }
#else
        internal ImageBoundForwarderRef(IFileReader reader, PEFile peFile)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            TimeDateStamp = reader.ReadUInt32();
            OffsetModuleName = reader.ReadUInt16();
            Reserved = reader.ReadUInt16();

            var nameRVA = (RawOffset) peFile.OptionalHeader.BoundImportTableDirectory.VirtualAddress + OffsetModuleName;
            reader.Seek(nameRVA);
            var str = reader.ReadAnsiNullTerminatedString();
            Name = new RVA<string>((RVA) OffsetModuleName, nameRVA, str);
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            if (Name.IsValid)
                writer.WriteGlobal(Name.ActualOffset, Name.Value, Name.Value.Length + 1, ViewKind.ImageBoundForwarderRef_Name);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(IMAGE_BOUND_FORWARDER_REF), this, ViewKind.ImageBoundForwarderRef, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(OffsetModuleName), OffsetModuleName);
            s.WriteField(nameof(Reserved), Reserved);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
