using System;
using PESpy.View;

namespace PESpy
{
    //_RTTIBaseClassDescriptor
    public readonly struct RTTIBaseClassDescriptor : IValue, IViewable
    {
        private const int pTypeDescriptorOffset = 0;
        private const int numContainedBasesOffset = 4;
        private const int whereOffset = 8;
        private const int attributesOffset = 8 + PMD.StructSize;
        private const int pClassDescriptorOffset = 12 + PMD.StructSize;

        public RVA<TypeDescriptor> pTypeDescriptor
        {
            get
            {
                var rva = chunk.PeekInt32(pTypeDescriptorOffset); //TypeDescriptor

                if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    return new RVA<TypeDescriptor>(rva, valueChunk.AbsoluteOffset, new TypeDescriptor(valueChunk));

                return new RVA<TypeDescriptor>(rva);
            }
        }

        public int numContainedBases => chunk.PeekInt32(numContainedBasesOffset);

        public PMD where => chunk.PeekUnmanaged<PMD>(whereOffset);

        public BCD attributes => (BCD) chunk.PeekUInt32(attributesOffset);

        public RVA<RTTIClassHierarchyDescriptor> pClassDescriptor
        {
            get
            {
                var rva = chunk.PeekInt32(pClassDescriptorOffset); //_RTTIClassHierarchyDescriptor

                if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    return new RVA<RTTIClassHierarchyDescriptor>(rva, valueChunk.AbsoluteOffset, new RTTIClassHierarchyDescriptor(valueChunk));

                return new RVA<RTTIClassHierarchyDescriptor>(rva);
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //pTypeDescriptor
            sizeof(int) + //numContainedBases
            PMD.StructSize + //where
            sizeof(int) + //attributes
            sizeof(int); //pClassDescriptor

        private readonly MemoryChunk chunk;

        internal RTTIBaseClassDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(pTypeDescriptor, Offset, fieldOffset: pTypeDescriptorOffset);

            //RTTIClassHierarchyDescriptor -> RTTIBaseClassArray -> RTTIBaseClassDescriptor -> RTTIClassHierarchyDescriptor again.
            //This will cause us to get into an infinite loop; as such we need to make sure we only write hierarchy descriptor once
            writer.WriteUniqueRVAField(pClassDescriptor, Offset, fieldOffset: pClassDescriptorOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.RTTIBaseClassDescriptor, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteRVAField(nameof(pTypeDescriptor), pTypeDescriptorOffset, pTypeDescriptor);
                    break;

                case 1:
                    structWriter.WriteField(nameof(numContainedBases), numContainedBasesOffset, numContainedBases);
                    break;

                case 2:
                    structWriter.WriteStructField(nameof(where), whereOffset, where);
                    break;

                case 3:
                    structWriter.WriteField(nameof(attributes), attributesOffset, attributes, sizeof(int));
                    break;

                case 4:
                    structWriter.WriteRVAField(nameof(pClassDescriptor), pClassDescriptorOffset, pClassDescriptor);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return pTypeDescriptor.ToString();
        }
    }
}
