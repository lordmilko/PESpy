using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("off = {off}, cb = {cb}")]
    public struct OffCb : IViewable
    {
        public int off;
        public int cb;

        internal const int StructSize =
            sizeof(int) + //off
            sizeof(int);  //cb

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.OffCb, this, ViewKind.OffCb, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter writer)
        {
            using var s = writer.CreateStruct(parent);

            s.WriteField(nameof(off), off);
            s.WriteField(nameof(cb), cb);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
