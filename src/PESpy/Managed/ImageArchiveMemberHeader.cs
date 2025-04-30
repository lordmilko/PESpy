using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    //Note: we are not currently parsing these members properly
    public readonly struct ImageArchiveMemberHeader : IValue, IViewable
    {
        public FixedAnsiString Name => chunk.PeekAnsiFixedLength(0, 16);

        public FixedAnsiString Date => chunk.PeekAnsiFixedLength(16, 12); //Should parse as number

        public FixedAnsiString UserID => chunk.PeekAnsiFixedLength(28, 6); //Should be a decimal

        public FixedAnsiString GroupID => chunk.PeekAnsiFixedLength(34, 6); //Should be a decimal

        public FixedAnsiString Mode => chunk.PeekAnsiFixedLength(40, 8); //Should be octal

        public int Size => chunk.PeekSpacePaddedInt32(48, 10);

        public FixedAnsiString EndHeader => chunk.PeekAnsiFixedLength(58, 2);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            16 + //Name
            12 + //Date
            6 +  //UserID
            6 +  //GroupID
            8 +  //Mode
            10 + //Size
            2;   //EndHeader

        private readonly MemoryChunk chunk;

        internal ImageArchiveMemberHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_ARCHIVE_MEMBER_HEADER), this, ViewKind.ImageArchiveMemberHeader);

            s.WriteAnsiFixedLengthField(nameof(Name), Name);
            s.WriteAnsiFixedLengthField(nameof(Date), Date);
            s.WriteAnsiFixedLengthField(nameof(UserID), UserID);
            s.WriteAnsiFixedLengthField(nameof(GroupID), GroupID);
            s.WriteAnsiFixedLengthField(nameof(Mode), Mode);
            s.WriteAnsiFixedLengthField(nameof(Size), chunk.PeekAnsiFixedLength(48, 10));
            s.WriteAnsiFixedLengthField(nameof(EndHeader), EndHeader);
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
