using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    internal class PDBFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.PDB;

        internal readonly PDBFile PDBFile;

        internal PDBFileSymbolAccessor(PDBFile pdbFile)
        {
            PDBFile = pdbFile;
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            var psgsi = PDBFile.PSGSI;

            if (psgsi != null)
            {
                if (psgsi.TryGetSymbol(name, out var symType))
                {
                    if (symType.TryGetRVA(PDBFile, out targetAddress))
                        return true;

                    //We found the symbol, and it doesn't have an address
                    return false;
                }
            }

            var gsi = PDBFile.GSI;

            if (gsi != null)
            {
                if (gsi.TryGetSymbol(name, out var symType))
                {
                    if (symType.TryGetRVA(PDBFile, out targetAddress))
                        return true;

                    //We found the symbol, and it doesn't have an address
                    return false;
                }
            }

            //Not sure what to do for internal symbols

            targetAddress = default;
            return false;
        }

        public bool TryGetNameFromAddress(int rva, out SymString name, out int displacement)
        {
            if (PDBFile.TryGetSymbolByRVA(rva, out var symType, out displacement, out var imod))
            {
                if (symType.rectyp == ClrDebug.PDB.SYM_ENUM_e.S_SEPCODE)
                {
                    var sepCode = (SepCodeSym) symType;

                    var codeViewModuleAccessor = PDBFile.DBI!.Modules![imod].Symbols;

                    symType = sepCode.GetParent(codeViewModuleAccessor);

                    if (sepCode.sect != sepCode.sectParent)
                        throw new System.NotImplementedException(); //Convert sepcode and parent to rva and then get difference

                    displacement = sepCode.off - sepCode.offParent;
                }

                name = symType.GetName(PDBFile);
                return true;
            }

            name = default;
            return false;
        }

        public bool TryGetLengthFromAddress(int rva, ISectionDataAccessor sectionDataAccessor, out int length)
        {
            if (PDBFile.TryGetSymbolByRVA(rva, out var symType, out var displacement))
            {
                if (symType.TryGetLength(out length, PDBFile))
                {
                    //If displacement is greater than length, we clearly have a public that reported a section contrib
                    //that is prior to the actual code location we asked for
                    if (displacement < length)
                    {
                        length -= displacement;
                        return true;
                    }
                }

                return false;
            }

            length = default;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
