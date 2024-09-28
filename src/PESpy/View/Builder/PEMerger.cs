using System;
using System.Collections.Generic;
using System.Linq;

namespace PESpy.View.Builder
{
    internal class PEMerger : Merger
    {
        private PEFile peFile;
        private ViewMode mode;

        internal PEMerger(
            PEFile peFile,
            List<IView> sortedStructs,
            HashSet<IView> delayNameViews,
            List<DirectoryInfo> discoveredDataDirectories,
            Extension extension,
            ViewMode mode) : base(sortedStructs, delayNameViews, discoveredDataDirectories, extension)
        {
            this.peFile = peFile;
            this.mode = mode;
        }

        internal override IView[] Merge()
        {
            var results = new List<IView>();

            var sizeOfHeaders = peFile.OptionalHeader.SizeOfHeaders;

            var headerMetadata = new HeaderView(sizeOfHeaders, BuildSection((Int32) 0, (Int32) sizeOfHeaders, v => v));
            results.Add(headerMetadata);

            var isVirtualMode = (mode == ViewMode.Default && peFile.IsLoadedImage) || mode == ViewMode.Virtual;
            
            foreach (var section in peFile.SectionHeaders)
            {
                //We are dealing with RVAs, so I think this whole thing is predicated on using virtual addresses

                Int32 start;
                int size;

                Func<int, int> getRealOffset = null;

                switch (mode)
                {
                    case ViewMode.Default:
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
                        }
                        else
                        {
                            //We're physical and trying to read physical
                        }
                        break;

                    case ViewMode.Virtual:
                        if (peFile.IsLoadedImage)
                        {
                            //We're virtual and trying to read virtual
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
                        }
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(ViewMode)} '{mode}'");
                }

                if (getRealOffset == null)
                    getRealOffset = v => v; //The real mode is the same as our merged mode. No conversion for reading bytes necessary

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

                var data = BuildSection(start, start + size, getRealOffset);

                results.Add(new SectionView(start, section, data, size));
            }

            //Overlay data is not loaded in virtual modules, and perhaps more importantly: it uses physical addressing, which could overlap with any virtual addresses we might be using!
            if (!isVirtualMode)
            {
                //Add any remaining data listed after all sections and the end of the file. This is a bit tricky, because SizeOfImage describes the size
                //when loaded into memory, which is not the same as the size on disk. Overlay data does not get loaded into memory, which also means
                //this data might not exist when reading a loaded image
                var lastResult = results.Last();
                var overlayStart = lastResult.Offset + lastResult.Size;
                var fileEnd = (Int32) peFile.OptionalHeader.SizeOfImage;
                var overlayData = BuildSection(overlayStart, fileEnd, v => v, true);

                if (overlayData.Length > 0)
                {
                    var size = overlayData.Sum(v => v.Size);
                    results.Add(new OverlayView(overlayStart, overlayData, size));
                }   
            }

            return results.ToArray();
        }
    }
}
