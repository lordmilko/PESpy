using System;
using PESpy.View;

namespace PESpy
{
    //_RTTIBaseClassArray
    public readonly struct RTTIBaseClassArray : IValue, IViewable
    {
        public RVA<RTTIBaseClassDescriptor>[] arrayOfBaseClassDescriptors { get; }

        public long Offset { get; }

        private readonly NativeSpan<int> rvas;

        internal int StructSize => rvas.Length * sizeof(int);

        internal RTTIBaseClassArray(in MemoryChunk chunk, int numBaseClasses)
        {
            Offset = chunk.AbsoluteOffset;

            //All this type does is store its base classes, so just eagerly read them

            var rvas = chunk.PeekNativeSpan<int>(0, numBaseClasses);
            this.rvas = rvas;

            var results = new RVA<RTTIBaseClassDescriptor>[numBaseClasses];

            var peFile = chunk.PEFile();

            for (var i = 0; i < results.Length; i++)
            {
                var rva = rvas[i];

                if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    results[i] = new RVA<RTTIBaseClassDescriptor>(rva, valueChunk.AbsoluteOffset, new RTTIBaseClassDescriptor(valueChunk));
                else
                    results[i] = new RVA<RTTIBaseClassDescriptor>(rva);
            }

            arrayOfBaseClassDescriptors = results;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            for (var i = 0; i < arrayOfBaseClassDescriptors.Length; i++)
                writer.WriteUniqueRVAField(arrayOfBaseClassDescriptors[i], offset, i * sizeof(int));
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.RTTIBaseClassArray, StructSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(arrayOfBaseClassDescriptors), 0, rvas);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
