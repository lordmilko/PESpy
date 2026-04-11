using System.Linq;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.LIB;
using PESpy.OBJ;
using PESpy.PDB;

namespace PESpy
{
    internal class LongImportLibraryMemberSymbolAccessor : ICodeViewAccessor
    {
        private OBJTypesTable typesTable;

        public bool HasLengthPrefixedStrings { get; }

        private LongImportLibraryMember longImportLibraryMember;

        IMAGE_FILE_MACHINE ICodeViewAccessor.MachineType => longImportLibraryMember.FileHeader.Machine;

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

        public bool TryGetSectionContrib(SymType symType, ISECT sectionNumber, int relativeOffset, out SC40 sc)
        {
            throw new System.NotImplementedException();
        }

        public TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            if (typesTable == null)
            {
                typesTable = longImportLibraryMember.GetSectionData<OBJTypesTable>(".debug$T").FirstOrDefault();

                if (typesTable == null)
                    throw new System.NotImplementedException();
            }

            return typesTable.GetTypTypeFromIndex(typeIndex);
        }

        public TypType GetTypTypeFromIndex(CV_ItemId typeIndex)
        {
            throw new System.NotImplementedException();
        }

        public int? GetRelativeVirtualAddress(ushort seg, int off) =>
            SymType.GetRelativeVirtualAddressFromSectionHeaders(GetSectionHeaders(), seg, off);

        public bool TryGetSymbolBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out SymType symType,
            out int displacement,
            out IMOD imod)
        {
            throw new System.NotImplementedException();
        }
    }
}
