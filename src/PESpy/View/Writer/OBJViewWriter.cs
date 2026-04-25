using System;
using ClrDebug;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class OBJViewWriter : ViewWriter, IMachineWriter
    {
        private readonly OBJFile objFile;

        IMAGE_FILE_MACHINE IMachineWriter.GetMachine(in MemoryChunk chunk) => objFile.FileHeader.Machine;

        internal unsafe OBJViewWriter(OBJFile objFile) : base(objFile.CreateByteViewProvider(null), ViewMode.Default, TryGetViewOffset, null)
        {
            this.objFile = objFile;
        }

        private static new bool TryGetViewOffset(int offset, out int viewoffset)
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

            using var merger = new Merger(objFile, this, structs, byteViewProvider);

            var results = merger.MergeOBJ();

            return new FileView(ViewMode.Physical, objFile, results, this, ViewKind.OBJFile);
        }
    }
}
