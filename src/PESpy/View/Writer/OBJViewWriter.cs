#if PEFAST
using System;
using ClrDebug;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class OBJViewWriter : ViewWriter, IMachineWriter
    {
        private OBJFile objFile;

        public IMAGE_FILE_MACHINE Machine => objFile.FileHeader.Machine;

        internal OBJViewWriter(OBJFile objFile, IFileReader reader) : base(reader, null, ViewMode.Default, TryGetViewOffset, null)
        {
            this.objFile = objFile;
        }

        private static bool TryGetViewOffset(int offset, out int viewoffset)
        {
            viewoffset = offset;
            return true;
        }

        public override IView Finalize()
        {
            if (viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            var merger = new OBJMerger(objFile, structs, extension);

            var results = merger.Merge();

            return new FileView(ViewMode.Physical, results, ViewKind.OBJFile);
        }
    }
}
#endif
