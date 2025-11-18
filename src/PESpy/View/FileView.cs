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

        public IEnumerator<IView> GetEnumerator() => ((IEnumerable<IView>) Children).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitFile(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitFile(this);
    }
}
