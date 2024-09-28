using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the AMD64 <see cref="RUNTIME_FUNCTION"/> structure.
    /// </summary>
    [DebuggerDisplay("BeginAddress = 0x{BeginAddress.ToString(\"X\"),nq}, EndAddress = 0x{EndAddress.ToString(\"X\"),nq}, UnwindData = 0x{UnwindData.ListedOffset.ToString(\"X\"),nq}")]
    public readonly struct RuntimeFunction : IValue, IViewable
    {
        public int BeginAddress { get; }

        public int EndAddress { get; }

        public RVA<UnwindInfo> UnwindData { get; }

        public RawOffset Offset { get; }

        public const int StructSize =
            sizeof(int) + //BeginAddress
            sizeof(int) + //EndAddress
            sizeof(int);  //UnwindData

        internal RuntimeFunction(ref FileReader reader, PEFile peFile, in ImageDataDirectory exceptionDirectory, ExceptionHandlerContext context)
        {
            Offset = (RawOffset) reader.Position;

            BeginAddress = reader.ReadInt32();
            EndAddress = reader.ReadInt32();

            var unwindData = (RVA) reader.ReadInt32();

            //Some modules, such as sqlncli11 and mscorlib.ni have bizarre values in their RUNTIME_FUNCTION.UnwindData that point exactly 1 byte
            //into another RUNTIME_FUNCTION entry. I don't understand what causes this. It's clearly not a one-off, because mscorlib.ni has a whole
            //stream of them one after the other. Given that the exception directory is supposed to purely be comprised of RUNTIME_FUNCTION entries,
            //if we see an UnwindData that lies within the bounds of the exception directory, we'll assume it's one of these anomalous entries, and
            //mark it as bad (since it's clearly not pointing to an UnwindInfo)
            if (!(unwindData >= exceptionDirectory.VirtualAddress && unwindData <= (exceptionDirectory.VirtualAddress + exceptionDirectory.Size)) && peFile.TryGetOffset(unwindData, out var offset))
            {
                reader.Seek(offset);

                var data = new UnwindInfo(ref reader, peFile, exceptionDirectory, context);

                UnwindData = new RVA<UnwindInfo>(unwindData, offset, data);
            }
            else
                UnwindData = new RVA<UnwindInfo>(unwindData);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(RUNTIME_FUNCTION), this, ViewKind.RuntimeFunction);

            s.WriteField(nameof(BeginAddress), BeginAddress);
            s.WriteField(nameof(EndAddress), EndAddress);
            s.WriteRVAField(nameof(UnwindData), UnwindData);
        }
    }
}
