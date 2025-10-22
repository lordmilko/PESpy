using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal sealed class TypeMDTokenMapParser : MDTokenMapParser<TypeMDTokenMap, TypeMDTokenMap.Entry>
    {
        protected override unsafe TypeMDTokenMap.Entry CreateSmallEntry(byte* chunkPointer, RawMDTokenMapEntry rawEntry, int ridOrTypeSig)
        {
            return new TypeMDTokenMap.Entry(
                new TypOrEnumType(chunkPointer, (CV_typ_t) rawEntry.RVAOrTypeIndex),
                ridOrTypeSig
            );
        }

        protected override unsafe TypeMDTokenMap.Entry CreateLargeEntry(in MemoryChunk blobChunk, RawMDTokenMapEntry rawEntry, int blobLength)
        {
            return new TypeMDTokenMap.Entry(
                new TypOrEnumType(blobChunk.Pointer, (CV_typ_t) rawEntry.RVAOrTypeIndex),
                blobChunk.PeekNativeSpan<byte>(0, blobLength)
            );
        }

        protected override TypeMDTokenMap CreateMap(int numEntries, TypeMDTokenMap.Entry[] entries, NativeSpan<byte> dataBlob)
        {
            return new TypeMDTokenMap(numEntries, entries, dataBlob);
        }
    }
}
