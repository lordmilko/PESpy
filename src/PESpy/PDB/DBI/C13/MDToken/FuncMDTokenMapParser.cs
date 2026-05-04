using ClrDebug;

namespace PESpy.PDB
{
    internal sealed class FuncMDTokenMapParser : MDTokenMapParser<FuncMDTokenMap, FuncMDTokenMap.Entry>
    {
        protected override unsafe FuncMDTokenMap.Entry CreateSmallEntry(long offset, byte* chunkPointer, RawMDTokenMapEntry rawEntry, int ridOrTypeSig)
        {
            //corert has an assert that EmitMetadataHandleForTypeSystemEntity returns a handle of type MemberReference for the method it passes in
            var token = (mdMemberRef) Extensions.TokenFromRid((int) ridOrTypeSig, CorTokenType.mdtMemberRef);

            return new FuncMDTokenMap.Entry(offset, rawEntry.RVAOrTypeIndex, rawEntry.Offset, token);
        }

        protected override FuncMDTokenMap.Entry CreateLargeEntry(
            long structOffset,
            in MemoryChunk blobChunk,
            RawMDTokenMapEntry rawEntry,
            int blobLength)
        {
            /* A big gotcha with the count of generic parameters is that it was set as cGenericArguments << 24 | methodTokenRid,
             * which means in little endian it's the last byte, not the first!
             * 
             * If the high bit of offset is set, this means that there's no need for any TypeSpec items, and the mdToken
             * can be encoded in offset directly. The RID part of the token is thus stored in the last 3 bytes of offset*/

            blobLength -= 4; //Subtract the bytes for the count of generic args and the RID from the count!

            //As mentioned above, the count of generic arguments is written at the same time as the method RID, which means
            //in little endian the count of generic arguments is actually the "last" byte in the DWORD. Thus, we need to read
            //both the RID and the count of generic arguments simultaneously
            var numGenericArgsAndRID = blobChunk.PeekUInt32(0);

            var numGenericArgs = numGenericArgsAndRID >> 24;
            var rid = numGenericArgsAndRID & 0xffffff;

            var token = (mdMemberRef) Extensions.TokenFromRid((int) rid, CorTokenType.mdtMemberRef);

            var typeSpecBlobs = blobChunk.PeekNativeSpan<byte>(4, blobLength);

            var entry = new FuncMDTokenMap.Entry(
                structOffset,
                rawEntry.RVAOrTypeIndex,
                rawEntry.Offset,
                token,
                (int) numGenericArgs,
                typeSpecBlobs
            );

            return entry;
        }

        protected override FuncMDTokenMap CreateMap(long structOffset, int numEntries, FuncMDTokenMap.Entry[] entries, NativeSpan<byte> dataBlob)
        {
            return new FuncMDTokenMap(structOffset, numEntries, entries, dataBlob);
        }
    }
}
