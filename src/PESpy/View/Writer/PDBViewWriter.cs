using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.PDB;
using PESpy.View.Builder;
using DirectoryInfo = PESpy.View.Builder.DirectoryInfo;

namespace PESpy.View
{
    public class PDBViewWriter : ViewWriter
    {
        internal PDBFile pdbFile;

        internal override ICodeViewAccessor GetSymbolAccessor() => pdbFile;

        internal unsafe PDBViewWriter(PDBFile pdbFile) : base(pdbFile.CreateByteViewProvider(), ViewMode.Default, TryGetViewOffset, null)
        {
            this.pdbFile = pdbFile;
        }

        private static new bool TryGetViewOffset(int offset, out int viewOffset)
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

            var numPages = pdbFile.NumPages;
            var pages = ArrayPool<DirectoryInfo>.Shared.Rent(numPages);
            var contiguousSections = new PooledList<PDBContiguousSectionInfo>();

            try
            {
                GetContiguousSectionInfos(pdbFile, ref contiguousSections, pages);

                using var merger = new Merger(pdbFile, this, structs, pages.AsSpan(0, numPages), byteViewProvider);

                var results = merger.MergePDB(contiguousSections);

                return new FileView(ViewMode.Physical, pdbFile.Name, results, this, ViewKind.PDBFile);
            }
            finally
            {
                ArrayPool<DirectoryInfo>.Shared.Return(pages);
                contiguousSections.Dispose();
            }
        }

        private static Dictionary<int, string> BuildStreamIndexToNameMap(PDBFile pdbFile)
        {
            var streamIndexToNameMap = new Dictionary<int, string>();

            if (pdbFile.PDB != null)
            {
                foreach (var kv in pdbFile.PDB.StreamNameTable.NameToStreamNumberMap)
                {
                    //Note that you can have named streams that don't actually have any pages!
                    streamIndexToNameMap.Add(kv.Value, kv.Key);
                }
            }

            //Add in global symbol streams
            //todo: not sure how you detect that the stream isnt present

            if (pdbFile.DBI != null)
            {
                var dbiHdr = pdbFile.DBI.DbiHdr;

                if (dbiHdr.snSymRecs != SN.Nil)
                    streamIndexToNameMap.Add(dbiHdr.snSymRecs, $"Symbol Records");

                if (dbiHdr.snGSSyms != SN.Nil)
                    streamIndexToNameMap.Add(dbiHdr.snGSSyms, $"Globals");

                if (dbiHdr.snPSSyms != SN.Nil)
                    streamIndexToNameMap.Add(dbiHdr.snPSSyms, $"Publics");
            }

            if (pdbFile.TPI != null)
            {
                var hdr = pdbFile.TPI.Hdr;

                if (hdr is HDR h)
                {
                    var tpihash = h.tpihash;

                    if (tpihash.sn != SN.Nil)
                        streamIndexToNameMap.Add(tpihash.sn, "TPI Hash");

                    if (tpihash.snPad != SN.Nil)
                        streamIndexToNameMap.Add(tpihash.sn, "TPI Hash (Aux)");
                }
                else if (hdr is HDR_VC50Interim h50)
                {
                    if (h50.snHash != SN.Nil)
                        streamIndexToNameMap.Add(h50.snHash, "TPI Hash");
                }
                else if (hdr is HDR_16t h16)
                {
                    if (h16.snHash != SN.Nil)
                        streamIndexToNameMap.Add(h16.snHash, "TPI Hash");
                }
                else
                    throw new NotImplementedException();
            }

            if (pdbFile.IPI != null)
            {
                var hdr = pdbFile.IPI.Hdr;

                if (hdr is HDR h)
                {
                    var tpihash = h.tpihash;

                    if (tpihash.sn != SN.Nil)
                        streamIndexToNameMap.Add(tpihash.sn, "IPI Hash");

                    if (tpihash.snPad != SN.Nil)
                        streamIndexToNameMap.Add(tpihash.sn, "IPI Hash (Aux)");
                }
                else
                    throw new NotImplementedException();
            }

            if (pdbFile.DBI != null)
            {
                if (pdbFile.DBI.Modules != null)
                {
                    using var moduleNames = new ValueStringBuilder();

                    foreach (var module in pdbFile.DBI.Modules)
                    {
                        if (module.sn != SN.Nil)
                        {
                            //Can't do Path.GetFileName, because in .NET PDBs each module is named after a class,
                            //and compile generated classes may contain <>

                            var span = module.szModule.AsSpan();

                            var lastIndex = span.LastIndexOfAny((byte) '\\', (byte) '/');

                            if (lastIndex != -1 && lastIndex < span.Length - 1)
                                span = span.Slice(lastIndex + 1);

                            moduleNames.Append("Symbols: ");
                            moduleNames.Append(span);

                            streamIndexToNameMap.Add(module.sn, moduleNames.ToString());
                            moduleNames.Clear();
                        }
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

                    //Max simply represents the highest known stream; there are additional
                    //streams like XFG data that have been added since microsoft-pdb was published,
                    //however we don't know what they're called or at which position they are
                }
            }

            Debug.Assert(!streamIndexToNameMap.ContainsKey(SN.Nil));

            return streamIndexToNameMap;
        }

        private static Dictionary<PN, PageInfo> BuildPageInfoMap(PDBFile pdbFile, Dictionary<int, string> streamIndexToNameMap)
        {
            var pageToSIMap = new Dictionary<PN, PageInfo>(pdbFile.NumPages);

            //Build up a list of pages and which streams reside in each page
            for (var i = 0; i < pdbFile.StreamTable.StreamInfos.Length; i++)
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

                for (var j = 0; j < item.PageList.Length; j++)
                {
                    var page = item.PageList[j];
                    pageToSIMap.Add(page, new PageInfo(item, i, j, name));
                }
            }

            return pageToSIMap;
        }

