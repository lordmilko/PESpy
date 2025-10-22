using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the AMD64 <see cref="RUNTIME_FUNCTION"/> structure that provides information on how an 64-bit stack frame should be unwound.
    /// </summary>
    [DebuggerDisplay("BeginAddress = 0x{BeginAddress.ToString(\"X\"),nq}, EndAddress = 0x{EndAddress.ToString(\"X\"),nq}")] //I had issues with my ReadyToRunHeader_Test wherein when an exception occurs trying to resolve the UnwindData, I start getting NullReferenceException errors in the Visual Studio debugger trying to inspect a RuntimeFunction object. So I'm not including the UnwindData in the DebuggerDisplay
    public struct RuntimeFunction : IValue, IViewable
    {
        private const int BeginAddressOffset = 0;
        private const int EndAddressOffset = 4;
        internal const int UnwindDataOffset = 8;

        public int BeginAddress => chunk.PeekInt32(BeginAddressOffset);

        public int EndAddress => chunk.PeekInt32(EndAddressOffset);

        private RVA<UnwindInfo> unwindData;

        public RVA<UnwindInfo> UnwindData
        {
            get
            {
                if (unwindData.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(UnwindDataOffset);

                    var peFile = chunk.PEFile();
                    var exceptionTableDirectory = peFile.OptionalHeader.ExceptionTableDirectory;

                    /* Some modules, such as sqlncli11 and mscorlib.ni have bizarre values in their RUNTIME_FUNCTION.UnwindData that point exactly 1 byte
                     * into another RUNTIME_FUNCTION entry. I don't understand what causes this. It's clearly not a one-off, because mscorlib.ni has a whole
                     * stream of them one after the other. Given that the exception directory is supposed to purely be comprised of RUNTIME_FUNCTION entries,
                     * if we see an UnwindData that lies within the bounds of the exception directory, we'll assume it's one of these anomalous entries, and
                     * mark it as bad (since it's clearly not pointing to an UnwindInfo) */
                    if (peFile.FileHeader.Machine == ClrDebug.IMAGE_FILE_MACHINE.AMD64 && //I think UnwindInfo only applies for AMD64
                        !(rva >= exceptionTableDirectory.VirtualAddress &&
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

        public int Offset => chunk.AbsoluteOffset;

        public const int StructSize =
            sizeof(int) + //BeginAddress
            sizeof(int) + //EndAddress
            sizeof(int);  //UnwindData

        private readonly MemoryChunk chunk;

        internal RuntimeFunction(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindData = default;

#if STRESS_TEST
            _ = UnwindData;
#endif
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(UnwindData, fieldOffset: UnwindDataOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.RUNTIME_FUNCTION, this, ViewKind.RuntimeFunction, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(BeginAddress), BeginAddressOffset, BeginAddress);
                    break;

                case 1:
                    structWriter.WriteField(nameof(EndAddress), EndAddressOffset, EndAddress);
                    break;

                case 2:
                    structWriter.WriteRVAField(nameof(UnwindData), UnwindDataOffset, UnwindData);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
