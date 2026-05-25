using ClrDebug;

namespace PESpy
{
    public class OBJFileOverview
    {
        public FileKind Kind => FileKind.OBJ;

        /// <summary>
        /// Gets the machine type listed in <see cref="ImageFileHeader.Machine"/>.
        /// </summary>
        public IMAGE_FILE_MACHINE Machine { get; set; }

        public OBJFileOverview(OBJFile file, ISymbolAccessor symbolAccessor)
        {
            Machine = file.FileHeader.Machine;
        }
    }
}
