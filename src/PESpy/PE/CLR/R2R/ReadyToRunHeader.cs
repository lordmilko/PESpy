using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using PESpy.View;

//This is in a namespace to distinguish this type from the NativeAOT types which share similar names
namespace PESpy.R2R
{
    //The types for R2R are defined in readytorun.h. However, confusingly there is a separate but related type called "ReadyToRunheader" defined in ModuleHeaders.h, which
    //has a slightly different layout for Native AOT: there's no CoreHeader; instead, all of the fields are defined inline, and NumberOfSections is a short instead of an int,
    //and there are two extra fields EntrySize and EntryType
    //EntryType is always 1, I haven't been able to figure out what it means
    //https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/aot/ILCompiler.Compiler/Compiler/DependencyAnalysis/ReadyToRunHeaderNode.cs#L65

    //READYTORUN_HEADER
    public class ReadyToRunHeader : IValue, IViewable
    {
        internal const int R2RSignature = 0x00525452; //R2R
        private const int SignatureOffset = 0;
        private const int MajorVersionOffset = 4;
        private const int MinorVersionOffset = 6;
        private const int CoreHeaderOffset = 8;

        public int Signature => chunk.PeekInt32(SignatureOffset);
        public short MajorVersion => chunk.PeekInt16(MajorVersionOffset);
        public short MinorVersion => chunk.PeekInt16(MinorVersionOffset);
        public ReadyToRunCoreHeader CoreHeader => new ReadyToRunCoreHeader(chunk.Slice(CoreHeaderOffset));

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) +
            sizeof(short) +
            sizeof(short);

        public int StructSize => FixedStructSize + CoreHeader.StructSize;

        private readonly MemoryChunk chunk;

        internal ReadyToRunHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ReadyToRunHeader, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Signature), SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteField(nameof(MajorVersion), MajorVersionOffset, MajorVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MinorVersion), MinorVersionOffset, MinorVersion);
                    break;

                case 3:
                    structWriter.WriteStructField(nameof(CoreHeader), CoreHeader);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
