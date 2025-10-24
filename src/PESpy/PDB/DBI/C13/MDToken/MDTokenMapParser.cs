namespace PESpy.PDB
{
    //We're better of just allocating a parser than passing 3 lambdas for the various
    //abstract methods to a single "super parser" method
    internal abstract class MDTokenMapParser<TMap, TEntry>
    {
        /* cvdump.cpp shows how to dump token maps in DumpModTokenMap. cvdump treats the type and func sections as having the same format. In a sense this is true (at least
         * when it comes to the main entries)
         * 
         * For func entries, per https://github.com/MichalStrehovsky/corert/blob/e5fe74555a23f35cf507051faeeee7e13fa8e9a2/src/ILCompiler.Compiler/src/Compiler/DependencyAnalysis/WindowsDebugMethodMapSection.cs#L63
         * the format is:
         * - 4 byte count of entries in the map
         * - count entries of a data structure with the following fields
         *     - RVA
         *     - an offset relative to the start of the method data blob
         * - method data, where the format of each record is
         *     - count of generic parameters
         *     - 3 bytes of the method's RID
         *     - count ECMA-335 TypeSpec signatures for each of the generic parameters
         * 
         * For type entries, per https://github.com/MichalStrehovsky/corert/blob/e5fe74555a23f35cf507051faeeee7e13fa8e9a2/src/ILCompiler.Compiler/src/Compiler/DependencyAnalysis/WindowsDebugTypeSignatureMapSection.cs#L44
         * the format basically seems to be the same as the format used for func entries.
         * 
         * In Func Entries, you have an RVA + Offset, where the Offset is either an offset in the data blob, or an RID with the high
         * bit set. In Type Entries, you have a TypeIndex + Offset, where the Offset is either an offset in the data blob, or a 4 byte
         * value representing an ECMA 335 encoded type signature.
         * 
         * The wording about how the high bit in the Offset is set when it comes to types is slightly different than when it comes to funcs.
         * 
         * corecrt says that the value stored in offset when the type signature is 4 bytes or less is
         * 
         *     offset = (1 << 31) | (sig[0] << 24 | sig[1] << 16 | sig[2] << 8 | sig[3])
         * 
         * However, the format that is used for func entries is
         * 
         *     emitted.IlTokenRid | 0x80000000
         *     
         * While these values may be constructed in different ways, in little endian format they are essentially in the same order.
         * But of course, since for funcs we want an RID and for types we want 3 raw bytes, our createEntry handler will need to make
         * sense of these bytes according to their required format
         */

        public unsafe TMap Parse(in MemoryChunk dataChunk, int length)
        {
            var numEntries = dataChunk.PeekInt32(0);

            var methodDataChunkStart = 4 + (numEntries * 8);

            var methodDataChunk = dataChunk.Slice(methodDataChunkStart);
            var methodDataChunkLength = length - methodDataChunkStart;

            var rawEntries = dataChunk.PeekNativeSpan<RawMDTokenMapEntry>(4, numEntries);

            var entries = new TEntry[numEntries];

            for (var i = 0; i < rawEntries.Length; i++)
            {
                var rawEntry = rawEntries[i];

                var offset = rawEntry.Offset;

                var structOffset = dataChunk.AbsoluteOffset + 4 + (i * 8);

                if ((offset >> 31) != 0)
                {
                    /* The high bit is set, which means that for a func entry this is the RID,
                     * and for a type entry the type signature fit within 4 bytes. We've got a bit
                     * of a problem when it comes to types, because we want to create a span around these bytes,
                     * but we also need the bit to be cleared! This is a catch-22, I don't see any good solution to this
                     * that doesn't involve allocating, so for types we'll need to expose an API that provides either the
                     * "cleaned" small type signature, or large normal one */
                    var ridOrTypeSig = (int) (offset &= 0x7fffffff); //Clear the high bit

                    //Type entries need a pointer to the chunk so they can resolve their types
                    entries[i] = CreateSmallEntry(structOffset, dataChunk.Pointer, rawEntry, ridOrTypeSig);
                }
                else
                {
                    //The items are sorted by offset, which means any items with the high bit set will be last

                    int blobLength;

                    if (i < rawEntries.Length - 1)
                    {
                        var nextOffset = rawEntries[i + 1].Offset;

                        if ((nextOffset >> 31) != 0)
                        {
                            //Given the entries are sorted by offset, if we're up to an entry with the high bit set, this means
                            //that that entry (and all entries after it) won't have any data in the method data blob, which therefore
                            //means that this is the last item, and thus our length is up to the very end of the method data blob
                            blobLength = methodDataChunkLength - (int) offset;
                        }
                        else
                        {
                            //The length of the distance between this and the next offset
                            blobLength = (int) (nextOffset - offset);
                        }
                    }
                    else
                    {
                        //We're the last item and there were no non-generic methods. We take up all bytes to the end of the
                        //method data chunk section
                        blobLength = methodDataChunkLength - (int) offset;
                    }

                    entries[i] = CreateLargeEntry(structOffset, methodDataChunk.Slice((int) offset), rawEntry, blobLength);
                }
            }

            var dataBlob = methodDataChunk.PeekNativeSpan<byte>(0, methodDataChunkLength);

            var map = CreateMap(dataChunk.AbsoluteOffset, numEntries, entries, dataBlob);

            return map;
        }

        //Value is the "cleaned" value with the top bit cleared
        protected abstract unsafe TEntry CreateSmallEntry(int structOffset, byte* chunkPointer, RawMDTokenMapEntry rawEntry, int ridOrTypeSig);

        protected abstract TEntry CreateLargeEntry(
            int structOffset,
            in MemoryChunk blobChunk,
            RawMDTokenMapEntry rawEntry,
            int blobLength);

        protected abstract TMap CreateMap(int structOffset, int numEntries, TEntry[] entries, NativeSpan<byte> dataBlob);
    }
}
