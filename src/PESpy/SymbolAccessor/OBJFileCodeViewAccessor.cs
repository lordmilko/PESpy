using System;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    internal class OBJFileCodeViewAccessor : ICodeViewAccessor
    {
        public bool HasLengthPrefixedStrings { get; }

        public IMAGE_FILE_MACHINE MachineType => objFile.FileHeader.Machine;

        public bool HasOmapFromSrc => false;

        private OBJFile objFile;

        public OBJFileCodeViewAccessor(OBJFile objFile, bool hasLengthPrefixedStrings)
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

        public bool TryGetSectionContrib(SymType symType, ISECT sectionNumber, int relativeOffset, out SC40 sc)
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

        public int? GetOmapRelativeVirtualAddress(ushort rawSeg, int rawOff) =>
            GetRawRelativeVirtualAddress(rawSeg, rawOff);

        public int? GetRawRelativeVirtualAddress(ushort seg, int off) =>
            SymType.GetRelativeVirtualAddressFromSectionHeaders(GetSectionHeaders(), seg, off);

        public bool TryGetSectionAndOffset(int rva, out ISECT sectionNumber, out int relativeOffset)
        {
            throw new NotImplementedException();
        }

        public NativeSpan<OMAP_DATA> GetOmapFromSrc()
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
    }
}
