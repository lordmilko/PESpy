using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_BOUND_FORWARDER_REF"/> structure.
    /// </summary>
    public struct ImageBoundForwarderRef : IValue, IViewable
    {
        internal const int OffsetModuleNameOffset = 4;

        public Timestamp TimeDateStamp => chunk.PeekUInt32(0);

        public ushort OffsetModuleName => chunk.PeekUInt16(OffsetModuleNameOffset);

        public ushort Reserved => chunk.PeekUInt16(6);

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

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) +    //TimeDateStamp
            sizeof(ushort) + //OffsetModuleName
            sizeof(ushort);  //Reserved

        private readonly MemoryChunk chunk;

        internal ImageBoundForwarderRef(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            name = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //The ImageBoundImportDescriptor can share the same name
            writer.WriteUniqueRVAAnsiNullTerminatedField(Name, ViewKind.ImageBoundImportName, OffsetModuleNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BOUND_FORWARDER_REF, this, ViewKind.ImageBoundForwarderRef, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(OffsetModuleName), OffsetModuleName);
            s.WriteField(nameof(Reserved), Reserved);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
