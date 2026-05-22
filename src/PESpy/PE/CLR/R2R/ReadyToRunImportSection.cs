using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

//This is in a namespace to distinguish this type from the NativeAOT types which share similar names
namespace PESpy.R2R
{
    [DebuggerDisplay("Flags = {Flags}, Type = {Type}")]
    public readonly struct ReadyToRunImportSection : IValue, IViewable
    {
        private const int SectionOffset = 0;
        private const int FlagsOffset = 8;
        private const int TypeOffset = 10;
        private const int EntrySizeOffset = 11;
        private const int SignaturesOffset = 12;
        private const int AuxiliaryDataOffset = 16;

        public ImageDataDirectory Section => new ImageDataDirectory(chunk.Slice(SectionOffset));
        public ReadyToRunImportSectionFlags Flags => (ReadyToRunImportSectionFlags) chunk.PeekUInt16(FlagsOffset);
        public ReadyToRunImportSectionType Type => (ReadyToRunImportSectionType) chunk.PeekByte(TypeOffset);
        public byte EntrySize => chunk.PeekByte(EntrySizeOffset);
        public int Signatures => chunk.PeekInt32(SignaturesOffset);
        public int AuxiliaryData => chunk.PeekInt32(AuxiliaryDataOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(long) +  //Section
            sizeof(short) + //Flags
            sizeof(byte) +  //Type
            sizeof(byte) +  //EntrySize
            sizeof(int) +   //Signatures
            sizeof(int);    //AuxiliaryData

        private readonly MemoryChunk chunk;

        internal ReadyToRunImportSection(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ReadyToRunImportSection, StructSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteStructField(nameof(Section), Section);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags, sizeof(short));
                    break;

                case 2:
                    structWriter.WriteField(nameof(Type), TypeOffset, Type, sizeof(byte));
                    break;

                case 3:
                    structWriter.WriteField(nameof(EntrySize), EntrySizeOffset, EntrySize);
                    break;

                case 4:
                    structWriter.WriteField(nameof(Signatures), SignaturesOffset, Signatures);
                    break;

                case 5:
                    structWriter.WriteField(nameof(AuxiliaryData), AuxiliaryDataOffset, AuxiliaryData);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
