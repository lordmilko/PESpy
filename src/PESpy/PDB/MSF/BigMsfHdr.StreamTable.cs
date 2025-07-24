using System;
using System.Collections.Generic;
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
            //snMac
            public int NumStreams => chunk.PeekInt32(0);

            //Eagerly read as a list so that we can append to it. We then need to manually serialize the stream table whenever we make changes
            public NativeSpan<int> StreamSizes => chunk.PeekNativeSpan<int>(4, NumStreams);

            //After the stream sizes there is a PN[][]. However, we need to use each StreamSize[i]
            //to get the number of elements in each sub-array, so we use the SI API to collect this
            //information instead
            public PN[][] StreamPages { get; }

            //This is not part of the on-disk data. Do not add/remove items to this list directly, use the appropriate APIs on StreamTable
            public SI[] StreamInfos { get; }

            public int Offset => chunk.AbsoluteOffset;

            public int StructSize
            {
                get
                {
                    var size = sizeof(int); //NumStreams

                    var streamSizes = StreamSizes;

                    size += streamSizes.Length + sizeof(int); //StreamSizes

                    //StreamPages
                    for (var i = 0; i < streamSizes.Length; i++)
                        size += streamSizes[i] * sizeof(int);

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
                writer.NewStruct(Strings.StreamTable, this, ViewKind.StreamTable, StructSize);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

                s.WriteField("NumStreams", NumStreams);
                s.WriteField("StreamSizes", StreamSizes.ToArray());

                for (var i = 0; i < StreamPages.Length; i++)
                {
                    var item = StreamPages[i];
                    s.WriteField($"PageList ({i})", item);
                }

                return s.ToArray();
            }
        }
    }
}
