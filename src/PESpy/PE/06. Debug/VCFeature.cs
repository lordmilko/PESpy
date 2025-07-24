using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the VCFeature debug directory.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public class VCFeature : IValue, IViewable //This will always be boxed, so no point being a struct
    {
        //Field names are based on the names listed with dumpbin

#if PEFAST
        public int PreVC11 => chunk.PeekInt32(0);
#else
        public int PreVC11 { get; }
#endif

#if PEFAST
        public int C_CPP => chunk.PeekInt32(4);
#else
        public int C_CPP { get; } //C/C++
#endif

#if PEFAST
        public int GS => chunk.PeekInt32(8);
#else
        public int GS { get; }
#endif

#if PEFAST
        public int SDL => chunk.PeekInt32(12);
#else
        public int SDL { get; }
#endif

#if PEFAST
        public int GuardN => chunk.PeekInt32(16);
#else
        public int GuardN { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //PreVC11
            sizeof(int) + //C_CPP
            sizeof(int) + //GS
            sizeof(int) + //SDL
            sizeof(int); //GuardN

#if PEFAST
        private readonly MemoryChunk chunk;

        internal VCFeature(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal VCFeature(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            PreVC11 = reader.ReadInt32();
            C_CPP = reader.ReadInt32();
            GS = reader.ReadInt32();
            SDL = reader.ReadInt32();
            GuardN = reader.ReadInt32();
        }
#endif

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

            return s.ToArray();
        }
    }
}
