using System;
using System.Diagnostics;
using System.Text;
using PESpy.View;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy
{
    /* winnt.h defines several types of runtime functions
     * 
     * IMAGE_CE_RUNTIME_FUNCTION_ENTRY
     * IMAGE_ARM_RUNTIME_FUNCTION_ENTRY
     * IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY
     * IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA
     * IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA_EXTENDED
     * IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA_EPILOG_SCOPE
     * IMAGE_ALPHA64_RUNTIME_FUNCTION_ENTRY
     * IMAGE_ALPHA_RUNTIME_FUNCTION_ENTRY
     * _IMAGE_RUNTIME_FUNCTION_ENTRY (x64 and IA64)
     * 
     * The RUNTIME_FUNCTION typedef is then aliased to one of these based on the architecture that is being
     * targeted
     */

    /// <summary>
    /// Represents the AMD64 <see cref="RUNTIME_FUNCTION"/> structure that provides information on how an 64-bit stack frame should be unwound.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public struct RuntimeFunction : IViewableValue
    {
        private string DebuggerDisplay()
        {
            var builder = new StringBuilder();

            var unwindData = UnwindData;

            if (unwindData.IsValid)
            {
                builder.Append("[");
                builder.Append(unwindData.Value.ExceptionHandlerKind);

                builder.Append("] ");
            }

            builder.Append($"BeginAddress = 0x{BeginAddress:X}, EndAddress = 0x{EndAddress:X}");

            return builder.ToString();
        }

        internal const int BeginAddressOffset = 0;
        internal const int EndAddressOffset = 4;
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
                    if (peFile.FileHeader.Machine == IMAGE_FILE_MACHINE_AMD64 && //I think UnwindInfo only applies for AMD64
                        !(rva >= exceptionTableDirectory.VirtualAddress &&
                        rva <= (exceptionTableDirectory.VirtualAddress + exceptionTableDirectory.Size)) &&
                        peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var data = new UnwindInfo(valueChunk, BeginAddress);

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
            var structOffset = Offset;

            writer.WriteRVAXRef(structOffset, BeginAddressOffset, BeginAddress);
            writer.WriteRVAXRef(structOffset, EndAddressOffset, EndAddress);
            writer.WriteRVAField(UnwindData, structOffset, fieldOffset: UnwindDataOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.RuntimeFunction, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //Note that when constructing a ViewByte model, we don't write RuntimeFunction directly;
            //we access the raw pointer memory for improved performance iterating the list

            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(BeginAddress), BeginAddressOffset, BeginAddress, FieldViewFlags.Address);
                    break;

                case 1:
                    structWriter.WriteField(nameof(EndAddress), EndAddressOffset, EndAddress, FieldViewFlags.Address);
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
