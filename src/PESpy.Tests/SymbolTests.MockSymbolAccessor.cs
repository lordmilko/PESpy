using System;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy.Tests
{
    internal class MockSymbolAccessor : ICodeViewAccessor
    {
        public bool HasLengthPrefixedStrings { get; set; }

        public IMAGE_FILE_MACHINE MachineType => IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386;

        public bool HasOmapFromSrc => false;

        public SymType GetModuleSymbol(ushort imod, int ibSym)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSymbolBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out SymType symType,
            out int displacement,
            out IMOD imod)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSectionContrib(SymType symType, ISECT sectionNumber, int relativeOffset, out SC40 sc)
        {
            throw new NotImplementedException();
        }

        public int? GetOmapRelativeVirtualAddress(ushort seg, int off)
        {
            throw new NotImplementedException();
        }

        public int? GetRawRelativeVirtualAddress(ushort seg, int off)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSectionAndOffset(int rva, out ISECT sectionNumber, out int relativeOffset)
        {
            throw new NotImplementedException();
        }

        public NativeSpan<OMAP_DATA> GetOmapFromSrc()
        {
            throw new NotImplementedException();
        }

        public ImageSectionHeader[] GetSectionHeaders()
        {
            throw new NotImplementedException();
        }

        public TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            throw new NotImplementedException();
        }

        public TypType GetTypTypeFromIndex(CV_ItemId typeIndex)
        {
            throw new NotImplementedException();
        }
    }
}
