using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.VB
{
    //Referenced in msvbvm60!_CreateOcxDefFromExe(OCXDEF, EXEOCXINFO)
    //Fields are made up
    [DebuggerDisplay("FileName = {FileNameOffset}, Name = {NameOffset}")]
    public readonly struct ExeOcxInfo : IViewableValue
    {
        private const int dwStructSizeOffset = 0;
        private const int dwUuidOffsetOffset = 4;
        private const int UnknownOffset0Offset = 8;
        private const int UnknownOffset1Offset = 12;
        private const int UnknownOffset2Offset = 16;
        private const int UnknownOffset3Offset = 20;
        private const int Unknownoffset4Offset = 24;
        private const int GUIDoffsetOffset = 28;
        private const int GUIDlengthOffset = 32;
        private const int UnknownOffse5Offset = 36;
        private const int FileNameOffsetOffset = 40;
        private const int SourceOffsetOffset = 44;
        private const int NameOffsetOffset = 48;
        private const int PaddingOffset = 52;

        public int dwStructSize => chunk.PeekInt32(dwStructSizeOffset);
        public int dwUuidOffset => chunk.PeekInt32(dwUuidOffsetOffset);
        public int UnknownOffset0 => chunk.PeekInt32(UnknownOffset0Offset);
        public int UnknownOffset1 => chunk.PeekInt32(UnknownOffset1Offset);
        public int UnknownOffset2 => chunk.PeekInt32(UnknownOffset2Offset);
        public int UnknownOffset3 => chunk.PeekInt32(UnknownOffset3Offset);
        public int Unknownoffset4 => chunk.PeekInt32(Unknownoffset4Offset);
        public int GUIDoffset => chunk.PeekInt32(GUIDoffsetOffset);
        public int GUIDlength => chunk.PeekInt32(GUIDlengthOffset);
        public int UnknownOffse5 => chunk.PeekInt32(UnknownOffse5Offset);
        public RVA<AnsiString> FileNameOffset => ExeProjectInfo.ReadTrailingAnsiString(chunk, FileNameOffsetOffset);
        public int SourceOffset => chunk.PeekInt32(SourceOffsetOffset);
        public RVA<AnsiString> NameOffset => ExeProjectInfo.ReadTrailingAnsiString(chunk, NameOffsetOffset);
        public int Padding => chunk.PeekInt32(PaddingOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //dwStructSize
            sizeof(int) + //dwUuidOffset
            sizeof(int) + //UnknownOffset0
            sizeof(int) + //UnknownOffset1
            sizeof(int) + //UnknownOffset2
            sizeof(int) + //UnknownOffset3
            sizeof(int) + //Unknownoffset4
            sizeof(int) + //GUIDoffset
            sizeof(int) + //GUIDlength
            sizeof(int) + //UnknownOffse5
            sizeof(int) + //FileNameOffset
            sizeof(int) + //SourceOffset
            sizeof(int) + //NameOffset
            sizeof(int); //Padding

        private readonly MemoryChunk chunk;

        internal ExeOcxInfo(MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteRVAAnsiNullTerminatedField(FileNameOffset, ViewKind.AnsiString, offset, FileNameOffsetOffset);
            writer.WriteRVAAnsiNullTerminatedField(NameOffset, ViewKind.AnsiString, offset, NameOffsetOffset);
        }

        //dwStructSize was way bigger than the size we've acocunted for, and collided with the data after this.
        //As such, we won't use dwStructSize here
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ExeOcxInfo, StructSize);

        int IViewable.NumChildren() => 14;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(dwStructSize), dwStructSizeOffset, dwStructSize);
                    break;

                case 1:
                    structWriter.WriteField(nameof(dwUuidOffset), dwUuidOffsetOffset, dwUuidOffset);
                    break;

                case 2:
                    structWriter.WriteField(nameof(UnknownOffset0), UnknownOffset0Offset, UnknownOffset0);
                    break;

                case 3:
                    structWriter.WriteField(nameof(UnknownOffset1), UnknownOffset1Offset, UnknownOffset1);
                    break;

                case 4:
                    structWriter.WriteField(nameof(UnknownOffset2), UnknownOffset2Offset, UnknownOffset2);
                    break;

                case 5:
                    structWriter.WriteField(nameof(UnknownOffset3), UnknownOffset3Offset, UnknownOffset3);
                    break;

                case 6:
                    structWriter.WriteField(nameof(Unknownoffset4), Unknownoffset4Offset, Unknownoffset4);
                    break;

                case 7:
                    structWriter.WriteField(nameof(GUIDoffset), GUIDoffsetOffset, GUIDoffset);
                    break;

                case 8:
                    structWriter.WriteField(nameof(GUIDlength), GUIDlengthOffset, GUIDlength);
                    break;

                case 9:
                    structWriter.WriteField(nameof(UnknownOffse5), UnknownOffse5Offset, UnknownOffse5);
                    break;

                case 10:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(FileNameOffset), FileNameOffsetOffset, FileNameOffset);
                    break;

                case 11:
                    structWriter.WriteField(nameof(SourceOffset), SourceOffsetOffset, SourceOffset);
                    break;

                case 12:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(NameOffset), NameOffsetOffset, NameOffset);
                    break;

                case 13:
                    structWriter.WriteField(nameof(Padding), PaddingOffset, Padding);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
