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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("CLR_ENGINE_METRICS", this, ViewKind.GlobalValueEntry);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(DbiVersion), DbiVersion);
            s.WritePointerField(nameof(ContinueStartupEvent), ContinueStartupEvent);
        }
    }
}
