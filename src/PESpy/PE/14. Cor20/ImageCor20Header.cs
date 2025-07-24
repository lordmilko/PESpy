using System;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_COR20_HEADER"/> type that is pointed to by IMAGE_OPTIONAL_HEADER.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR] in managed assemblies.
    /// </summary>
    public class ImageCor20Header : IValue, IViewable
    {
#if PEFAST
        public int ByteCount => chunk.PeekInt32(0);
#else
        public int ByteCount { get; init; }
#endif
#if PEFAST
        public ushort MajorRuntimeVersion => chunk.PeekUInt16(4);
#else
        public ushort MajorRuntimeVersion { get; init; }
#endif
#if PEFAST
        public ushort MinorRuntimeVersion => chunk.PeekUInt16(6);
#else
        public ushort MinorRuntimeVersion { get; init; }
#endif

        //This field should be called MetaData but I really don't like how that looks
#if PEFAST
        public ImageDataDirectory Metadata => new ImageDataDirectory(chunk.Slice(8));
#else
        public ImageDataDirectory<ClrMetadata> Metadata { get; init; }
#endif

#if PEFAST
        public COMIMAGE_FLAGS Flags => (COMIMAGE_FLAGS) chunk.PeekUInt32(16);
#else
        public COMIMAGE_FLAGS Flags { get; init; }
#endif
#if PEFAST
        public int EntryPointTokenOrRVA => chunk.PeekInt32(20);
#else
        public int EntryPointTokenOrRVA { get; init; }
#endif
#if PEFAST
        public ImageDataDirectory Resources => new ImageDataDirectory(chunk.Slice(24));
#else
        public ImageDataDirectory Resources { get; init; }
#endif
#if PEFAST
        public ImageDataDirectory StrongNameSignature => new ImageDataDirectory(chunk.Slice(32));
#else
        public ImageDataDirectory StrongNameSignature { get; init; }
#endif
#if PEFAST
        public ImageDataDirectory CodeManagerTable => new ImageDataDirectory(chunk.Slice(40));
#else
        public ImageDataDirectory CodeManagerTable { get; init; }
#endif
#if PEFAST
        public ImageDataDirectory VTableFixups => new ImageDataDirectory(chunk.Slice(48));
#else
        public ImageDataDirectory VTableFixups { get; init; } //While this member IS meant to be an IMAGE_DATA_DIRECTORY, there are apparently extra members in this directory as well: https://blog.xpnsec.com/the-net-export-portal/
#endif
#if PEFAST
        public ImageDataDirectory ExportAddressTableJumps => new ImageDataDirectory(chunk.Slice(56));
#else
        public ImageDataDirectory ExportAddressTableJumps { get; init; }
#endif
#if PEFAST
        public ImageDataDirectory ManagedNativeHeader => new ImageDataDirectory(chunk.Slice(64));
#else
        public ImageDataDirectory ManagedNativeHeader { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) +   //ByteCount
            sizeof(short) + //MajorRuntimeVersion
            sizeof(short) + //MinorRuntimeVersion
            sizeof(long) +  //Metadata
            sizeof(int) +   //Flags
            sizeof(int) +   //EntryPointTokenOrRelativeVirtualAddress
            sizeof(long) +  //Resources
            sizeof(long) +  //StrongNameSignature
            sizeof(long) +  //CodeManagerTable
            sizeof(long) +  //VTableFixups
            sizeof(long) +  //ExportAddressTableJumps
            sizeof(long);   //ManagedNativeHeader

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageCor20Header(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageCor20Header(IFileReader reader, PEFile peFile)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            ByteCount = reader.ReadInt32();
            MajorRuntimeVersion = reader.ReadUInt16();
            MinorRuntimeVersion = reader.ReadUInt16();

            Metadata = new ImageDataDirectory<ClrMetadata>(reader, peFile, PERegionKind.Cor20Header_Metadata, static (IFileReader r, PEFile p) => new ClrMetadata(r, p));

            Flags = (COMIMAGE_FLAGS) reader.ReadUInt32();
            EntryPointTokenOrRVA = reader.ReadInt32();

            Resources = new ImageDataDirectory(reader);
            StrongNameSignature = new ImageDataDirectory(reader);
            CodeManagerTable = new ImageDataDirectory(reader);
            VTableFixups = new ImageDataDirectory(reader);
            ExportAddressTableJumps = new ImageDataDirectory(reader);
            ManagedNativeHeader = new ImageDataDirectory(reader);

            //If Flags has IL_LIBRARY set, and ManagedNativeHeader is present, it's R2R

            //it's also possible for the r2r header to be listed in exports
            //https://github.com/dotnet/runtime/blob/a38ab4c0bc3780754259be600db1501cc2907a84/docs/design/coreclr/botr/readytorun-format.md#pe-headers-and-cli-headers
        }

        #endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_COR20_HEADER, this, ViewKind.ImageCor20Header, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("cb", ByteCount);
            s.WriteField(nameof(MajorRuntimeVersion), MajorRuntimeVersion);
            s.WriteField(nameof(MinorRuntimeVersion), MinorRuntimeVersion);
            s.WriteStructField("MetaData", Metadata);
            s.WriteField(nameof(Flags), Flags, sizeof(int));
            s.WriteField(nameof(EntryPointTokenOrRVA), EntryPointTokenOrRVA);
            s.WriteStructField(nameof(Resources), Resources);
            s.WriteStructField(nameof(StrongNameSignature), StrongNameSignature);
            s.WriteStructField(nameof(CodeManagerTable), CodeManagerTable);
            s.WriteStructField(nameof(VTableFixups), VTableFixups);
            s.WriteStructField(nameof(ExportAddressTableJumps), ExportAddressTableJumps);
            s.WriteStructField(nameof(ManagedNativeHeader), ManagedNativeHeader);

            return s.ToArray();
        }
    }
}
