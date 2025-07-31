using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    //Unified interface for allowing different kinds of files to provide access to symbols. e.g. PDB files represent modules as MODI,
    //NB05 vs NB05+ has different orderings that are used for CodeView subsections, etc
    internal interface ISymbolAccessor
    {
        ImageSectionHeader[]? GetSectionHeaders();

        SymType GetModuleSymbol(ushort imod, int ibSym);

        TypType GetTypTypeFromIndex(CV_typ_t typeIndex);

        int? GetRelativeVirtualAddress(ushort seg, int off);

        bool HasLengthPrefixedStrings { get; }
    }
}
