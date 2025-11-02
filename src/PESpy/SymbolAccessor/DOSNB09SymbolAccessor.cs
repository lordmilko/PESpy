using System.Diagnostics;
using ClrDebug;
using ClrDebug.OMF;

namespace PESpy
{
    internal class DOSNB09SymbolAccessor : NB09SymbolAccessor
    {
        private DOSFile dosFile;

        //If not found, we'll set it to -1
        private int segMapIndex = -2;

        public DOSNB09SymbolAccessor(IFile file) : base(file)
        {
            dosFile = (DOSFile) file;
        }

        public bool TryGetSectionCharacteristics(ushort seg, int off, out IMAGE_SCN characteristics)
        {
            if (seg != 1)
            {
                characteristics = default;
                return false;
            }

            //We're assuming seg 1 is always code
            characteristics = IMAGE_SCN.CNT_CODE;
            return true;
        }

        public override int? GetRelativeVirtualAddress(ushort seg, int off)
        {
            //We don't know how to properly resolve symbols that are not in segment 1
            if (seg != 1)
                return null;

            var segMapIndex = GetSegMapIndex();

            if (segMapIndex == -1)
                return null;

            ref var dirEntry = ref data.DirEntries[segMapIndex];

            var segMap = (OMFSegMap) dirEntry.Data!;

            ref var desc = ref segMap.rgDesc[seg - 1];
            ref var group = ref segMap.rgDesc[desc.group];

            Debug.Assert(desc.frame == 0);
            Debug.Assert(desc.offset == 0);
            Debug.Assert(group.offset == 0);

            return dosFile.SizeOfHeaders + off;
        }

        private int GetSegMapIndex()
        {
            if (segMapIndex != -2)
                return segMapIndex;

            for (var i = data.DirEntries.Length - 1; i >= 0; i--)
            {
                ref var entry = ref data.DirEntries[i];

                if (entry.iMod != ushort.MaxValue)
                    break;

                if (entry.SubSection == SST.sstSegMap)
                {
                    segMapIndex = i;
                    return segMapIndex;
                }
            }

            segMapIndex = -1;
            return segMapIndex;
        }
    }
}
