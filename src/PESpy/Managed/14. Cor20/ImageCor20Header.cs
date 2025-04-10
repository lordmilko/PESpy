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
        public int ByteCount { get; init; }
        public ushort MajorRuntimeVersion { get; init; }
        public ushort MinorRuntimeVersion { get; init; }

        //This field should be called MetaData but I really don't like how that looks
        public ImageDataDirectory<ClrMetadata> Metadata { get; init; }

        public COMIMAGE_FLAGS Flags { get; init; }
        public int EntryPointTokenOrRVA { get; init; }
        public ImageDataDirectory Resources { get; init; }
        public ImageDataDirectory StrongNameSignature { get; init; }
        public ImageDataDirectory CodeManagerTable { get; init; }
        public ImageDataDirectory VTableFixups { get; init; } //While this member IS meant to be an IMAGE_DATA_DIRECTORY, there are apparently extra members in this directory as well: https://blog.xpnsec.com/the-net-export-portal/
        public ImageDataDirectory ExportAddressTableJumps { get; init; }
        public ImageDataDirectory ManagedNativeHeader { get; init; }

        public RawOffset Offset { get; }

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

        #region Metadata

        //This type does not have a well-known native struct declaration
        //EMCA-335 II.24.2
        public class ClrMetadata : IValue, IViewable
        {
            public RawOffset Offset { get; }

            public StorageSignature Signature { get; }

            public StorageHeader Header { get; }

            internal ClrMetadata(IFileReader reader, IMetadataCallback callback)
            {
                Offset = (RawOffset) reader.Position;

                Signature = new StorageSignature(reader);
                Header = new StorageHeader(reader, callback, Signature.Offset);
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                //A region will be created around everything during merging

                writer.WriteGlobal(Signature);
                writer.WriteGlobal(Header);
            }
        }

        #endregion

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_COR20_HEADER), this, ViewKind.ImageCor20Header);

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
        }
    }
}
