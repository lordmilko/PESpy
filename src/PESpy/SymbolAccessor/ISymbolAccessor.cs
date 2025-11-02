using System;

namespace PESpy
{
    public enum SymbolAccessorKind
    {
        //No symbol accessor was found; the symbol accessor that was returned was the NullSymbolAccessor
        Null,

        //Symbols contained in the CoffSymbolTable, either of the PEFile or a secondary DBGFile
        Coff,

        //CodeView symbols embedded in the file in OMF header format
        CodeView,

        OBJ,
        LIB,

        PDB,

        PortablePDB,

        OMF
    }

    public interface ISymbolAccessor : IDisposable
    {
        SymbolAccessorKind Kind { get; }

        bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement);

        bool TryGetAddressFromName(SymString name, out int targetAddress);
    }
}
