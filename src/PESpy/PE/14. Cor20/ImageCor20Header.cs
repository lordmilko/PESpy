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
        public int ByteCount => chunk.PeekInt32(0);
        public ushort MajorRuntimeVersion => chunk.PeekUInt16(4);
        public ushort MinorRuntimeVersion => chunk.PeekUInt16(6);

        //This field should be called MetaData but I really don't like how that looks
        public ImageDataDirectory Metadata => new ImageDataDirectory(chunk.Slice(8));

        public COMIMAGE_FLAGS Flags => (COMIMAGE_FLAGS) chunk.PeekUInt32(16);
        public int EntryPointTokenOrRVA => chunk.PeekInt32(20);
        public ImageDataDirectory Resources => new ImageDataDirectory(chunk.Slice(24));
        public ImageDataDirectory StrongNameSignature => new ImageDataDirectory(chunk.Slice(32));
        public ImageDataDirectory CodeManagerTable => new ImageDataDirectory(chunk.Slice(40));
        public ImageDataDirectory VTableFixups => new ImageDataDirectory(chunk.Slice(48));
        public ImageDataDirectory ExportAddressTableJumps => new ImageDataDirectory(chunk.Slice(56));
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

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
