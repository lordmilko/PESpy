namespace PESpy
{
    //_RTTIBaseClassArray
    public readonly struct RTTIBaseClassArray
    {
        public RVA<RTTIBaseClassDescriptor[]> arrayOfBaseClassDescriptors { get; }

        public int Offset { get; }

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
    }
}
