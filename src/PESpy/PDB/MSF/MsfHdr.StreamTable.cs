using System;
using System.Collections.Generic;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly partial struct MsfHdr
    {
        public class StreamTable : IStreamTable
        {
            public int NumStreams => chunk.PeekInt32(0);

            public SI_PERSIST[] StreamPersists { get; }

            public PN[][] StreamPages { get; }

            //This is not part of the on-disk data
            public SI[] StreamInfos { get; }

            public int Offset => chunk.AbsoluteOffset;

            private readonly MemoryChunk chunk;

            internal StreamTable(in MemoryChunk chunk, int pageSize)
            {
                this.chunk = chunk;

                var numStreams = NumStreams;

                //SI_PERSIST stores the stream size and a page list to something (currently unknown), but the in-memory API of microsoft-pdb
                //tends to use SI entities, so we're going to do the same
                var streamPersists = new SI_PERSIST[numStreams];
                var streamInfos = new SI[numStreams];
                var streamPages = new PN[numStreams][];

                for (var i = 0; i < numStreams; i++)
                    streamPersists[i] = new SI_PERSIST(chunk.Slice(sizeof(int) + (i * SI_PERSIST.StructSize)));

                //Skip over NumStreams + StreamSizes
                var pagesChunk = chunk.Slice(4 + (NumStreams * SI_PERSIST.StructSize));

                for (var i = 0; i < numStreams; i++)
                {
                    //SI stores the size of a stream (in bytes) and then reads all of the blocks that belong to it.
                    //That's what we want to do here anyway, so may as well defer to SI to do the reading work for us
                    var si = new SI(pagesChunk, streamPersists[i], pageSize);

                    streamPages[i] = si.PageList;
                    streamInfos[i] = si;

                    pagesChunk = pagesChunk.Slice(si.PageList.Length * sizeof(short));
                }

                StreamPersists = streamPersists;
                StreamPages = streamPages;
                StreamInfos = streamInfos;
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct("Stream Table", this, ViewKind.StreamTable);

                s.WriteField("NumStreams", NumStreams);
                s.WriteInline(StreamPersists);

                //We store the StreamPages as 32-bit but they were originally 16-bit

                for (var i = 0; i < StreamPages.Length; i++)
                {
                    var item = StreamPages[i];

                    var arr = new ushort[item.Length];

                    for (var j = 0; j < item.Length; j++)
                        arr[j] = (ushort) (int) item[j];

                    s.WriteField($"PageList ({i})", arr);
                }
            }
        }
    }
}
