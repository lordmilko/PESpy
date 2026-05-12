using System;
using System.Threading;
using PESpy.View;

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

                case FileKind.DOS:
                case FileKind.NE:
                case FileKind.LE:
                    //Synthesize section headers? We need these for resolving CodeView symbol RVAs
                    return null;

                default:
                    throw new NotImplementedException($"Don't know how to get section headers from a file of type '{file}'");
            }
        }

        public static FileView GetView(
            this IFile file,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default,
            bool excludeSymbols = false)
        {
            return file.GetView(new FileAnalyzerOptions
            {
                HttpPolicy = httpPolicy,
                TrackXRefs = trackXRefs,
                CancellationToken = cancellationToken,
                ExcludeSymbols = excludeSymbols
            });
        }

        public static FileView GetView(
            this PEFile peFile,
            ViewMode viewMode,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default,
            bool excludeSymbols = false)
        {
            return peFile.GetView(viewMode, new FileAnalyzerOptions
            {
                HttpPolicy = httpPolicy,
                TrackXRefs = trackXRefs,
                CancellationToken = cancellationToken,
                ExcludeSymbols = excludeSymbols
            });
        }
    }
}
