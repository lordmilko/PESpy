using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using PESpy.View;

namespace PESpy.PDB
{
    public readonly partial struct BigMsfHdr
    {
        //The format of the Stream Table in PDB v2 is different, so I'm encapsulating this in BigMsfHdr to signify that it's unique to BigMsfHdr

        /// <summary>
        /// Describes the on-disk representation of the PDB Stream Table, which describes the number, size and locations
        /// of each of the streams contained in the PDB file.<para/>
        /// This type does not have a well-known native struct declaration. Note that the PDB1 StrmTbl type represents
        /// the in-memory stream table only, after merging these values together.
        /// </summary>
        public class StreamTable : IStreamTable, IValue, IViewable //Stream 0 (snST) has a copy of the previous stream table. Stream 0 may not be present, so this is a class
        {
            private const int NumStreamsOffset = 0;
            private const int StreamSizesOffset = 4;
            private const int StreamPagesOffset = 8;

            //snMac
            public int NumStreams => chunk.PeekInt32(NumStreamsOffset);

            //Eagerly read as a list so that we can append to it. We then need to manually serialize the stream table whenever we make changes
            public NativeSpan<int> StreamSizes => chunk.PeekNativeSpan<int>(StreamSizesOffset, NumStreams);

            //After the stream sizes there is a PN[][]. However, we need to use each StreamSize[i]
            //to get the number of elements in each sub-array, so we use the SI API to collect this
            //information instead
            public PN[][] StreamPages { get; }

            //This is not part of the on-disk data. Do not add/remove items to this list directly, use the appropriate APIs on StreamTable
            public SI[] StreamInfos { get; }

            public long Offset => chunk.AbsoluteOffset;

            public int StructSize
            {
                get
                {
                    var size = sizeof(int); //NumStreams

                    var streamInfos = StreamInfos;

                    size += StreamInfos.Length * sizeof(int);

                    //StreamPages
                    for (var i = 0; i < streamInfos.Length; i++)
                    {
                        ref var si = ref streamInfos[i];

                        //StreamSizes contains the number of bytes in each stream; you have to divide by the page size
                        //to get the actual number of pages
                        size += si.PageList.Length * sizeof(int);
                    }

                    return size;
                }
            }

            private readonly MemoryChunk chunk;

            //Deserialize a stream table from a PDB
            internal StreamTable(in MemoryChunk chunk, int pageSize)
            {
                this.chunk = chunk;
                StreamPages = default!;
                StreamInfos = default!;

                var numStreams = NumStreams;

                var streamSizes = StreamSizes;

                var streamInfos = new SI[numStreams];
                var streamPages = new PN[numStreams][];

                //Skip over NumStreams + StreamSizes
                var pagesChunk = chunk.Slice(sizeof(int) + (NumStreams * sizeof(int)));

                for (var i = 0; i < numStreams; i++)
                {
                    //SI stores the size of a stream (in bytes) and then reads all of the blocks that belong to it.
                    //That's what we want to do here anyway, so may as well defer to SI to do the reading work for us
                    var si = new SI(pagesChunk, streamSizes[i], pageSize);

                    streamPages[i] = si.PageList;
                    streamInfos[i] = si;

                    pagesChunk = pagesChunk.Slice(si.PageList.Length * sizeof(int));
                }

                StreamPages = streamPages;
                StreamInfos = streamInfos;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(this, ViewKind.StreamTable, StructSize);

            int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                //Some streams may have no pages associated with them, so the safest thing for us to write eager

                if (index != -1)
                    throw StructWriter.GetEagerLoadOnlyException();

                using var s = structWriter.CreateEagerWriter();

                s.WriteField("NumStreams", NumStreams);
                s.WriteField("StreamSizes", StreamSizes);

                for (var i = 0; i < StreamPages.Length; i++)
                {
                    var item = StreamPages[i];
                    s.WriteField($"PageList ({i})", item);
                }

                structWriter.EagerFields = s.ToArray();
            }
        }
    }
}
