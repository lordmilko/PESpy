using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    internal class OBJSymbolAccessor : ICodeViewAccessor
    {
        public bool HasLengthPrefixedStrings { get; }

        private OBJFile objFile;

        public OBJSymbolAccessor(OBJFile objFile, bool hasLengthPrefixedStrings)
        {
            this.objFile = objFile;
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
