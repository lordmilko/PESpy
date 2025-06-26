using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the AMD64 <see cref="RUNTIME_FUNCTION"/> structure that provides information on how an 64-bit stack frame should be unwound.
    /// </summary>
    [DebuggerDisplay("BeginAddress = 0x{BeginAddress.ToString(\"X\"),nq}, EndAddress = 0x{EndAddress.ToString(\"X\"),nq}")] //I had issues with my ReadyToRunHeader_Test wherein when an exception occurs trying to resolve the UnwindData, I start getting NullReferenceException errors in the Visual Studio debugger trying to inspect a RuntimeFunction object. So I'm not including the UnwindData in the DebuggerDisplay
    public struct RuntimeFunction : IValue, IViewable
    {
#if PEFAST
        public int BeginAddress => chunk.PeekInt32(0);
#else
        public int BeginAddress { get; }
#endif

#if PEFAST
        public int EndAddress => chunk.PeekInt32(4);
#else
        public int EndAddress { get; }
#endif

#if PEFAST
        private RVA<UnwindInfo> unwindData;

        public RVA<UnwindInfo> UnwindData
        {
            get
            {
                if (unwindData.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(8);

                    var peFile = chunk.PEFile();
                    var exceptionTableDirectory = peFile.OptionalHeader.ExceptionTableDirectory;

                    /* Some modules, such as sqlncli11 and mscorlib.ni have bizarre values in their RUNTIME_FUNCTION.UnwindData that point exactly 1 byte
                     * into another RUNTIME_FUNCTION entry. I don't understand what causes this. It's clearly not a one-off, because mscorlib.ni has a whole
                     * stream of them one after the other. Given that the exception directory is supposed to purely be comprised of RUNTIME_FUNCTION entries,
                     * if we see an UnwindData that lies within the bounds of the exception directory, we'll assume it's one of these anomalous entries, and
                     * mark it as bad (since it's clearly not pointing to an UnwindInfo) */
                    if (!(rva >= exceptionTableDirectory.VirtualAddress &&
                        rva <= (exceptionTableDirectory.VirtualAddress + exceptionTableDirectory.Size)) &&
                        peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var data = new UnwindInfo(valueChunk);

                        unwindData = new RVA<UnwindInfo>(rva, valueChunk.AbsoluteOffset, data);
                    }
                    else
                        unwindData = new RVA<UnwindInfo>(rva);
                }

                return unwindData;
            }
        }
#else
        public RVA<UnwindInfo> UnwindData { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        public const int StructSize =
            sizeof(int) + //BeginAddress
            sizeof(int) + //EndAddress
            sizeof(int);  //UnwindData

#if PEFAST
        private readonly MemoryChunk chunk;

        internal RuntimeFunction(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindData = default;

#if STRESS_TEST
            _ = UnwindData;
#endif
        }
#else
        internal RuntimeFunction(IFileReader reader, PEFile peFile, in ImageDataDirectory exceptionDirectory, ExceptionHandlerContext context)
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

                var data = new UnwindInfo(reader, peFile, exceptionDirectory, context);

                UnwindData = new RVA<UnwindInfo>(unwindData, offset, data);
            }
            else
                UnwindData = new RVA<UnwindInfo>(unwindData);
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(UnwindData);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(RUNTIME_FUNCTION), this, ViewKind.RuntimeFunction, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(BeginAddress), BeginAddress);
            s.WriteField(nameof(EndAddress), EndAddress);
            s.WriteRVAField(nameof(UnwindData), UnwindData);

            return s.ToArray();
        }
    }
}
