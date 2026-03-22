using System.Collections.Generic;

namespace PESpy
{
    internal abstract class ImportLibraryMemberBuilder
    {
        public ImageArchiveMemberHeaderBuilder ArchiveHeader { get; }

        public LIBFileBuilder.NameList Names { get; }

        //If there's multiple names there might be multiple indices
        internal List<uint> FirstLinkerIndex;

        //See LibFileBuilder for why this is necessary
        internal uint SecondLinkerIndex;

        protected ImportLibraryMemberBuilder(
            ImageArchiveMemberHeader imageArchiveHeader,
            LIBFileBuilder libFileBuilder,
            List<uint> firstLinkerIndex,
            uint secondLinkerIndex)
        {
            ArchiveHeader = new ImageArchiveMemberHeaderBuilder(imageArchiveHeader, ArchiveMemberKind.ImportMember);
            Names = new LIBFileBuilder.NameList(this, libFileBuilder);
            FirstLinkerIndex = firstLinkerIndex;
            SecondLinkerIndex = secondLinkerIndex;
        }

        public abstract void WriteTo(FileWriter writer, Dictionary<string, int> nameToOffsetMap);

        public override string ToString()
        {
            if (Names.Count > 0)
                return Names[0];

            return "<Unknown>";
        }
    }
}
