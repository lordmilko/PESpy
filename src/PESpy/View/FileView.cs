using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    [DebuggerDisplay("{ViewDebuggerDisplay.File(this),nq}")]
    public class FileView : IContainerView, IEnumerable<IView>
    {
        public ViewMode ViewMode { get; }

        public string? Name { get; }

        public int Offset { get; }
        public int Size { get; }

        public ViewKind Kind { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter);

        private ViewWriter viewWriter;
        private IViewable childProvider;

        public FileView(ViewMode viewMode, string? name, IView[] children, ViewWriter viewWriter, ViewKind kind)
        {
            if (viewMode == ViewMode.Default)
                throw new ArgumentException($"ViewMode {viewMode} should have been transformed into a more specific type");

            ViewMode = viewMode;
            Name = name;
            Offset = children.Length > 0 ? children[0].Offset : 0;
            Size = children.Sum(r => r.Size);
            childProvider = new ViewChildProvider(children);
            this.viewWriter = viewWriter;
            Kind = kind;
        }

        internal FileView(
            in NestedFileRange range,
            FileAccessor fileAccessor,
            ViewEntityIterator iterator)
        {
            Name = range.File.Name;
            ViewMode = ViewMode.Physical;
            Offset = range.StartOffset;
            Size = range.Length;
            Debug.Assert(range.NestedWriter != null);
            this.viewWriter = range.NestedWriter;

            if (range.File.Kind == FileKind.PE)
            {
                var peFile = (PEFile) range.File;

                //SectionRanges lists virtual addresses, but SectionHeaders always list physical
                var sectionHeaders = peFile.SectionHeaders;

                using var list = new PooledList<IView>(sectionHeaders.Length + 1);

                var sizeOfHeaders = peFile.OptionalHeader.SizeOfHeaders;
                var startOffset = peFile.blockProvider.StartOffset;

                list.Add(new HeaderView(startOffset, sizeOfHeaders, fileAccessor, iterator.SliceFromCurrent(sizeOfHeaders), range.NestedWriter));
                var lastSectionEnd = range.StartOffset + sizeOfHeaders;
                iterator.MoveTo(lastSectionEnd);

                for (var i = 0; i < sectionHeaders.Length; i++)
                {
                    ref var sectionHeader = ref sectionHeaders[i];

                    var sectionStart = startOffset + sectionHeader.PointerToRawData;
                    list.Add(new SectionView(
                        sectionStart,
                        sectionHeader.SizeOfRawData,
                        sectionHeader.Name.ToString(),
                        fileAccessor,
                        iterator.SliceFromCurrent(sectionHeader.SizeOfRawData),
                        range.NestedWriter)
                    );
                    lastSectionEnd = sectionStart + sectionHeader.SizeOfRawData;
                    iterator.MoveTo(lastSectionEnd);
                }

                if (lastSectionEnd != range.EndOffset)
                    throw new NotImplementedException(); //There's also an overlay

                childProvider = new ViewChildProvider(list.ToArray());
                Kind = ViewKind.PEFile;
            }
            else
                throw new NotImplementedException();
        }

        public IEnumerator<IView> GetEnumerator() => ((IEnumerable<IView>) Children).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitFile(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitFile(this);
    }
}
