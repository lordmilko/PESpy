using System;
using System.Buffers;
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
            public int NumStreams { get; set; }

            //Eagerly read as a list so that we can append to it. We then need to manually serialize the stream table whenever we make changes
            public List<int> StreamSizes { get; }

            //After the stream sizes there is a PN[][]. However, we need to use each StreamSize[i]
            //to get the number of elements in each sub-array, so we use the SI API to collect this
            //information instead
            public List<List<PN>> StreamPages { get; }

            //This is not part of the on-disk data. Do not add/remove items to this list directly, use the appropriate APIs on StreamTable
            public List<SI> StreamInfos { get; }

            public int Offset => chunk.AbsoluteOffset;

            internal MemoryChunk chunk;

            //Deserialize a stream table from a PDB
            internal StreamTable(in MemoryChunk chunk, int pageSize)
            {
                this.chunk = chunk;
                StreamPages = default!;
                StreamInfos = default!;

                //StreamTable is way too complicated to read straight from the MMF, as we need to have an API that allows performant modification
                NumStreams = chunk.PeekInt32(0);
                StreamSizes = chunk.PeekNativeSpan<int>(4, NumStreams).ToList(); //Span does not have ToList, so use NativeSpan instead

                var numStreams = NumStreams;
                var streamSizes = StreamSizes;

                //Lists so we can add to them when we're editing PDBs
                var streamInfos = new List<SI>(numStreams);
                var streamPages = new List<List<PN>>(numStreams);

                //Skip over NumStreams + StreamSizes
                var pagesChunk = chunk.Slice(sizeof(int) + (NumStreams * sizeof(int)));

                for (var i = 0; i < numStreams; i++)
                {
                    //SI stores the size of a stream (in bytes) and then reads all of the blocks that belong to it.
                    //That's what we want to do here anyway, so may as well defer to SI to do the reading work for us
                    var si = new SI(pagesChunk, streamSizes[i], pageSize);

                    streamPages.Add(si.PageList);
                    streamInfos.Add(si);

                    pagesChunk = pagesChunk.Slice(si.PageList.Count * sizeof(int));
                }

                StreamPages = streamPages;
                StreamInfos = streamInfos;
            }

            public bool HasStream(SN sn)
            {
                if (sn >= StreamInfos.Count)
                    return false;

                var si = StreamInfos[sn];

                return si.ByteCount != -1;
            }

            public SI this[SN sn]
            {
                get
                {
                    EnsureStream(sn);

                    return StreamInfos[sn];
                }
                set
                {
                    EnsureStream(sn);

                    if (value.ByteCount == -1)
                        throw new InvalidOperationException("Attempted to store a stream with an uninitialized byte count");

                    StreamSizes[sn] = value.ByteCount;
                    StreamInfos[sn] = value;
                }
            }

            public SN AllocStream()
            {
                var sn = (SN) NumStreams;
                EnsureStream(sn);
                StreamSizes[sn] = 0;
                var si = StreamInfos[sn];
                si.ByteCount = 0;
                StreamInfos[sn] = si;
                return sn;
            }

            private void EnsureStream(SN sn)
            {
                VerifyStreams();

                while (StreamInfos.Count <= sn)
                {
                    //I tested and confirmed: if you allocate a new stream #5 with mspdbcore.dll
                    //and then save, all of the prior streams will have length -1
                    StreamSizes.Add(-1);

                    var pages = new List<PN>();

                    StreamInfos.Add(new SI(default, -1, pages));
                    StreamPages.Add(pages);
                    NumStreams++;
                }
            }

            private void VerifyStreams()
            {
                if (NumStreams != StreamSizes.Count)
                    throw new InvalidOperationException($"{nameof(NumStreams)} ({NumStreams}) does not match {nameof(StreamSizes)} ({StreamSizes.Count}). Stream Table is corrupt");

                if (NumStreams != StreamInfos.Count)
                    throw new InvalidOperationException($"{nameof(NumStreams)} ({NumStreams}) does not match {nameof(StreamInfos)} ({StreamInfos.Count}). Stream Table is corrupt");

                if (NumStreams != StreamPages.Count)
                    throw new InvalidOperationException($"{nameof(NumStreams)} ({NumStreams}) does not match {nameof(StreamPages)} ({StreamPages.Count}). Stream Table is corrupt");
            }

            public void Serialize(ref SI si, PDB7File pdb)
            {
                VerifyStreams();

                var highestUsedStream = -1; //We'll do +1 below to calculate the actual number of streams, so this cancels out

                //First calculate how much space is required to store the stream table
                var size = sizeof(int);

                //Store the size of each stream's page list
                for (var i = 0; i < StreamInfos.Count; i++)
                {
                    var streamInfo = StreamInfos[i];

                    //If the stream is not initialized (i.e. its length is -1) don't use its size
                    //in our calculations of how big the stream table is
                    var byteCount = streamInfo.ByteCount;

                    if (byteCount >= 0) //If the byteCount is 0 it should be included, only -1 is not included
                    {
                        size += streamInfo.PageList.Count * sizeof(int);
                        highestUsedStream = i;
                    }
                }

                //+1 because highestUsedStream is an index
                highestUsedStream++;

                //Store the number of pages in each stream that has data
                size += highestUsedStream * sizeof(int);

                //How many pages is that?
                var numPages = SI.DivideUp(size, pdb.PageSize);

                //Allocate pages
                si.ByteCount = size;
                si.PageList.Capacity = numPages;
                var streamTableLocation = si.PageList;

                ref readonly var fpm = ref pdb.ActiveFPM;

                for (var i = 0; i < numPages; i++)
                    streamTableLocation.Add(pdb.AllocPage());

                //todo: what happens when we alloc pages beyond our initial global block

                if (chunk.block is PDBGlobalMemoryBlock b)
                {
                    chunk = b.SlicePaged(streamTableLocation, size, copyOld: false);
                }
                else
                {
                    //It's already a paged block. Update the pages + size
                    var pagedBlock = (PagedMemoryBlock) chunk.block;
                    pagedBlock.ReplacePages(streamTableLocation, size, copyOld: false); //No need to copy old, we're going to replace the whole thing
                }

                /* Write the stream table to the pages that should back it. In the event all
                 * of the pages are contiguous, this will write to the actual memory that
                 * backs the PDB. Otherwise, we will write to the temporary memory that backs
                 * the block, which will be copied back to each non-contiguous page when the
                 * data is written to disk */

                //There are three physical components to the stream table: NumStreams, StreamSizes and StreamPages

                //NumStreams
                chunk.PokeInt32(0, highestUsedStream);

                if (highestUsedStream > 0)
                {
                    //StreamSizes
                    var off = 4;

#if NET5_0_OR_GREATER
                    Span<int> streamSizes = CollectionsMarshal.AsSpan(StreamSizes).Slice(0, highestUsedStream);
                    chunk.PokeSpan(off, streamSizes.Length, streamSizes);
#else
                    var rentedArray = ArrayPool<int>.Shared.Rent(highestUsedStream);

                    try
                    {
                        StreamSizes.CopyTo(rentedArray);
                        var streamSizes = new Span<int>(rentedArray, 0, highestUsedStream);
                        chunk.PokeSpan(off, streamSizes.Length, streamSizes);
                    }
                    finally
                    {
                        ArrayPool<int>.Shared.Return(rentedArray);
                    }
#endif

                    off += highestUsedStream * sizeof(int);

                    //StreamPages
                    for (var i = 0; i < highestUsedStream; i++)
                    {
                        var pages = StreamPages[i];

                        if (pages.Count == 0)
                            continue;

#if NET5_0_OR_GREATER
                        Span<PN> span = CollectionsMarshal.AsSpan(pages);
                        chunk.PokeSpan(off, span.Length, span);
#else
                        var rentedPageArray = ArrayPool<PN>.Shared.Rent(pages.Count);

                        try
                        {
                            pages.CopyTo(rentedPageArray);
                            var span = new Span<PN>(rentedPageArray, 0, pages.Count);
                            chunk.PokeSpan(off, span.Length, span);
                        }
                        finally
                        {
                            ArrayPool<PN>.Shared.Return(rentedPageArray);
                        }
#endif
                        off += pages.Count * sizeof(int);
                    }
                }
            }

            //Create an empty stream table
            internal StreamTable(PDBGlobalMemoryBlock block)
            {
                this.chunk = new MemoryChunk(block, 0); //Dummy chunk just to get things going

                /* The default capacity of List<T> is 4. microsoft-pdb allocates space in StrmTbl.mpsnsi
                 * for 256 streams to start with, but then sets the current length to 5 (thereby covering
                 * streams 0-4: snSt -> snIpi). Thus, the next stream that is allocated will be the 6th stream,
                 * stream 5 */

                var defaultCapacity = 256;

                StreamSizes = new List<int>(defaultCapacity);
                StreamPages = new List<List<PN>>(defaultCapacity);
                StreamInfos = new List<SI>(defaultCapacity);

                //Now allocate the first 5 streams which are reserved for snSt -> snIpi. This will ensure
                //that the first allocated user SN is stream 5. Note that none of these streams will be serialized
                //to disk unless their ByteCount is changed from its default value of -1
                EnsureStream(4);
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct("Stream Table", this, ViewKind.StreamTable);

                s.WriteField("NumStreams", NumStreams);
                s.WriteField("StreamSizes", StreamSizes.ToArray());

                for (var i = 0; i < StreamPages.Count; i++)
                {
                    var item = StreamPages[i];
                    s.WriteField($"PageList ({i})", item.ToArray());
                }
            }
        }
    }
}
