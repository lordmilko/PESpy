using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    class SimpleViewWriterHelper : IViewWriterHelper
    {
        protected IFile file;

        public FileKind FileKind => file.Kind;

        public bool Is32Bit => throw new NotSupportedException();

        public ViewWriter.TryGetOffsetDelegate TryGetOffsetDelegate { get; }

        public Func<int, int>? GetRealOffsetDelegate { get; }

        internal SimpleViewWriterHelper(IFile file)
        {
            this.file = file;
            TryGetOffsetDelegate = TryGetViewOffset;
            GetRealOffsetDelegate = null;
        }

        public static bool TryGetViewOffset(long offset, out long viewoffset)
        {
            viewoffset = offset;
            return true;
        }

        public virtual IView Finalize(ViewWriter viewWriter)
        {
            if (viewWriter.viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = viewWriter.globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            using var merger = new Merger(file, viewWriter, structs, viewWriter.byteViewProvider);

            IView[] results;
            ViewKind kind;

            switch (file.Kind)
            {
                case FileKind.DBG:
                    kind = ViewKind.DBGFile;
                    results = merger.MergeDBG();
                    break;

                case FileKind.DOS:
                    kind = ViewKind.DOSFile;
                    results = merger.MergeDOS();
                    break;

                case FileKind.LE:
                    kind = ViewKind.LEFile;
                    results = merger.MergeLE();
                    break;

                case FileKind.NE:
                    kind = ViewKind.NEFile;
                    results = merger.MergeNE();
                    break;

                case FileKind.OBJ:
                    kind = ViewKind.OBJFile;
                    results = merger.MergeOBJ();
                    break;

                case FileKind.OMF:
                    kind = ViewKind.OMFFile;
                    throw new NotImplementedException();

                case FileKind.OMFLIB:
                    kind = ViewKind.OMFLIBFile;
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }

            return new FileView(ViewMode.Physical, file, results, viewWriter, kind);
        }

        public virtual void CollectDataDirectories(ref ValueList<DirectoryInfo> dataDirectories)
        {
        }
    }
}
