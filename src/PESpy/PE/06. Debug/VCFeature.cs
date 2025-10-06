using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the VCFeature debug directory.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public class VCFeature : IValue, IViewable //This will always be boxed, so no point being a struct
    {
        //Field names are based on the names listed with dumpbin

        public int PreVC11 => chunk.PeekInt32(0);

        public int C_CPP => chunk.PeekInt32(4);

        public int GS => chunk.PeekInt32(8);

        public int SDL => chunk.PeekInt32(12);

        public int GuardN => chunk.PeekInt32(16);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //PreVC11
            sizeof(int) + //C_CPP
            sizeof(int) + //GS
            sizeof(int) + //SDL
            sizeof(int); //GuardN

        private readonly MemoryChunk chunk;

        internal VCFeature(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.VCFeature, this, ViewKind.VCFeature, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(PreVC11), PreVC11);
            s.WriteField(nameof(C_CPP), C_CPP);
            s.WriteField(nameof(GS), GS);
            s.WriteField(nameof(SDL), SDL);
            s.WriteField(nameof(GuardN), GuardN);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
