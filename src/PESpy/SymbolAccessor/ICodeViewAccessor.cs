using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    //Unified interface for allowing different kinds of files to provide access to symbols. e.g. PDB files represent modules as MODI,
    //NB05 vs NB05+ has different orderings that are used for CodeView subsections, etc
    public interface ICodeViewAccessor
    {
        ImageSectionHeader[]? GetSectionHeaders();

        SymType GetModuleSymbol(ushort imod, int ibSym);

        bool TryGetSymbolBySectionAndOffset(ISECT sectionNumber, int relativeOffset, out SymType symType, out int displacement);

        /// <summary>
        /// Gets a type from the TPI stream.
        /// </summary>
        /// <param name="typeIndex">An index into the TPI stream.</param>
        /// <returns>The type pointed to by the type index.</returns>
        TypType GetTypTypeFromIndex(CV_typ_t typeIndex);

        /// <summary>
        /// Gets a type from the IPI stream.
        /// </summary>
        /// <param name="typeIndex">An index into the IPI stream.</param>
        /// <returns>The type pointed to by the type index.</returns>
        TypType GetTypTypeFromIndex(CV_ItemId typeIndex);

        int? GetRelativeVirtualAddress(ushort seg, int off);

        bool HasLengthPrefixedStrings { get; }
    }
}
