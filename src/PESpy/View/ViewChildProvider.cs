using System;

namespace PESpy.View
{
    internal class ViewChildProvider : IViewable
    {
        private IView[] children;

        public void WriteGlobals(ViewWriter writer) => throw new NotImplementedException();

        public IView? WriteStruct(ViewWriter writer) => throw new NotImplementedException();

        internal ViewChildProvider(IView[] children)
        {
            this.children = children;
        }

        public int NumChildren => children.Length;

        public void WriteChild(int index, ref StructWriter structWriter)
        {
            structWriter.Field = children[index];
        }
    }
}
