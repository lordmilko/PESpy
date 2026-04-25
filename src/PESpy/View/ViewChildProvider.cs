using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy.View
{
    internal class ViewChildProvider<T> : IViewable where T : IView
    {
        internal T[] _children;

        public void WriteGlobals(ViewWriter writer) => throw new NotImplementedException();

        public IView? WriteStruct(ViewWriter writer) => throw new NotImplementedException();

        internal ViewChildProvider(T[] children)
        {
            _children = children;
        }

        public int NumChildren() => _children.Length;

        public void WriteChild(int index, ref StructWriter structWriter)
        {
            structWriter.Field = _children[index];
        }
    }
}
