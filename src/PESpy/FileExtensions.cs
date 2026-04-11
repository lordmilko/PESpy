using System;

namespace PESpy
{
    public static class FileExtensions
    {
        public static ImageSectionHeader[]? GetSectionHeaders(this IFile file)
        {
            switch (file.Kind)
            {
                case FileKind.PDB:
                    return ((PDBFile) file).GetSectionHeaders(); //Non-extension method; handles fallback section headers as well

                case FileKind.PE:
                    return ((PEFile) file).SectionHeaders;

                case FileKind.DBG:
                    return ((DBGFile) file).SectionHeaders;

                case FileKind.OBJ:
                    return ((OBJFile) file).SectionHeaders;

                default:
                    throw new NotImplementedException($"Don't know how to get section headers from a file of type '{file}'");
            }
        }
    }
}
