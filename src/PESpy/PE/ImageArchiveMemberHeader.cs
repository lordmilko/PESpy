using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    //Note: we are not currently parsing these members properly
    public readonly struct ImageArchiveMemberHeader : IValue, IViewable
    {
        private const int NameOffset = 0;
        private const int DateOffset = 16;
        private const int UserIDOffset = 28;
        private const int GroupIDOffset = 34;
        private const int ModeOffset = 40;
        private const int SizeOffset = 48;
        private const int EndHeaderOffset = 58;

        public FixedAnsiString Name => chunk.PeekAnsiFixedLength(NameOffset, 16);

        public FixedAnsiString Date => chunk.PeekAnsiFixedLength(DateOffset, 12); //Should parse as number

        public FixedAnsiString UserID => chunk.PeekAnsiFixedLength(UserIDOffset, 6); //Should be a decimal

        public FixedAnsiString GroupID => chunk.PeekAnsiFixedLength(GroupIDOffset, 6); //Should be a decimal

        public FixedAnsiString Mode => chunk.PeekAnsiFixedLength(ModeOffset, 8); //Should be octal

        //The size of the data following this ImageArchiveMemberHeader. The total size of the member is Size + sizeof(IMAGE_ARCHIVE_MEMBER_HEADER)
        public int Size => chunk.PeekSpacePaddedInt32(SizeOffset, 10);

        public FixedAnsiString EndHeader => chunk.PeekAnsiFixedLength(EndHeaderOffset, 2);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_ARCHIVE_MEMBER_HEADER, this, ViewKind.ImageArchiveMemberHeader, StructSize);

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteAnsiFixedLengthField(nameof(Name), NameOffset, Name);
                    break;

                case 1:
                    structWriter.WriteAnsiFixedLengthField(nameof(Date), DateOffset, Date);
                    break;

                case 2:
                    structWriter.WriteAnsiFixedLengthField(nameof(UserID), UserIDOffset, UserID);
                    break;

                case 3:
                    structWriter.WriteAnsiFixedLengthField(nameof(GroupID), GroupIDOffset, GroupID);
                    break;

                case 4:
                    structWriter.WriteAnsiFixedLengthField(nameof(Mode), ModeOffset, Mode);
                    break;

                case 5:
                    structWriter.WriteAnsiFixedLengthField(nameof(Size), SizeOffset, chunk.PeekAnsiFixedLength(48, 10));
                    break;

                case 6:
                    structWriter.WriteAnsiFixedLengthField(nameof(EndHeader), EndHeaderOffset, EndHeader);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
