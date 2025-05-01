using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct StateMachineMethodRow : IValue, IViewable
    {
        public int MoveNextMethod { get; }

        public int KickoffMethod { get; }

        public RawOffset Offset { get; }

        internal static StateMachineMethodRow New(MetadataReader metadataReader) => new StateMachineMethodRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) + //MoveNextMethod
            sizeof(int);  //KickoffMethod

        internal StateMachineMethodRow(MetadataReader metadataReader)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#statemachinemethod-table-0x36

            Offset = (RawOffset) metadataReader.Position;

            MoveNextMethod = metadataReader.ReadInt32();
            KickoffMethod = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("StateMachineMethod Row", this, ViewKind.PortablePdb_StateMachineMethodRow);

            s.WriteValue(nameof(MoveNextMethod), MoveNextMethod);
            s.WriteValue(nameof(KickoffMethod), KickoffMethod);
        }
    }
}
