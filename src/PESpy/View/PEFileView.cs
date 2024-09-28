using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    [DebuggerDisplay("{ViewDebuggerDisplay.PEFile(this),nq}")]
    public class PEFileView : IContainerView, IEnumerable<IView>
    {
        public ViewMode ViewMode { get; }

        public RawOffset Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.PEFile;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        internal PEFileView(IView[] children, ViewMode viewMode)
        {
            if (viewMode == ViewMode.Default)
                throw new ArgumentException($"ViewMode {viewMode} should have been transformed into a more specific type");

            Offset = children[0].Offset;
            Size = children.Sum(r => r.Size);
            Children = children;
            ViewMode = viewMode;
        }

        public IEnumerator<IView> GetEnumerator() => ((IEnumerable<IView>) Children).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitPEFile(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitPEFile(this);
    }
}
