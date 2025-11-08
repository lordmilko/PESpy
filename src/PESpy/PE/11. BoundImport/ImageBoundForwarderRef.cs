using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_BOUND_FORWARDER_REF"/> structure.
    /// </summary>
    public struct ImageBoundForwarderRef : IValue, IViewable
    {
        private const int TimeDateStampOffset = 0;
        internal const int OffsetModuleNameOffset = 4;
        private const int ReservedOffset = 6;

        public Timestamp TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        public ushort OffsetModuleName => chunk.PeekUInt16(OffsetModuleNameOffset);

        public ushort Reserved => chunk.PeekUInt16(ReservedOffset);

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
            writer.WriteUniqueRVAAnsiNullTerminatedField(Name, ViewKind.ImageBoundImportName, Offset, OffsetModuleNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BOUND_FORWARDER_REF, this, ViewKind.ImageBoundForwarderRef, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 1:
                    structWriter.WriteField(nameof(OffsetModuleName), OffsetModuleNameOffset, OffsetModuleName);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Reserved), ReservedOffset, Reserved);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
