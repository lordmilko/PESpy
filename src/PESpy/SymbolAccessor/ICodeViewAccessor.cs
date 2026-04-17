using ClrDebug;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    public interface ICodeViewModuleAccessor
    {
        SymTypeList Symbols { get; }

        bool TryGetFunctionSymbol(int off, ISECT seg, out SymType symType);
    }

    //Unified interface for allowing different kinds of files to provide access to symbols. e.g. PDB files represent modules as MODI,
    //NB05 vs NB05+ has different orderings that are used for CodeView subsections, etc
    public interface ICodeViewAccessor
    {
        IMAGE_FILE_MACHINE MachineType { get; }

        bool HasOmapFromSrc { get; }

        ImageSectionHeader[]? GetSectionHeaders();

        SymType GetModuleSymbol(ushort imod, int ibSym);

        bool TryGetSymbolBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out SymType symType,
            out int displacement,
            out IMOD imod);

        bool TryGetSectionContrib(SymType symType, ISECT sectionNumber, int relativeOffset, out SC40 sc);

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

        //rawSeg and rawOff will be converted to an RVA, which will then be resolved to an OMAP-aware RVA
        int? GetOmapRelativeVirtualAddress(ushort rawSeg, int rawOff);

        /// <summary>
        /// Gets the relative virtual address associated with an offset
        /// into a given section. This method does not perform source or destination OMAP transformations;
        /// the given off/seg is simply converted straight to an RVA based on the <see cref="ImageSectionHeader"/>
        /// <paramref name="seg"/> references.
        /// </summary>
        /// <param name="seg">The segment of the address.</param>
        /// <param name="off">The relative offset into the given segment.</param>
        /// <returns>The RVA that is associated with the given off/seg pair, or null if the off/seg pair
        /// do not correspond to a valid RVA.</returns>
        int? GetRawRelativeVirtualAddress(ushort seg, int off);

        bool TryGetSectionAndOffset(int rva, out ISECT sectionNumber, out int relativeOffset);

        NativeSpan<OMAP_DATA> GetOmapFromSrc();

        bool HasLengthPrefixedStrings { get; }
    }
}
