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

        public Timestamp Date => (uint) chunk.PeekSpacePaddedInt32(DateOffset, 12 - dateMissing); //Should parse as number

        public int? UserID => chunk.PeekSpacePaddedNullableInt32(UserIDOffset - dateMissing, 6); //Should be a decimal

        public int? GroupID => chunk.PeekSpacePaddedNullableInt32(GroupIDOffset - dateMissing, 6); //Should be a decimal

        public int? Mode => chunk.PeekSpacePaddedNullableInt32(ModeOffset - dateMissing, 8, @base: 8); //Should be octal

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
        internal readonly int dateMissing;

        internal unsafe ImageArchiveMemberHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            dateMissing = 0; //How many bytes are missing from the date

            //In Windows 3.1 library files, the field alignment is very messed up. The Date field is only 10 bytes (9 characters followed by space)
            //instead of the required 12, which messes up the alignment of every field after it, up to right before the Size, where a bogus " 0" is
            //inserted to get things back on track so the Size is correctly aligned

            var dateSpan = chunk.PeekAnsiFixedLength(DateOffset, 12 - dateMissing).AsSpan();

            //If there are any characters after the first space, we've got a bogus date
            var firstSpace = dateSpan.IndexOf((byte) ' ');

            if (firstSpace != -1)
            {
#if NETSTANDARD
                var firstNonSpace = -1;

                for (var i = firstSpace + 1; i < dateSpan.Length; i++)
                {
                    if (dateSpan[i] != ' ')
                    {
                        firstNonSpace = i;
                        break;
                    }
                }
#else
                var firstNonSpace = dateSpan.Slice(firstSpace + 1).IndexOfAnyExcept((byte) ' ');

                if (firstNonSpace != -1)
                    firstNonSpace += firstSpace + 1;
#endif

                if (firstNonSpace != -1)
                {
                    //We can't trust the date
                    dateMissing = 12 - firstNonSpace;
                }
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageArchiveMemberHeader, StructSize);

        int IViewable.NumChildren() => dateMissing == 0 ? 7 : 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteAnsiFixedLengthField(nameof(Name), NameOffset, Name);
                    break;

                case 1:
                    structWriter.WriteAnsiFixedLengthField(nameof(Date), DateOffset, chunk.PeekAnsiFixedLength(DateOffset, 12 - dateMissing));
                    break;

                case 2:
                    structWriter.WriteAnsiFixedLengthField(nameof(UserID), UserIDOffset - dateMissing, chunk.PeekAnsiFixedLength(UserIDOffset - dateMissing, 6));
                    break;

                case 3:
                    structWriter.WriteAnsiFixedLengthField(nameof(GroupID), GroupIDOffset - dateMissing, chunk.PeekAnsiFixedLength(GroupIDOffset - dateMissing, 6));
                    break;

                case 4:
                    structWriter.WriteAnsiFixedLengthField(nameof(Mode), ModeOffset - dateMissing, chunk.PeekAnsiFixedLength(ModeOffset - dateMissing, 8));
                    break;

                case 5:
                    if (dateMissing == 0)
                        structWriter.WriteAnsiFixedLengthField(nameof(Size), SizeOffset, chunk.PeekAnsiFixedLength(SizeOffset, 10));
                    else
                        structWriter.WriteByteBlob(SizeOffset - dateMissing, dateMissing);
                    break;

                case 6:
                    if (dateMissing == 0)
                        structWriter.WriteAnsiFixedLengthField(nameof(EndHeader), EndHeaderOffset, EndHeader);
                    else
                        structWriter.WriteAnsiFixedLengthField(nameof(Size), SizeOffset, chunk.PeekAnsiFixedLength(SizeOffset, 10));
                    break;

                case 7:
                    if (dateMissing == 0)
                        throw new IndexOutOfRangeException();

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
