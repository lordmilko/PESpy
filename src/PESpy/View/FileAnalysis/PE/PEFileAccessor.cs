using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using PESpy.View.Builder;

namespace PESpy.View
{
    internal unsafe class PEFileAccessor : FileAccessor
    {
        /// <summary>
        /// Gets the <see cref="PEFile"/> that this object provides access to.
        /// </summary>
        public PEFile PEFile { get; }

        public DirectoryInfo[] DataDirectories { get; set; }

        /* To reduce the cost of having to constantly lookup what section a given RVA belongs to and whether that section
         * can contain code or not, we maintain a cache of the last detected section, which can improve performance when
         * we're constantly looking up values that likely all belong to the same section */
        private PESectionLookupCache _lookupCache;

        public PEFileAccessor(PEFile peFile) : base(peFile.Is32Bit ? 32 : 64)
        {
            PEFile = peFile;

            _lookupCache = new PESectionLookupCache(peFile);

            ImageBase = peFile.OptionalHeader.ImageBase;

            /* For each section, we need to construct a SectionAccessor that will provide access to information about the
             * data contained in that section. For the purposes of analysis, both the header and overlay areas are also
             * considered to be "sections". The header should always exist; the question is simply whether the overlay
             * exists too */

            var sectionHeaders = peFile.SectionHeaders;

            var numSections = sectionHeaders.Length + 1; //The header is also a section

            var isLoaded = peFile.IsLoadedImage;
            var sizeOfHeaders = peFile.OptionalHeader.SizeOfHeaders;

            int overlayStart = 0;
            int overlayLength = 0;

            //If the file is not loaded into memory, try and detect an overlay. Overlay data is not present
            //in in-memory images, because by definition this data is outside the bounds of any section headers,
            //and so the loader ignores this data
            if (!isLoaded)
            {
                //We might have an overlay

                if (peFile.TryGetRawOverlayData(out _, out overlayLength))
                {
                    if (sectionHeaders.Length == 0)
                    {
                        overlayStart = sizeOfHeaders;
                    }
                    else
                    {
                        ref var lastSection = ref sectionHeaders[sectionHeaders.Length - 1];
                        var lastSectionEnd = lastSection.PointerToRawData + lastSection.SizeOfRawData;

                        overlayStart = lastSectionEnd;
                    }
                }

                if (overlayLength > 0)
                    numSections++;
            }

            var sectionAccessors = new SectionAccessor[numSections];

            sectionAccessors[0] = new SectionAccessor(0, sizeOfHeaders, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, sizeOfHeaders * ViewByte.Size));

            for (var i = 0; i < sectionHeaders.Length; i++)
            {
                ref var section = ref sectionHeaders[i];

                int start;
                int size;

                if (isLoaded)
                {
                    start = section.VirtualAddress;
                    size = section.VirtualSize;
                }
                else
                {
                    //Some sections, like .textbss, may say that their physical size is 0, in which case there's nothing for us to create an MMF around

                    start = section.PointerToRawData;
                    size = section.SizeOfRawData;
                }

                if (size == 0)
                {
                    ref var previous = ref sectionAccessors[i];

                    sectionAccessors[i + 1] = new SectionAccessor(SectionAccessorKind.Section, i, previous.EndAddress, section.Name.ToString());
                }
                else
                {
                    var mmf = MemoryMappedFile.CreateNew(null, size * ViewByte.Size);

                    var accessor = new SectionAccessor(start, start + size, SectionAccessorKind.Section, i, section.Name.ToString(), mmf);
                    sectionAccessors[i + 1] = accessor;
                }
            }

            if (overlayLength > 0)
            {
                sectionAccessors[sectionAccessors.Length - 1] = new SectionAccessor(overlayStart, overlayStart + overlayLength, SectionAccessorKind.Overlay, -1, "OVERLAY", MemoryMappedFile.CreateNew(null, overlayLength * ViewByte.Size));
            }

            Length = sectionAccessors[sectionAccessors.Length - 1].EndAddress;

            SectionAccessors = sectionAccessors;
        }

        public override void GetRawSectionData(in SectionAccessor sectionAccessor, out byte* pByte, out int rva, out int remainingLength)
        {
            switch (sectionAccessor.Kind)
            {
                case SectionAccessorKind.Header:
                    PEFile.GetRawHeaderData(out pByte, out remainingLength);
                    rva = sectionAccessor.StartAddress; //No RVA yet
                    break;

                case SectionAccessorKind.Section:
                    if (PEFile.IsLoadedImage)
                    {
                        PEFile.GetRawSectionDataFromRVA(sectionAccessor.StartAddress, sectionAccessor.SectionIndex, out pByte, out remainingLength);
                        rva = sectionAccessor.StartAddress; //StartAddress is an RVA
                    }
                    else
                    {
                        PEFile.GetRawSectionDataFromOffset(sectionAccessor.StartAddress, sectionAccessor.SectionIndex, out pByte, out remainingLength);
                        ref var section = ref PEFile.SectionHeaders[sectionAccessor.SectionIndex];
                        rva = section.VirtualAddress;
                    }
                    break;

                case SectionAccessorKind.Overlay:
                    //We've already established we have an overlay, so this should not fail
                    if (!PEFile.TryGetRawOverlayData(out pByte, out remainingLength))
                        throw new InvalidOperationException("Failed to read the data of the overlay after we had already established that an overlay exists. This should be impossible.");

                    Debug.Assert(remainingLength == sectionAccessor.Length);
                    rva = -1; //Not allowed to have code in the overlay
                    break;

                default:
                    pByte = default;
                    rva = default;
                    remainingLength = default;
                    Debug.Assert(false);
                    break;
            }
        }

        internal override ISectionDataAccessor CreateThreadLocalSectionDataAccessor() =>
            new PEFileThreadLocalSectionDataAccessor(PEFile);

        public override void Dispose()
        {
            PEFile.Dispose();

            base.Dispose();
        }
    }
}
