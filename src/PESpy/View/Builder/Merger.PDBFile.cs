using System;
using System.Collections.Generic;
using PESpy.PDB;

#nullable disable

namespace PESpy.View.Builder
{
    //Merges PDBs that use MSF. PDB v1 does not use MSF
    internal ref partial struct Merger
    {
        //Not streams, but we need their indices to be in the page number to SI index lookup map for file analysis
        //lookups (we don't need them for merging)
        internal const int SPECIAL_STREAM_MASTER_INDEX = -1;
        internal const int SPECIAL_STREAM_FPM_0 = -2;
        internal const int SPECIAL_STREAM_FPM_1 = -3;

        internal const int SPECIAL_STREAM_STREAMTABLE = -4;
        internal const int SPECIAL_STREAM_STREAMTABLE_LOCATION = -5;
        internal const int SPECIAL_STREAM_FREE = -6;

        internal Dictionary<PN, int> pageNumberToSIIndex;

        internal IView[] MergePDB(PooledList<PDBContiguousSectionInfo> contiguousSections)
        {
            //When a value spans multiple pages, we'll split the value. The page that the first half is in
            //may be far away from the page that the second half is in. The way we figure out what our "next" page is
            //is by mapping each page number to the index of its stream info in the StreamTable. When we need to do a split,
            //we'll lookup the SI (which is a struct), find the index of our current page, and then the index of the page after it.
            //That page will be used as the starting offset of the split value

            var pdbFile = (PDBFile) file;

            if (pdbFile is PDB1File)
                throw new NotImplementedException("Handling a PDB1File is not implemented");

            pageNumberToSIIndex = GetPageNumberToSIIndex(pdbFile);

            var currentPageIndex = 0;

            using var results = new PooledList<IView>();

            var pageSize = pdbFile.PageSize;

            //Calling BuildSection multiple times will clear the masterList, which will prevent us from back patching bytes we read as junk
            //which actually turned out to be data from a section that came after it and had to be split. So Plan B: we'll read the whole thing,
            //and then carve it up into sections

            var raw = BuildSection(0, pdbFile.NumPages * pageSize);

            for (var i = 0; i < contiguousSections.Count; i++)
            {
                var section = contiguousSections[i];

                if (currentPageIndex < section.GlobalStartIndex)
                {
                    //Write all pages up to the start of this page
                    results.AddRange(raw, currentPageIndex, section.GlobalStartIndex - currentPageIndex);
                }

                var sectionViews = raw.AsSpan(section.GlobalStartIndex, section.NumPages).ToArray();

                results.Add(new SectionView(
                    section.GlobalStartIndex * pageSize,
                    section.ToString(),
                    sectionViews,
                    viewWriter,
                    section.NumPages * pageSize
                ));

                currentPageIndex = section.GlobalEndIndex + 1;
            }

            if (currentPageIndex < raw.Length)
                results.AddRange(raw, currentPageIndex, raw.Length - currentPageIndex);

            return results.ToArray();
        }

        internal static Dictionary<PN, int> GetPageNumberToSIIndex(PDBFile pdbFile)
        {
            var dict = new Dictionary<PN, int>();

            dict[0] = SPECIAL_STREAM_MASTER_INDEX;

            foreach (var page in pdbFile.FPM0.FpmPages)
                dict[page] = SPECIAL_STREAM_FPM_0;

            foreach (var page in pdbFile.FPM1.FpmPages)
                dict[page] = SPECIAL_STREAM_FPM_1;

            var streamInfos = pdbFile.StreamTable.StreamInfos;

            for (var i = 0; i < streamInfos.Length; i++)
            {
                var si = streamInfos[i];

                foreach (var pn in si.PageList)
                    dict[pn] = i;
            }

            if (pdbFile is PDB7File v7)
            {
                //For the pages that describe the location of the stream table, we list these as being at index -1, which we special case
                //to know that we need to retrieve the StreamTableLocation.PageList
                foreach (var page in v7.StreamTableLocation.PageList)
                    dict[page] = SPECIAL_STREAM_STREAMTABLE;

                //And for the pages that describe the location of the stream table's pages, we list these as being at -2
                foreach (var page in v7.MsfHeader.PagesOfStreamTablePageList)
                    dict[page] = SPECIAL_STREAM_STREAMTABLE_LOCATION;
            }
            else
            {
                //In V2, mpspnpnSt lists the pages of the stream table directly
                foreach (var page in ((PDB2File) pdbFile).MsfHeader.StreamTablePageList)
                    dict[page] = SPECIAL_STREAM_STREAMTABLE;
            }

            return dict;
        }
    }
}
