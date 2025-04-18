using System;
using PESpy.View;

namespace PESpy.PDB
{
    public class StreamTable : IValue, IViewable //Stream 0 (snST) has a copy of the previous stream table. Stream 0 may not be present, so this is a class
    {
        public int NumStreams => chunk.PeekInt32(0);

        public Span<int> StreamSizes => chunk.PeekSpan<int>(4, NumStreams);

        //After the stream sizes there is a PN[][]. However, we need to use each StreamSize[i]
        //to get the number of elements in each sub-array, so we use the SI API to collect this
        //information instead
        public PN[][] StreamBlocks { get; }

        //This is not part of the on-disk data
        public SI[] StreamInfos { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal StreamTable(in MemoryChunk chunk, int pageSize)
        {
            this.chunk = chunk;
            StreamBlocks = default!;
            StreamInfos = default!;

            var numStreams = NumStreams;

            var streamSizes = StreamSizes;

            var streamInfos = new SI[numStreams];
            var streamPages = new PN[numStreams][];

            MemoryChunk streamChunk = chunk.Slice(4 + (NumStreams * 4));

            for (var i = 0; i < numStreams; i++)
            {
                //SI stores the size of a stream (in bytes) and then reads all of the blocks that belong to it.
                //That's what we want to do here anyway, so may as well defer to SI to do the reading work for us
                var si = new SI(streamChunk, streamSizes[i], pageSize);

                streamPages[i] = si.PageList;
                streamInfos[i] = si;

                streamChunk = streamChunk.Slice(si.PageList.Length * 4);
            }

            StreamBlocks = streamPages;
            StreamInfos = streamInfos;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Stream Table", this, ViewKind.StreamTable);

            s.WriteField("NumStreams", NumStreams);
            s.WriteField("StreamSizes", StreamSizes);

            for (var i = 0; i < StreamBlocks.Length; i++)
            {
                var item = StreamBlocks[i];
                s.WriteField($"PageList ({i})", item);
            }
        }
    }
}
