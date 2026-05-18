using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    public class FileView : IViewInternal, IEnumerable<IView>
    {
        public ViewMode ViewMode { get; }

        public string? Name => File.Name;

        public IFile File { get; }

        public long Offset { get; }
        public long Size { get; }

        public ViewKind Kind { get; }

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        //This type does not participate in xrefs
        ViewXRefList IView.XRefs => default;

        public ViewImplKind ImplKind => ViewImplKind.File;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter, this);

        public FileAccessor FileAccessor => viewWriter._fileAccessor;

        private ViewWriter viewWriter;
        private IViewable childProvider;

        internal FileView(
            ViewMode viewMode,
            IFile file,
            IView[] children,
            ViewWriter viewWriter,
            ViewKind kind)
        {
            if (viewMode == ViewMode.Default)
                throw new ArgumentException($"ViewMode {viewMode} should have been transformed into a more specific type");

            ViewMode = viewMode;
            File = file;
            Offset = children.Length > 0 ? children[0].Offset : 0;

            if (file.Kind == FileKind.PE)
            {
                var peFile = (PEFile) file;

                //If we're a loaded image, regardless of whether we're pretending to be physical or not,
                //the loaded size of the image is all we have
                if (peFile.IsLoadedImage)
                    Size = peFile.OptionalHeader.SizeOfImage;
                else
                    Size = file.Length;
            }
            else
                Size = file.Length;

            childProvider = new ViewChildProvider<IView>(children);
            this.viewWriter = viewWriter;
            Kind = kind;
        }

        internal FileView(
            ViewMode viewMode,
            IFile file,
            GlobalViewProvider childProvider,
            ViewWriter viewWriter,
            int length,
            ViewKind kind)
        {
            ViewMode = viewMode;
            File = file;
            Offset = 0;
            this.childProvider = childProvider;
            this.viewWriter = viewWriter;
            Kind = kind;
        }

        internal FileView(
            in NestedFileRange range,
            FileAccessor fileAccessor,
            ViewEntityIterator iterator)
        {
            File = range.File;
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

                using var list = new ValueList<IView>(sectionHeaders.Length + 1);

                var sizeOfHeaders = peFile.GetSizeOfHeaders(ViewMode);
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

                childProvider = new ViewChildProvider<IView>(list.ToArray());
                Kind = ViewKind.PEFile;
            }
            else
                throw new NotImplementedException();
        }

        public IView this[int index] => Children[index];

        #region View

        //Gets the logical value associated with the given physical offset. If this address points to the beginning
        //of a struct, the top level struct will be returned. If it points to a field inside a struct, the field will
        //be returned instead
        public IView? GetViewFromOffset(long offset)
        {
            var fileAccessor = viewWriter._fileAccessor;

            if (fileAccessor == null)
                throw new InvalidOperationException("Cannot get view: ViewWriter does not have a FileAccessor");

            if (ViewMode != ViewMode.Physical)
            {
                //Specifying ViewMode.Virtual is only supported with PEFile instances

                if (!((PEFile) File).TryGetRVA((int) offset, out var targetAddress))
                    throw new InvalidOperationException($"Failed to translate offset 0x{offset:X} to an RVA as required by {nameof(PESpy.View.ViewMode)}.{nameof(ViewMode.Virtual)}");

                return fileAccessor.GetView(targetAddress);
            }
            else
            {
                //We want physical and we already have physical

                return fileAccessor.GetView(offset);
            }
        }

        //Gets the logical value associated with the given RVA. If this address points to the beginning
        //of a struct, the top level struct will be returned. If it points to a field inside a struct, the field will
        //be returned instead
        public IView? GetViewFromRVA(int rva)
        {
            var fileAccessor = viewWriter._fileAccessor;

            if (fileAccessor == null)
                throw new InvalidOperationException("Cannot get view: ViewWriter does not have a FileAccessor");

            if (ViewMode != ViewMode.Virtual)
            {
                if (!fileAccessor.TryGetTargetAddress(rva, out var targetAddress, out var sectionIndex))
                    throw new InvalidOperationException($"Failed to translate RVA 0x{rva:X} to a physical offset as required by {nameof(PESpy.View.ViewMode)}.{nameof(ViewMode.Physical)}");

                return fileAccessor.GetView(targetAddress);
            }
            else
            {
                //We want virtual and we already have virtual

                return fileAccessor.GetView(rva);
            }
        }

        //Gets the logical value associated with the given VA. If this address points to the beginning
        //of a struct, the top level struct will be returned. If it points to a field inside a struct, the field will
        //be returned instead
        public IView? GetViewFromVA(long va)
        {
            var fileAccessor = viewWriter._fileAccessor;

            if (fileAccessor == null)
                throw new InvalidOperationException("Cannot get view: ViewWriter does not have a FileAccessor");

            if (va < fileAccessor.ImageBase)
                return null;

            var rva = (int) (va - fileAccessor.ImageBase);

            return GetViewFromRVA(rva);
        }

        #endregion
        #region Parent

        //Gets the parent view associated with the given physical offset. If this address points to a field
        //inside a struct, the immediate (and not top level) parent of that field will be returned instead
        public IView? GetParentFromOffset(long offset)
        {
            var fileAccessor = viewWriter._fileAccessor;

            if (fileAccessor == null)
                throw new InvalidOperationException("Cannot get view: ViewWriter does not have a FileAccessor");

            if (ViewMode != ViewMode.Physical)
            {
                //Specifying ViewMode.Virtual is only supported with PEFile instances

                if (!((PEFile) File).TryGetRVA((int) offset, out var targetAddress))
                    throw new InvalidOperationException($"Failed to translate offset 0x{offset:X} to an RVA as required by {nameof(PESpy.View.ViewMode)}.{nameof(ViewMode.Virtual)}");

                return fileAccessor.GetParentView(targetAddress);
            }
            else
            {
                //We want physical and we already have physical

                return fileAccessor.GetParentView(offset);
            }
        }

        //Gets the parent view associated with the given RVA. If this address points to a field
        //inside a struct, the immediate (and not top level) parent of that field will be returned instead
        public IView? GetParentFromRVA(int rva)
        {
            var fileAccessor = viewWriter._fileAccessor;

            if (fileAccessor == null)
                throw new InvalidOperationException("Cannot get view: ViewWriter does not have a FileAccessor");

            if (ViewMode != ViewMode.Virtual)
            {
                if (!fileAccessor.TryGetTargetAddress(rva, out var targetAddress, out var sectionIndex))
                    throw new InvalidOperationException($"Failed to translate RVA 0x{rva:X} to a physical offset as required by {nameof(PESpy.View.ViewMode)}.{nameof(ViewMode.Physical)}");

                return fileAccessor.GetParentView(targetAddress);
            }
            else
            {
                //We want virtual and we already have virtual

                return fileAccessor.GetParentView(rva);
            }
        }

        //Gets the parent view associated with the given VA. If this address points to a field
        //inside a struct, the immediate (and not top level) parent of that field will be returned instead
        public IView? GetParentFromVA(long va)
        {
            var fileAccessor = viewWriter._fileAccessor;

            if (fileAccessor == null)
                throw new InvalidOperationException("Cannot get view: ViewWriter does not have a FileAccessor");

            if (va < fileAccessor.ImageBase)
                return null;

            var rva = (int) (va - fileAccessor.ImageBase);

            return GetParentFromRVA(rva);
        }

        #endregion

        public IEnumerator<IView> GetEnumerator() => ((IEnumerable<IView>) Children).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitFile(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitFile(this);

        public override string ToString()
        {
            return ViewFormatter.FormatFile(this);
        }
    }
}
