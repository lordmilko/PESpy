using PESpy.View;

namespace PESpy
{
    public class ClrEngineMetrics : IValue, IViewable
    {
        public int Size => chunk.PeekInt32(0);
        public int DbiVersion => chunk.PeekInt32(4);
        public ulong ContinueStartupEvent => chunk.PeekPointer(8);

        public int Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            sizeof(int) + //Size
            sizeof(int) + //DbiVersion
            (is32Bit ? 4 : 8); //ContinueStartupEvent

        private readonly MemoryChunk chunk;

        internal ClrEngineMetrics(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CLR_ENGINE_METRICS, this, ViewKind.ClrEngineMetrics, StructSize(((PEViewWriter) writer).Is32Bit));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(DbiVersion), DbiVersion);
            s.WritePointerField(nameof(ContinueStartupEvent), ContinueStartupEvent);

            return s.ToArray();
        }
    }
}
