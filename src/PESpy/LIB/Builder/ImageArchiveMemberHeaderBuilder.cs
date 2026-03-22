using System;
using System.Collections.Generic;
using PESpy.Native;

namespace PESpy
{
    internal enum ArchiveMemberKind
    {
        FirstLinkerMember,
        SecondLinkerMember,
        LongNamesMember,
        ImportMember
    }

    internal class ImageArchiveMemberHeaderBuilder
    {
        public string Name { get; set; }

        public Timestamp Date { get; set; }

        public int? UserID { get; set; }

        public int? GroupID { get; set; }

        public int? Mode { get; set; }

        //Size is not exposed; we'll compute this during serialization

        public ArchiveMemberKind Kind { get; }

        internal int DateMissing { get; set; }

        internal ImageArchiveMemberHeaderBuilder(ImageArchiveMemberHeader imageArchiveHeader, ArchiveMemberKind kind)
        {
            switch (kind)
            {
                case ArchiveMemberKind.ImportMember:
                    Name = imageArchiveHeader.Name.ToString().TrimEnd(' ', '/');
                    break;

                default:
                    Name = imageArchiveHeader.Name.ToString();
                    break;
            }
            
            Date = imageArchiveHeader.Date;
            UserID = imageArchiveHeader.UserID;
            GroupID = imageArchiveHeader.GroupID;
            Mode = imageArchiveHeader.Mode;
            Kind = kind;
            DateMissing = imageArchiveHeader.dateMissing;

            //Size is not exposed
        }

        public ImageArchiveMemberHeaderBuilder(ArchiveMemberKind kind)
        {
            Kind = kind;
        }

        //Returns the position at which size should be written
        public void WriteTo(FileWriter writer, int size, Dictionary<string, int> longNamesMap)
        {
            switch (Kind)
            {
                case ArchiveMemberKind.ImportMember:
                    if (Name.Length <= 15) //The name has to end in /
                        writer.WriteSpacePaddedAnsiString(Name + "/", 16);
                    else
                        writer.WriteSpacePaddedAnsiString("/" + longNamesMap[Name], 16);

                    break;

                default:
                    writer.WriteSpacePaddedAnsiString(Name, 16);
                    break;
            }

            writer.WriteSpacePaddedInt32((int) (uint) Date, 12 - DateMissing);
            writer.WriteSpacePaddedInt32(UserID, 6);
            writer.WriteSpacePaddedInt32(GroupID, 6);
            writer.WriteSpacePaddedInt32(Mode, 8, @base: 8);

            if (DateMissing > 0)
                writer.WriteSpacePaddedInt32(0, 2);

            //In the one example I had, when DateMissing was 2, the size occupied 8 bytes and the last 2 bytes were 0
            writer.WriteSpacePaddedInt32(size, 10 - DateMissing);

            if (DateMissing > 0)
            {
                for (var i = 0; i < DateMissing; i++)
                    writer.WriteByte(0);
            }

            writer.WriteFixedAnsiString(IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_END);
        }
    }
}
