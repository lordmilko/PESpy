using System;
using System.Linq;

namespace PESpy.View.Builder
{
    internal ref partial struct Merger
    {
        internal IView[] MergePE(ViewMode mode)
        {
            var peFile = (PEFile) file;

            var results = new PooledList<IView>();

            try
            {
                var sizeOfHeaders = peFile.OptionalHeader.SizeOfHeaders;

                var headerMetadata = new HeaderView(sizeOfHeaders, BuildSection(0, sizeOfHeaders, v => v, v => v));
                results.Add(headerMetadata);
                
                var lastSectionEnd = sizeOfHeaders;

                var isVirtualMode = (mode == ViewMode.Default && peFile.IsLoadedImage) || mode == ViewMode.Virtual;
            
                foreach (var section in peFile.SectionHeaders)
                {
                    //We are dealing with RVAs, so I think this whole thing is predicated on using virtual addresses

                    int start;
                    int size;

                    //Note: we don't have to worry about potentially changing the size of a DirectoryInfo, because directories exist _inside_ sections
                    if (isVirtualMode)
                    {
                        start = (Int32) section.VirtualAddress;
                        size = section.VirtualSize;
                    }
                    else
                    {
                        start = section.PointerToRawData;
                        size = section.SizeOfRawData;
                    }

                    if (size == 0)
                        continue;

                    //You can have extra padding in-between the header and the start of the first section; this logic is general purpose enough to also handle the possibility
                    //of padding also existing between other physical sections
                    ReadInterSectionData(lastSectionEnd, start, this, ref results);

                    Func<int, int>? getRealOffset = null;

                    //When requesting bytes from the user, we need to tell them what their RVA is
                    Func<int, int> getRVA;

                    switch (mode)
                    {
                        case ViewMode.Default:
                            if (peFile.IsLoadedImage)
                                getRVA = v => v; //We're loaded and we want loaded. All addresses are RVAs anyway
                            else
                            {
                                //We're unloaded. The merge is going to send us physical addresses, so we need to convert them to RVAs
                                getRVA = offset =>
                                {
                                    if (!peFile.TryGetRVA(offset, out var rva))
                                        return offset;

                                    return rva;
                                };
                            }

                            break;

                        case ViewMode.Physical:
                            if (peFile.IsLoadedImage)
                            {
                                //We're virtual and need to convert to physical

                                //The merge is going to send us physical addresses, but we're actually virtual so we need to convert them for the purposes of trying to read data
                                getRealOffset = offset =>
                                {
                                    if (!peFile.TryGetRVA(offset, out var rva))
                                        return offset;

                                    return rva;
                                };

                                getRVA = getRealOffset;
                            }
                            else
                            {
                                //We're physical and trying to read physical

                                //Still need to convert to resolve RVAs though
                                getRVA = offset =>
                                {
                                    if (!peFile.TryGetRVA(offset, out var rva))
                                        return offset;

                                    return rva;
                                };
                            }
                            break;

                        case ViewMode.Virtual:
                            if (peFile.IsLoadedImage)
                            {
                                //We're virtual and trying to read virtual
                                getRVA = v => v;
                            }
                            else
                            {
                                //We're physical and trying to read virtual

                                //The merge is going to send us virtual addresses, but we're actually physical so we need to convert them for the purposes of trying to read data
                                getRealOffset = rva =>
                                {
                                    if (!peFile.TryGetOffset(rva, out var offset))
                                        return rva;

                                    return offset;
                                };

                                getRVA = v => v; //We want RVAs, so we're all good
                            }
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to handle {nameof(ViewMode)} '{mode}'");
                    }

                    if (getRealOffset == null)
                        getRealOffset = v => v; //The real mode is the same as our merged mode. No conversion for reading bytes necessary

                    var data = BuildSection(start, start + size, getRealOffset, getRVA);

                    results.Add(new SectionView(start, section.Name.ToString(), data, size));

                    lastSectionEnd = start + size;
                }

                //Overlay data is not loaded in virtual modules, and perhaps more importantly: it uses physical addressing, which could overlap with any virtual addresses we might be using!
                if (!isVirtualMode)
                {
                    //Add any remaining data listed after all sections and the end of the file. This is a bit tricky, because SizeOfImage describes the size
                    //when loaded into memory, which is not the same as the size on disk. Overlay data does not get loaded into memory, which also means
                    //this data might not exist when reading a loaded image
                    var overlayStart = lastSectionEnd;
                    var fileEnd = (int) extension.GetInputLength();

                    TryCreateOMFRegion(peFile, ref results);

                    var overlayData = BuildSection(overlayStart, fileEnd, v => v, v => v, true);

                    if (overlayData.Length > 0)
                    {
                        var size = overlayData.Sum(v => v.Size);
                        results.Add(new OverlayView(overlayStart, overlayData, size));
                    }   
                }

                return results.ToArray();
            }
            finally
            {
                results.Dispose();
            }
        }

        private void TryCreateOMFRegion(PEFile peFile, ref PooledList<IView> results)
        {
            /* If we have OMF data, we want to read that separately. We would expect that if we have OMF data,
             * even if it's pointed to by a debug directory, that the typical OMF pattern is followed and that the
             * file ends with OMF data. We still want this (and any other data, like ImageDebugMisc entries)
             * in the overlay to be wrapped up in an "OverlayView" item. However, since we don't support building
             * nested logical views, we need to fake it like we do with LIBMerger: mess with our current position,
             * read the OMF data, insert it into the list of sorted structs, and then revert the offsets back to
             * how they were so that BuildSection doesn't suspect a thing */

            var debugTable = peFile.DebugTable;

            if (debugTable == null)
                return;

            NB05Data? data = null;

            for (var i = 0; i < debugTable.Length; i++)
            {
                ref var debugDir = ref debugTable[i];

                if (debugDir.Data is NB05Data d)
                {
                    data = d;
                    break;
                }
            }

            if (data == null)
                return;

            var sizeOfData = data.LfoBase;
            var start = data.Offset;
            var end = start + sizeOfData; //We would expect that this should take us to the end of the file. We don't have to +4 to cover the area that lfoBase is in

            var originalNextStructIndex = nextStructIndex;

            //Skip ahead to find the first struct that pertains to the OMF area
            for (; nextStructIndex < sortedStructs.Count; nextStructIndex++)
            {
                if (sortedStructs[nextStructIndex].Offset >= start)
                    break;
            }

            var nextStructIndexToInsertAt = nextStructIndex;

            var region = new LogicalRegionView(start, $"{data.Signature} OMF Data", BuildSection(start, end), ViewKind.NB05Data, sizeOfData);

            //Remove all the items we read into the region from the global struct list
            var endNextStructIndex = nextStructIndex;

            var numStructsInserted = endNextStructIndex - nextStructIndexToInsertAt;

            sortedStructs.RemoveRange(nextStructIndexToInsertAt, numStructsInserted);

            sortedStructs.Insert(nextStructIndexToInsertAt, region);

            //Pretend we were never here!
            nextStructIndex = originalNextStructIndex;
        }
    }
}
