using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PESpy.PDB;
using PESpy.View.Builder;
using DirectoryInfo = PESpy.View.Builder.DirectoryInfo;

namespace PESpy.View
{
    public class PDBViewWriter : ViewWriter
    {
        internal PDBFile pdbFile;

        internal unsafe PDBViewWriter(
            PDBFile pdbFile,
#if PEFAST
            byte* mmf,
            int length
#else
            IFileReader reader,
#endif
            ) : base(
#if PEFAST
            mmf,
            length,
#else
            reader,
#endif
            null, ViewMode.Default, TryGetViewOffset, null)
        {
            this.pdbFile = pdbFile;
        }

        private static bool TryGetViewOffset(int offset, out int viewOffset)
        {
            viewOffset = offset;
            return true;
        }

        public override IView Finalize()
        {
            if (viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            var pages = new List<DirectoryInfo>();

            var streamIndexToNameMap = new Dictionary<int, string>();

            if (pdbFile.PDB != null)
            {
                foreach (var kv in pdbFile.PDB.StreamNameTable.NameToStreamNumberMap)
                {
                    //Note that you can have named streams that don't actually have any pages!
                    streamIndexToNameMap.Add(kv.Value, kv.Key);
                }
            }

            var pageToSIMap = new Dictionary<PN, (SI si, int siIndex, int pageIndex, string name)>();

            //Add in global symbol streams
            //todo: not sure how you detect that the stream isnt present

            if (pdbFile.DBI != null)
            {
                streamIndexToNameMap.Add(pdbFile.DBI.DbiHdr.snSymRecs, $"Symbol Records");
                streamIndexToNameMap.Add(pdbFile.DBI.DbiHdr.snGSSyms, $"Globals");
                streamIndexToNameMap.Add(pdbFile.DBI.DbiHdr.snPSSyms, $"Publics");
            }

            if (pdbFile.TPI != null)
            {
                var hdr = pdbFile.TPI.Hdr;

                if (hdr is HDR h)
                {
                    var tpihash = h.tpihash;
                    streamIndexToNameMap.Add(tpihash.sn, "TPI Hash");

                    if (tpihash.snPad != SN.Nil)
                        streamIndexToNameMap.Add(tpihash.sn, "TPI Hash (Aux)");
                }
            }

            if (pdbFile.DBI != null)
            {
                if (pdbFile.DBI.Modules != null)
                {
                    foreach (var module in pdbFile.DBI.Modules)
                    {
                        if (module.sn != SN.Nil)
                            streamIndexToNameMap.Add(module.sn, $"Symbols: {Path.GetFileName(module.ToString())}"); 
                    }
                }

                if (pdbFile.DBI.DbgHdr != null)
                {
                    var d = pdbFile.DBI.DbgHdr;

                    if (d.FPO != SN.Nil)
                        streamIndexToNameMap.Add(d.FPO, "FPO");

                    if (d.Exception != SN.Nil)
                        streamIndexToNameMap.Add(d.Exception, "Exception");

                    if (d.Fixup != SN.Nil)
                        streamIndexToNameMap.Add(d.Fixup, "Fixup");

                    if (d.OmapToSrc != SN.Nil)
                        streamIndexToNameMap.Add(d.OmapToSrc, "OmapToSrc");

                    if (d.OmapFromSrc != SN.Nil)
                        streamIndexToNameMap.Add(d.OmapFromSrc, "OmapFromSrc");

                    if (d.SectionHdr != SN.Nil)
                        streamIndexToNameMap.Add(d.SectionHdr, "SectionHdr");

                    if (d.TokenRidMap != SN.Nil)
                        streamIndexToNameMap.Add(d.TokenRidMap, "TokenRidMap");

                    if (d.XData != SN.Nil)
                        streamIndexToNameMap.Add(d.XData, "XData");

                    if (d.PData != SN.Nil)
                        streamIndexToNameMap.Add(d.PData, "PData");

                    if (d.NewFPO != SN.Nil)
                        streamIndexToNameMap.Add(d.NewFPO, "NewFPO");

                    if (d.SectionHdrOrig != SN.Nil)
                        streamIndexToNameMap.Add(d.SectionHdrOrig, "SectionHdrOrig");

                    //Don't add Max as it's not a real value
                }
            }

            //Build up a list of pages and which streams reside in each page
            for (var i = 0; i < pdbFile.StreamTable.StreamInfos.Count; i++)
            {
                var item = pdbFile.StreamTable.StreamInfos[i];

                string name;

                switch (i)
                {
                    case 0:
                        //When we're writing, the in-memory snSt will match the in-memory stream table. Only the on-disk snSt contains the previous stream table
                        if (pdbFile.globalBlock.writable)
                            continue;
                        else
                            name = "Previous Stream Table";
                        break;

                    case 1:
                        name = "PDB";
                        break;

                    case 2:
                        name = "TPI";
                        break;

                    case 3:
                        name = "DBI";
                        break;

                    case 4:
                        if (pdbFile.PDB?.HasIPI == true)
                            name = "IPI";
                        else
                            streamIndexToNameMap.TryGetValue(i, out name);
                        break;

                    default:
                        streamIndexToNameMap.TryGetValue(i, out name);
                        break;
                }

                for (var j = 0; j < item.PageList.Count; j++)
                {
                    var page = item.PageList[j];
                    pageToSIMap.Add(page, (item, i, j, name));
                }
            }

            var specialPageMap = new Dictionary<int, string>();

            //As per msf.cpp, the first few pages are special

            specialPageMap.Add(0, "Master Index");

            ref readonly var activeFPM = ref pdbFile.ActiveFPM;

            var fpm0 = pdbFile.FPM0;

            //In Big MSF the first FPM is always page 1 and the second FPM is page 2.
            //In Small MSF the first FPM is also always page 1, and the second FPM depends on the page size

            var fpmStatus = pdbFile.ActiveFpmPageNo == 1 ? "Active" : "Inactive";

            for (var i = 0; i < fpm0.FpmPages.Count; i++)
                specialPageMap.Add(fpm0.FpmPages[i], $"FPM 0 ({i + 1}/{fpm0.FpmPages.Count}) ({fpmStatus})");

            //Scope v7 variable
            {
                if (pdbFile is PDB7File v7)
                    fpmStatus = v7.MsfHeader.FpmPageNo == 2 ? "Active" : "Inactive";
                else
                {
                    var secondFPM = MSFParms.FromPageSize(pdbFile.PageSize).Fpm1PageNo;

                    fpmStatus = pdbFile.ActiveFpmPageNo == secondFPM ? "Active" : "Inactive";
                }

                var fpm1 = pdbFile.FPM1;

                for (var i = 0; i < fpm1.FpmPages.Count; i++)
                    specialPageMap.Add(fpm1.FpmPages[i], $"FPM 1 ({i + 1}/{fpm0.FpmPages.Count}) ({fpmStatus})");
            }

            //Scope v7 variable
            {
                if (pdbFile is PDB7File v7)
                {
                    //In V7, mpspnpnSt lists the pages that the pages of the stream table can be found
                    var pagesOfStreamTablePageList = v7.MsfHeader.PagesOfStreamTablePageList;

                    for (var i = 0; i < pagesOfStreamTablePageList.Length; i++)
                        specialPageMap.Add(pagesOfStreamTablePageList[i], $"Stream Table Page List ({i + 1}/{pagesOfStreamTablePageList.Length})");
                }
                else
                {
                    //In V2, mpspnpnSt lists the actual pages of the stream table directly
                    var streamTablePageList = ((PDB2File) pdbFile).MsfHeader.StreamTablePageList;

                    for (var i = 0; i < streamTablePageList.Length; i++)
                        specialPageMap.Add(streamTablePageList[i], $"Stream Table Page ({i + 1}/{streamTablePageList.Length})");
                }
            }

            //Tag any pages listed by the FPM as free.
            //Free pages may actually contain data (e.g. I've observed a page that contains an entire copy of the IPI stream)
            //however it's assumed that this is "junk data" that was written during the PDB's construction

            if (pdbFile is PDB7File)
            {
                //Additional FPM pages are scattered across the file at regular intervals

                for (var i = 0; i < pdbFile.NumPages; i++)
                {
                    /* We want to capture all of the extra pages that get allocated for the FPM. Not all of these pages are actually
                     * recorded in the FPM however (since it's a bug). A page is reserved to be an FPM page in FPM::fpmPn if
                     * pn & pageSize is 1 or 2. We implement the same logic here; without it, we may throw attempting to add a free
                     * page that's already known to be an FPM page */
                    var maybeFPMPage = i & (pdbFile.PageSize - 1);

                    if (maybeFPMPage == 1 || maybeFPMPage == 2)
                        continue;

                    if (activeFPM.PageMap[i])
                        specialPageMap.Add(i, "Free");
                }
            }
            else
            {
                //There is only a single FPM page

                for (var i = 0; i < pdbFile.NumPages; i++)
                {
                    if (activeFPM.PageMap[i])
                        specialPageMap.Add(i, "Free");
                }
            }

            //Scope v7 variable
            {
                if (pdbFile is PDB7File v7)
                {
                    for (var i = 0; i < v7.StreamTableLocation.PageList.Count; i++)
                    {
                        specialPageMap.Add(v7.StreamTableLocation.PageList[i], $"Stream Table ({i + 1}/{v7.StreamTableLocation.PageList.Count})");
                    }
                }
            }

            for (var i = 0; i < pdbFile.NumPages; i++)
            {
                var nameBuilder = new StringBuilder();

                nameBuilder.Append(i);

                if (pageToSIMap.TryGetValue(i, out var match))
                {
                    string name;

                    if (match.name != null)
                        name = $"'{match.name} ({match.siIndex})'";
                    else
                        name = $"Stream{match.siIndex}";

                    /* When microsoft-pdb serializes the stream table, it performs the following actions in order:
                     * 1. Frees any pages that were previously associated with the stream table, adding them to a secondary fpmFreed FPM
                     * 2. Serializes the current state of the stream table to disk, allocating new page numbers as we go
                     * 3. Adds a SI for snSt (i.e. the stream table) to the stream table
                     * 4. Merges fpmFreed into the main FPM
                     * 5. Serializes the FPM
                     *
                     * This sequence of actions has several consequences:
                     * - Firstly, it means that the description of snSt persisted to disk will always be one revision behind the current status,
                     *   by virtue of the fact a SI for snSt is added to the stream table _after_ it was already written to disk
                     * - Secondly, any pages that were freed in the current transaction that were originally committed in a previous transaction
                     *   cannot immediately be reused; we must wait for a commit before we're allowed to reuse those pages. Note that any page
                     *   that was allocated _and_ freed in the current transaction _can_ immediately be reused. These are the purpose of
                     *   the fpmCommitted and fpmFreed members of MSF_HB */

                    if (specialPageMap.TryGetValue(i, out var specialName))
                    {
                        if (match.siIndex == 0 && specialName == "Free")
                            name = $"Delayed Free / {name}";
                        else
                            name = $"{specialName} / {name}";
                    }

                    nameBuilder.Append(" | " + name).Append(" (").Append(match.pageIndex + 1).Append("/").Append(match.si.PageList.Count).Append(")");
                }
                else if (specialPageMap.TryGetValue(i, out var name))
                {
                    nameBuilder.Append(" | " + name);
                }

                //Try and include some details about which streams reside in this page

                pages.Add(new DirectoryInfo(nameBuilder.ToString(), i * pdbFile.PageSize, pdbFile.PageSize));
            }

            var merger = new PdbMsfMerger(pdbFile, structs, pages, extension);

            var results = merger.Merge();

            return new FileView(ViewMode.Physical, results, ViewKind.PDBFile);
        }
    }
}
