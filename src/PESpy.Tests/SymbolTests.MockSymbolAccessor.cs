using System;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy.Tests
{
    internal class MockSymbolAccessor : ICodeViewAccessor
    {
        public bool HasLengthPrefixedStrings { get; set; }

        public SymType GetModuleSymbol(ushort imod, int ibSym)
        {
            throw new NotImplementedException();
        }

        public int? GetRelativeVirtualAddress(ushort seg, int off)
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

        public bool TryGetSymbolBySectionAndOffset(ISECT sectionNumber, int relativeOffset, out SymType symType, out int displacement)
        {
            throw new NotImplementedException();
        }
    }
}
