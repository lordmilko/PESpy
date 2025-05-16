using System.Collections.Generic;
using PESpy.PDB;

#nullable disable

namespace PESpy.View.Builder
{
    //Merges PDBs that use MSF. PDB v1 does not use MSF
    internal class PdbMsfMerger : Merger
    {
        internal PDBFile pdbFile;

        internal Dictionary<PN, int> pageNumberToSIIndex;

        internal PdbMsfMerger(
            PDBFile pdbFile,
            List<IView> sortedStructs,
            List<DirectoryInfo> pages,
            Extension extension) : base(sortedStructs, null, pages, extension)
        {
            this.pdbFile = pdbFile;
        }

        internal override IView[] Merge()
        {
            //When a value spans multiple pages, we'll split the value. The page that the first half is in
            //may be far away from the page that the second half is in. The way we figure out what our "next" page is
            //is by mapping each page number to the index of its stream info in the StreamTable. When we need to do a split,
            //we'll lookup the SI (which is a struct), find the index of our current page, and then the index of the page after it.
            //That page will be used as the starting offset of the split value

            if (pdbFile is PDB1File)
                throw new System.NotImplementedException();

            var streamInfos = pdbFile.StreamTable.StreamInfos;

            var dict = new Dictionary<PN, int>();

            for (var i = 0; i < streamInfos.Count; i++)
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
                    dict[page] = -1;

                //And for the pages that describe the location of the stream table's pages, we list these as being at -2
                foreach (var page in v7.MsfHeader.PagesOfStreamTablePageList)
                    dict[page] = -2;
            }
            else
            {
                //In V2, mpspnpnSt lists the pages of the stream table directly
                foreach (var page in ((PDB2File) pdbFile).MsfHeader.StreamTablePageList)
                    dict[page] = -1;
            }

            pageNumberToSIIndex = dict;

            var results = BuildSection(0, pdbFile.NumPages * pdbFile.PageSize);

            return results;
        }
    }
}
