using System;
using System.Buffers;
using System.Collections.Generic;

namespace PESpy.PDB
{
    public class StreamTableBuilder
    {
        private StreamInfoBuilder[] streams;
        private int numStreams;

        private PDBFileBuilder pdbFileBuilder;
        private int highestUsedStream;

        internal StreamTableBuilder(PDBFileBuilder pdbFileBuilder)
        {
            this.pdbFileBuilder = pdbFileBuilder;

            streams = new StreamInfoBuilder[256]; //microsoft-pdb allocates an initial buffer of 256

            /* The default capacity of List<T> is 4. microsoft-pdb allocates space in StrmTbl.mpsnsi
             * for 256 streams to start with, but then sets the current length to 5 (thereby covering
             * streams 0-4: snSt -> snIpi). Thus, the next stream that is allocated will be the 6th stream,
             * stream 5 */

            //I tested and confirmed: if you allocate a new stream #5 with mspdbcore.dll
            //and then save, all of the prior streams will have length -1

            //Now allocate the first 5 streams which are reserved for snSt -> snIpi. This will ensure
            //that the first allocated user SN is stream 5. Note that none of these streams will be serialized
            //to disk unless their ByteCount is changed from its default value of -1
            streams[SN.ST] = new StreamInfoBuilder  { sn = SN.ST,  ByteCount = -1, PageList = new List<PN>() };
            streams[SN.PDB] = new StreamInfoBuilder { sn = SN.PDB, ByteCount = -1, PageList = new List<PN>() };
            streams[SN.TPI] = new StreamInfoBuilder { sn = SN.TPI, ByteCount = -1, PageList = new List<PN>() };
            streams[SN.DBI] = new StreamInfoBuilder { sn = SN.DBI, ByteCount = -1, PageList = new List<PN>() };
            streams[SN.IPI] = new StreamInfoBuilder { sn = SN.IPI, ByteCount = -1, PageList = new List<PN>() };

            numStreams = 5;
        }

        internal StreamTableBuilder(BigMsfHdr.StreamTable streamTable, PDBFileBuilder pdbFileBuilder)
        {
            this.pdbFileBuilder = pdbFileBuilder;

            throw new NotImplementedException();
        }

        public ref StreamInfoBuilder this[SN index]
        {
            get
            {
                if (index >= numStreams)
                {
                    if (numStreams >= streams.Length)
                    {
                        var newCapacity = streams.Length * 2;
                        Array.Resize(ref streams, newCapacity);
                    }

                    streams[index] = new StreamInfoBuilder
                    {
                        sn = index,
                        ByteCount = -1,
                        PageList = new List<PN>()
                    };
                    numStreams++;
                }

                return ref streams[index];
            }
        }

        public SN AllocStream()
        {
            ref var si = ref this[(SN) numStreams];
            si.ByteCount = 0;
            return si.sn;
        }

        internal StreamInfoBuilder Measure()
        {
            highestUsedStream = -1; //We'll do +1 below to calculate the actual number of streams, so this cancels out

            //First calculate how much space is required to store the stream table
            var size = sizeof(int);

            //Store the size of each stream's page list
            for (var i = 0; i < numStreams; i++)
            {
                ref var streamInfo = ref streams[i];

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

            //We allocate a new stream for the new stream table, and only update snSt after serializing
            var si = pdbFileBuilder.AllocTempStream(size);

            return si;
        }

        internal void Serialize(in StreamInfoBuilder newSiSt)
        {
            var chunk = pdbFileBuilder.SlicePaged(newSiSt.PageList.ToArray(), newSiSt.ByteCount);

            //NumStreams
            chunk.PokeInt32(0, highestUsedStream);

            if (highestUsedStream > 0)
            {
                //StreamSizes
                var off = 4;

                for (var i = 0; i < highestUsedStream; i++)
                {
                    ref var si = ref streams[i];
                    chunk.PokeInt32(off, si.ByteCount);
                    off += sizeof(int);
                }

                //StreamPages
                for (var i = 0; i < highestUsedStream; i++)
                {
                    ref var si = ref streams[i];

                    var pages = si.PageList;

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

            //Now that the old snSt has been serialized, write the new one to the stream table
            pdbFileBuilder.ReplaceStream(SN.ST, newSiSt);
        }
    }
}
