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

        public Int32 Offset { get; }
        public int Size { get; }

        public ViewKind Kind { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public FileView(ViewMode viewMode, IView[] children, ViewKind kind)
        {
            if (viewMode == ViewMode.Default)
                throw new ArgumentException($"ViewMode {viewMode} should have been transformed into a more specific type");

            ViewMode = viewMode;

            Offset = children[0].Offset;
            Size = children.Sum(r => r.Size);
            Children = children;
            Kind = kind;
        }

        public IEnumerator<IView> GetEnumerator() => ((IEnumerable<IView>) Children).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitFile(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitFile(this);
    }
}
