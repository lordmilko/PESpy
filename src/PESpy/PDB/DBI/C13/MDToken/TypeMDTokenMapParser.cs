using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal sealed class TypeMDTokenMapParser : MDTokenMapParser<TypeMDTokenMap, TypeMDTokenMap.Entry>
    {
        protected override unsafe TypeMDTokenMap.Entry CreateSmallEntry(long structOffset, byte* chunkPointer, RawMDTokenMapEntry rawEntry, int ridOrTypeSig)
        {
            return new TypeMDTokenMap.Entry(
                structOffset,
                new TypOrEnumType(chunkPointer, (CV_typ_t) rawEntry.RVAOrTypeIndex),
                ridOrTypeSig
            );
        }

        protected override unsafe TypeMDTokenMap.Entry CreateLargeEntry(long structOffset, in MemoryChunk blobChunk, RawMDTokenMapEntry rawEntry, int blobLength)
        {
            return new TypeMDTokenMap.Entry(
                structOffset,
                new TypOrEnumType(blobChunk.Pointer, (CV_typ_t) rawEntry.RVAOrTypeIndex),
                rawEntry.Offset,
                blobChunk.PeekNativeSpan<byte>(0, blobLength)
            );
        }

        protected override TypeMDTokenMap CreateMap(long structOffset, int numEntries, TypeMDTokenMap.Entry[] entries, NativeSpan<byte> dataBlob)
        {
            return new TypeMDTokenMap(structOffset, numEntries, entries, dataBlob);
        }
    }
}