        private static Dictionary<int, SpecialPageInfo> GetSpecialPageMap(PDBFile pdbFile)
        {
            var specialPageMap = new Dictionary<int, SpecialPageInfo>();

            //As per msf.cpp, the first few pages are special

            specialPageMap.Add(0, new SpecialPageInfo("Master Index", Merger.SPECIAL_STREAM_MASTER_INDEX, 0, 1));

            ref readonly var activeFPM = ref pdbFile.ActiveFPM;

            var fpm0 = pdbFile.FPM0;

            //In Big MSF the first FPM is always page 1 and the second FPM is page 2.
            //In Small MSF the first FPM is also always page 1, and the second FPM depends on the page size

            var fpmStatus = pdbFile.ActiveFpmPageNo == 1 ? "Active" : "Inactive";

            for (var i = 0; i < fpm0.FpmPages.Length; i++)
                specialPageMap.Add(fpm0.FpmPages[i], new SpecialPageInfo("FPM 0", Merger.SPECIAL_STREAM_FPM_0, i, fpm0.FpmPages.Length, fpmStatus));

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

                for (var i = 0; i < fpm1.FpmPages.Length; i++)
                    specialPageMap.Add(fpm1.FpmPages[i], new SpecialPageInfo("FPM 1", Merger.SPECIAL_STREAM_FPM_1, i, fpm1.FpmPages.Length, fpmStatus));
            }

            //Scope v7 variable
            {
                if (pdbFile is PDB7File v7)
                {
                    //In V7, mpspnpnSt lists the pages that the pages of the stream table can be found
                    var pagesOfStreamTablePageList = v7.MsfHeader.PagesOfStreamTablePageList;

                    for (var i = 0; i < pagesOfStreamTablePageList.Length; i++)
                        specialPageMap.Add(pagesOfStreamTablePageList[i], new SpecialPageInfo("Stream Table Page List", Merger.SPECIAL_STREAM_STREAMTABLE_LOCATION, i, pagesOfStreamTablePageList.Length));
                }
                else
                {
                    //In V2, mpspnpnSt lists the actual pages of the stream table directly
                    var streamTablePageList = ((PDB2File) pdbFile).MsfHeader.StreamTablePageList;

                    for (var i = 0; i < streamTablePageList.Length; i++)
                        specialPageMap.Add(streamTablePageList[i], new SpecialPageInfo("Stream Table Page", Merger.SPECIAL_STREAM_STREAMTABLE, i, streamTablePageList.Length));
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
                        specialPageMap.Add(i, new SpecialPageInfo("Free", Merger.SPECIAL_STREAM_FREE, 0, 1));
                }
            }
            else
            {
                //There is only a single FPM page

                for (var i = 0; i < pdbFile.NumPages; i++)
                {
                    if (activeFPM.PageMap[i])
                        specialPageMap.Add(i, new SpecialPageInfo("Free", Merger.SPECIAL_STREAM_FREE, 0, 1));
                }
            }

            //Scope v7 variable
            {
                if (pdbFile is PDB7File v7)
                {
                    for (var i = 0; i < v7.StreamTableLocation.PageList.Length; i++)
                    {
                        specialPageMap.Add(v7.StreamTableLocation.PageList[i], new SpecialPageInfo($"Stream Table", Merger.SPECIAL_STREAM_STREAMTABLE, i, v7.StreamTableLocation.PageList.Length));
                    }
                }
            }

