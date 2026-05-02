using System;
using PESpy.View;

namespace PESpy
{
    //_RTTIBaseClassArray
    public readonly struct RTTIBaseClassArray : IValue, IViewable
    {
        public RVA<RTTIBaseClassDescriptor[]> arrayOfBaseClassDescriptors { get; }

        public int Offset { get; }

        internal const int StructSize = sizeof(int);

        internal RTTIBaseClassArray(in MemoryChunk chunk, int numBaseClasses)
        {
            Offset = chunk.AbsoluteOffset;

            //All this type does is store its base classes, so just eagerly read them

            var rva = chunk.PeekInt32(0);

            if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
            {
                var results = new RTTIBaseClassDescriptor[numBaseClasses];

                for (var i = 0; i < results.Length; i++)
                    results[i] = new RTTIBaseClassDescriptor(valueChunk.Slice(i * RTTIBaseClassDescriptor.StructSize));

                arrayOfBaseClassDescriptors = new RVA<RTTIBaseClassDescriptor[]>(rva, valueChunk.AbsoluteOffset, results);
            }
            else
                arrayOfBaseClassDescriptors = new RVA<RTTIBaseClassDescriptor[]>(rva);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteUniqueRVAField(arrayOfBaseClassDescriptors, Offset, 0);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.RTTIBaseClassArray, StructSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteRVAField(nameof(arrayOfBaseClassDescriptors), 0, arrayOfBaseClassDescriptors);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
