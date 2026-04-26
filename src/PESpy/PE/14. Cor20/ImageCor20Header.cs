using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_COR20_HEADER"/> type that is pointed to by IMAGE_OPTIONAL_HEADER.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR] in managed assemblies.
    /// </summary>
    public class ImageCor20Header : IValue, IViewable
    {
        private const int ByteCountOffset = 0;
        private const int MajorRuntimeVersionOffset = 4;
        private const int MinorRuntimeVersionOffset = 6;
        private const int MetadataOffset = 8;
        private const int FlagsOffset = 16;
        private const int EntryPointTokenOrRVAOffset = 20;
        private const int ResourcesOffset = 24;
        private const int StrongNameSignatureOffset = 32;
        private const int CodeManagerTableOffset = 40;
        private const int VTableFixupsOffset = 48;
        private const int ExportAddressTableJumpsOffset = 56;
        private const int ManagedNativeHeaderOffset = 64;

        public int ByteCount => chunk.PeekInt32(ByteCountOffset);
        public ushort MajorRuntimeVersion => chunk.PeekUInt16(MajorRuntimeVersionOffset);
        public ushort MinorRuntimeVersion => chunk.PeekUInt16(MinorRuntimeVersionOffset);

        //This field should be called MetaData but I really don't like how that looks
        public ImageDataDirectory Metadata => new ImageDataDirectory(chunk.Slice(MetadataOffset));

        public COMIMAGE_FLAGS Flags => (COMIMAGE_FLAGS) chunk.PeekUInt32(FlagsOffset);
        public int EntryPointTokenOrRVA => chunk.PeekInt32(EntryPointTokenOrRVAOffset);
        public ImageDataDirectory Resources => new ImageDataDirectory(chunk.Slice(ResourcesOffset));
        public ImageDataDirectory StrongNameSignature => new ImageDataDirectory(chunk.Slice(StrongNameSignatureOffset));
        public ImageDataDirectory CodeManagerTable => new ImageDataDirectory(chunk.Slice(CodeManagerTableOffset));
        public ImageDataDirectory VTableFixups => new ImageDataDirectory(chunk.Slice(VTableFixupsOffset));
        public ImageDataDirectory ExportAddressTableJumps => new ImageDataDirectory(chunk.Slice(ExportAddressTableJumpsOffset));
        public ImageDataDirectory ManagedNativeHeader => new ImageDataDirectory(chunk.Slice(64));

        public int Offset => chunk.AbsoluteOffset;

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

        private readonly MemoryChunk chunk;

        internal ImageCor20Header(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageCor20Header, StructSize);

        int IViewable.NumChildren() => 12;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("cb", ByteCountOffset, ByteCount);
                    break;

                case 1:
                    structWriter.WriteField(nameof(MajorRuntimeVersion), MajorRuntimeVersionOffset, MajorRuntimeVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MinorRuntimeVersion), MinorRuntimeVersionOffset, MinorRuntimeVersion);
                    break;

                case 3:
                    structWriter.WriteStructField("MetaData", Metadata);
                    break;

                case 4:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags, sizeof(int));
                    break;

                case 5:
                    structWriter.WriteField(nameof(EntryPointTokenOrRVA), EntryPointTokenOrRVAOffset, EntryPointTokenOrRVA);
                    break;

                case 6:
                    structWriter.WriteStructField(nameof(Resources), Resources);
                    break;

                case 7:
                    structWriter.WriteStructField(nameof(StrongNameSignature), StrongNameSignature);
                    break;

                case 8:
                    structWriter.WriteStructField(nameof(CodeManagerTable), CodeManagerTable);
                    break;

                case 9:
                    structWriter.WriteStructField(nameof(VTableFixups), VTableFixups);
                    break;

                case 10:
                    structWriter.WriteStructField(nameof(ExportAddressTableJumps), ExportAddressTableJumps);
                    break;

                case 11:
                    structWriter.WriteStructField(nameof(ManagedNativeHeader), ManagedNativeHeader);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