            return specialPageMap;
        }

        internal static void GetContiguousSectionInfos(
            PDBFile pdbFile,
            ref PooledList<PDBContiguousSectionInfo> contiguousSections,
            DirectoryInfo[] pages)
        {
            var streamIndexToNameMap = BuildStreamIndexToNameMap(pdbFile);
            var pageToSIMap = BuildPageInfoMap(pdbFile, streamIndexToNameMap);

            var specialPageMap = GetSpecialPageMap(pdbFile);

            PDBContiguousSectionInfo currentContiguousSection = default;

            using var nameBuilder = new ValueStringBuilder();
            var fullNameBuilder = new ValueStringBuilder();

            try
            {
                for (var i = 0; i < pdbFile.NumPages; i++)
                {
                    var fullNameInfo = new FullNameInfo();

                    fullNameBuilder.Clear();
                    nameBuilder.Clear();

                    fullNameInfo.GlobalPageIndex = i;

                    fullNameBuilder.Append(i);

                    if (pageToSIMap.TryGetValue(i, out var match))
                    {
                        //string name;

                        fullNameInfo.StreamIndex = match.siIndex;
                        fullNameInfo.MatchName = match.name;

                        if (match.name != null)
                        {
                            nameBuilder.Append('\'');
                            nameBuilder.Append(match.name);
                            nameBuilder.Append(" (Stream");
                            nameBuilder.Append(match.siIndex);
                            nameBuilder.Append(")'");
                        }
                        else
                        {
                            nameBuilder.Append("Stream");
                            nameBuilder.Append(match.siIndex);
                        }

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

                        if (specialPageMap.TryGetValue(i, out var specialMatch))
                        {
                            if (match.siIndex == 0 && specialMatch.Name == "Free")
                            {
                                nameBuilder.Insert(0, "Delayed Free / ");
                                fullNameInfo.IsDelayedFree = true;
                            }
                            else
                            {
                                nameBuilder.Insert(0, " / ");
                                nameBuilder.Insert(0, specialMatch.Name);
                            }
                        }

                        RecordContiguousSection(
                            ref currentContiguousSection,
                            ref contiguousSections,
                            fullNameInfo,
                            match.siIndex,
                            match.pageIndex,
                            i,
                            match.si.PageList.Length
                        );

                        fullNameInfo.LocalPageIndex = match.pageIndex;
                        fullNameInfo.TotalPagesInStream = match.si.PageList.Length;

                        fullNameBuilder.Append(" | ");
                        fullNameBuilder.Append(nameBuilder.AsSpan());
                        fullNameBuilder.Append(" (Page ");
                        fullNameBuilder.Append(match.pageIndex + 1);
                        fullNameBuilder.Append("/");
                        fullNameBuilder.Append(match.si.PageList.Length);
                        fullNameBuilder.Append(")");
                    }
                    else if (specialPageMap.TryGetValue(i, out var specialMatch))
                    {
                        fullNameInfo.IsSpecialName = true;
                        fullNameInfo.MatchName = specialMatch.Name;
                        fullNameInfo.LocalPageIndex = specialMatch.LocalIndex;
                        fullNameInfo.TotalPagesInStream = specialMatch.TotalPagesInStream;
                        fullNameInfo.SpecialStatus = specialMatch.SpecialStatus;

                        fullNameBuilder.Append(" | ");
                        specialMatch.ToString(ref fullNameBuilder);

                        RecordContiguousSection(
                            ref currentContiguousSection,
                            ref contiguousSections,
                            fullNameInfo,
                            specialMatch.SpecialIndex,
                            specialMatch.LocalIndex,
                            i,
                            specialMatch.TotalPagesInStream
                        );
                    }
                    else
                    {
                        if (currentContiguousSection.NameInfo.MatchName != null)
                        {
                            if (currentContiguousSection.NumPages > 1)
                                contiguousSections.Add(currentContiguousSection);

                            currentContiguousSection = default;
                        }
                    }

                    //Try and include some details about which streams reside in this page

                    pages[i] = new DirectoryInfo(fullNameInfo, i * pdbFile.PageSize, pdbFile.PageSize);
                }
            }
            finally
            {
                fullNameBuilder.Dispose();
            }
        }

        private static void RecordContiguousSection(
            ref PDBContiguousSectionInfo currentContiguousSection,
            ref PooledList<PDBContiguousSectionInfo> contiguousSections,
            in FullNameInfo nameInfo,
            int streamIndex,
            int localPageIndex,
            int globalPageIndex,
            int totalPagesInStream)
        {
            //We just want to know if the contiguous section has a value or not
            if (!currentContiguousSection.HasValue)
            {
                if (totalPagesInStream > 1)
                    currentContiguousSection = new PDBContiguousSectionInfo(nameInfo, streamIndex, localPageIndex, globalPageIndex, totalPagesInStream);
            }
            else
            {
                if (currentContiguousSection.StreamIndex != streamIndex || localPageIndex != currentContiguousSection.LocalEndIndex + 1)
                {
                    if (currentContiguousSection.NumPages > 1)
                        contiguousSections.Add(currentContiguousSection);

                    if (totalPagesInStream > 1)
                        currentContiguousSection = new PDBContiguousSectionInfo(nameInfo, streamIndex, localPageIndex, globalPageIndex, totalPagesInStream);
                    else
                        currentContiguousSection = default;
                }
                else
                {
                    currentContiguousSection.LocalEndIndex = localPageIndex;
                    currentContiguousSection.GlobalEndIndex = globalPageIndex;
                }
            }
        }
    }
}
