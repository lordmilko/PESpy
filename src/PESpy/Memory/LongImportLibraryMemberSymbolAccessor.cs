using ClrDebug.PDB;
using PESpy.LIB;
using PESpy.PDB;

namespace PESpy
{
    internal class LongImportLibraryMemberSymbolAccessor : ICodeViewAccessor
    {
        public bool HasLengthPrefixedStrings { get; }

        private LongImportLibraryMember longImportLibraryMember;

        public LongImportLibraryMemberSymbolAccessor(LongImportLibraryMember longImportLibraryMember, bool hasLengthPrefixedStrings)
        {
            this.longImportLibraryMember = longImportLibraryMember;
            HasLengthPrefixedStrings = hasLengthPrefixedStrings;
        }

        public SymType GetModuleSymbol(ushort imod, int ibSym)
        {
            throw new System.NotImplementedException();
        }

        public ImageSectionHeader[]? GetSectionHeaders()
        {
            throw new System.NotImplementedException();
        }

        public TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            throw new System.NotImplementedException();
        }

        public TypType GetTypTypeFromIndex(CV_ItemId typeIndex)
        {
            throw new System.NotImplementedException();
        }

        public int? GetRelativeVirtualAddress(ushort seg, int off) =>
            SymType.GetRelativeVirtualAddressFromSectionHeaders(GetSectionHeaders(), seg, off);
    }
}
