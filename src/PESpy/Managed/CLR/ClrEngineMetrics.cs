using PESpy.View;

namespace PESpy
{
    public class ClrEngineMetrics : IValue, IViewable
    {
        public int Size { get; }
        public int DbiVersion { get; }
        public long ContinueStartupEvent { get; }

        public int Offset { get; }

        internal ClrEngineMetrics(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            Size = reader.ReadInt32();
            DbiVersion = reader.ReadInt32();

            ContinueStartupEvent = peFile.OptionalHeader.Magic == PEMagic.PE32
                ? reader.ReadInt32()
                : reader.ReadInt64();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("CLR_ENGINE_METRICS", this, ViewKind.GlobalValueEntry);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(DbiVersion), DbiVersion);
            s.WritePointerField(nameof(ContinueStartupEvent), ContinueStartupEvent);
        }
    }
}
