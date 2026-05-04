using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="Native.CLR_ENGINE_METRICS"/> type used to provide
    /// debuggers with the <see cref="CorDebugInterfaceVersion"/> and location of the g_hContinueStartupEvent
    /// event required to attach a debugger during early .NET Core process startup.<para/>
    /// This data structure is exported as "g_CLREngineMetrics" at ordinal 2 in coreclr.dll and
    /// the single file apphost (singlefileapp.exe)
    /// </summary>
    [Source(SourceKind.dbgenginemetrics_h)]
    public class ClrEngineMetrics : IValue, IViewable
    {
        private const int SizeOffset = 0;
        private const int DbiVersionOffset = 4;
        private const int ContinueStartupEventOffset = 8;

        public int Size => chunk.PeekInt32(SizeOffset);
        public CorDebugInterfaceVersion DbiVersion => (CorDebugInterfaceVersion) chunk.PeekUInt32(DbiVersionOffset);
        public ulong ContinueStartupEvent => chunk.PeekPointer(ContinueStartupEventOffset);

        public long Offset => chunk.AbsoluteOffset;

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
            writer.NewStruct(this, ViewKind.ClrEngineMetrics, StructSize(writer.Is32Bit));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Size), SizeOffset, Size);
                    break;

                case 1:
                    structWriter.WriteField(nameof(DbiVersion), DbiVersionOffset, DbiVersion, sizeof(uint));
                    break;

                case 2:
                    structWriter.WritePointerField(nameof(ContinueStartupEvent), ContinueStartupEventOffset, ContinueStartupEvent);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
