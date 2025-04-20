using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    public class OBJFileView : IContainerView, IEnumerable<IView>
    {
        public int Offset { get; }
        public int Size { get; }
        public ViewKind Kind => default; //todo: make it an objfile

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public OBJFileView(IView[] children)
        {
            Offset = children[0].Offset;
            Size = children.Sum(r => r.Size);
            Children = children;
        }

        public IEnumerator<IView> GetEnumerator() => ((IEnumerable<IView>) Children).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //todo: this is no good, need a different iview interface for obj i think
        public T Accept<T>(PEViewVisitor<T> visitor) => throw new System.NotImplementedException();

        public void Accept(PEViewVisitor visitor) => throw new System.NotImplementedException();
    }
}
