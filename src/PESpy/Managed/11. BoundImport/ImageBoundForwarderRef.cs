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
    public readonly struct ImageBoundForwarderRef : IValue, IViewable
    {
        public uint TimeDateStamp { get; init; }

        public ushort OffsetModuleName { get; init; }

        public ushort Reserved { get; init; }

        public RVA<string> Name { get; }

        public RawOffset Offset { get; }

        internal const int StructSize =
            sizeof(int) +    //TimeDateStamp
            sizeof(ushort) + //OffsetModuleName
            sizeof(ushort);  //Reserved

        internal ImageBoundForwarderRef(ref FileReader reader, PEFile peFile)
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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_BOUND_FORWARDER_REF), this, ViewKind.ImageBoundForwarderRef);

            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(OffsetModuleName), OffsetModuleName);
            s.WriteField(nameof(Reserved), Reserved);

            if (Name.IsValid)
                writer.WriteGlobal(Name.ActualOffset, Name.Value, Name.Value.Length + 1, ViewKind.String);
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
