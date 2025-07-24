using PESpy.View;

namespace PESpy
{
    public class ClrEngineMetrics : IValue, IViewable
    {
#if PEFAST
        public int Size => chunk.PeekInt32(0);
#else
        public int Size { get; }
#endif
#if PEFAST
        public int DbiVersion => chunk.PeekInt32(4);
#else
        public int DbiVersion { get; }
#endif
#if PEFAST
        public ulong ContinueStartupEvent => chunk.PeekPointer(8);
#else
        public ulong ContinueStartupEvent { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

        internal static int StructSize(bool is32Bit) =>
            sizeof(int) + //Size
            sizeof(int) + //DbiVersion
            (is32Bit ? 4 : 8); //ContinueStartupEvent

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ClrEngineMetrics(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ClrEngineMetrics(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            Size = reader.ReadInt32();
            DbiVersion = reader.ReadInt32();

            ContinueStartupEvent = peFile.OptionalHeader.Magic == PEMagic.PE32
                ? reader.ReadUInt32()
                : reader.ReadUInt64();
        }
#endif

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
