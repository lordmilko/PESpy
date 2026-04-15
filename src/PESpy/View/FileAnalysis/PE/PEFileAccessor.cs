using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace PESpy.View
{
    internal unsafe class PEFileAccessor : FileAccessor, ISectionDataAccessor
    {
        /// <summary>
        /// Gets the <see cref="PEFile"/> that this object provides access to.
        /// </summary>
        public PEFile PEFile { get; }

        public override IFile File => PEFile;

        public override bool IsLoaded => PEFile.IsLoadedImage;

        public ViewMode ViewMode { get; }

        protected override ViewKind FileViewKind => ViewKind.PEFile;

        /* To reduce the cost of having to constantly lookup what section a given RVA belongs to and whether that section
         * can contain code or not, we maintain a cache of the last detected section, which can improve performance when
         * we're constantly looking up values that likely all belong to the same section */
        internal PESectionLookupCache _lookupCache;
        private ISymbolAccessor _symbolAccessor;
        private readonly bool _wantVirtual;

        internal bool OwnsPEFile = true;

        private ViewWriter _viewWriter;

        public PEFileAccessor(PEFile peFile, ViewMode viewMode) : base(peFile.Is32Bit ? 32 : 64)
        {
            PEFile = peFile;
            ViewMode = viewMode;

            _wantVirtual = viewMode switch
            {
                ViewMode.Default => IsLoaded,
                ViewMode.Physical => false,
                ViewMode.Virtual => true
            };

            _lookupCache = new PESectionLookupCache(peFile, _wantVirtual);

            ImageBase = peFile.OptionalHeader.ImageBase;

            /* For each section, we need to construct a SectionAccessor that will provide access to information about the
             * data contained in that section. For the purposes of analysis, both the header and overlay areas are also
             * considered to be "sections". The header should always exist; the question is simply whether the overlay
             * exists too */

            var sectionHeaders = peFile.SectionHeaders;

            var numSections = sectionHeaders.Length + 1; //The header is also a section

            var sizeOfHeaders = peFile.OptionalHeader.SizeOfHeaders;

            int overlayStart = 0;
            int overlayLength = 0;

            //If the file is not loaded into memory, try and detect an overlay. Overlay data is not present
            //in in-memory images, because by definition this data is outside the bounds of any section headers,
            //and so the loader ignores this data
            if (!_wantVirtual)
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

                if (_wantVirtual)
                {
                    start = section.VirtualAddress;
                    size = section.VirtualSize;

                    //We aren't going to be able to probe addresses that only exist in memory
                    if (!peFile.IsLoadedImage)
                        size = Math.Min(size, section.SizeOfRawData);
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

        protected override object CreateOverview()
        {
            //If we don't have a symbol accessor yet, that's OK; we'll defer applying symbols until symbols
            //have been loaded
            return new PEFileOverview(PEFile, _symbolAccessor ?? NullSymbolAccessor.Instance);
        }

        protected override void RefreshOverviewSymbols()
        {
            //The caller _must_ have populated our symbol accessor at this point
            Debug.Assert(_symbolAccessor != null);
            ((PEFileOverview) _overview).RefreshSymbols(_symbolAccessor);
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
                    if (_wantVirtual)
                    {
                        PEFile.GetRawSectionDataFromRVA(sectionAccessor.StartAddress, sectionAccessor.SectionIndex, out pByte, out remainingLength);
                        rva = sectionAccessor.StartAddress; //StartAddress is an RVA
                    }
                    else
                    {
                        PEFile.GetRawSectionDataFromRelativeOffset(0, sectionAccessor.SectionIndex, out pByte, out remainingLength);
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

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            if (!PEFile.TryGetValueChunkFromSectionOrHeader(rva, out var chunk))
                throw new InvalidOperationException($"Failed to resolve a memory chunk for RVA 0x{rva}");

            return chunk;
        }

        internal override MemoryChunk GetMemoryChunkFromAddress(int address)
        {
            PEFile peFile;

            if (TryGetNestedFileRange(address, out var range))
            {
                peFile = (PEFile) range.File;
                address -= range.StartOffset;
            }
            else
                peFile = PEFile;

            MemoryChunk chunk;

            if (_lookupCache._wantVirtual)
            {
                if (!peFile.TryGetValueChunkFromSectionOrHeader(address, out chunk))
                    throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");
            }
            else
            {
                if (!peFile.TryGetValueChunkFromPhysicalOffset(address, out chunk))
                    throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");
            }

            return chunk;
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new PEViewWriter(PEFile, PEFile.CreateByteViewProvider(null), ViewMode);

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
        }

        protected override ViewWriter GetViewWriterForAddress(int targetOffset)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex) =>
            _lookupCache.TryGetSectionInfo(rva, out targetAddress, out sectionIndex, out _);

        public bool TryGetOffSeg(int rva, out int off, out ushort seg)
        {
            var sectionHeaders = PEFile.SectionHeaders;

            for (var i = sectionHeaders.Length - 1; i >= 0; i--)
            {
                ref var sectionHeader = ref sectionHeaders[i];

                if (rva >= sectionHeader.VirtualAddress)
                {
                    off = rva - sectionHeader.VirtualAddress;
                    seg = (ushort) (i + 1);
                    return true;
                }
            }

            throw new NotImplementedException();
        }

        void ISectionDataAccessor.GetRawSectionData(int targetAddress, out byte* pByte, out int remainingLength) =>
            _lookupCache.GetRawSectionDataFromTargetAddress(targetAddress, out pByte, out remainingLength);

        void ISectionDataAccessor.GetRawSectionData(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength) =>
            _lookupCache.GetRawSectionDataFromTargetAddress(targetAddress, sectionIndex, out pByte, out remainingLength);

        internal override bool TryGetDataSymbol(ulong address, int rva, out FixedUtf8String name, out int displacement)
        {
            //Stuff like fs:[0] will take us below 0
            if (rva > 0 && _lookupCache.TryGetSectionInfo((int) rva, out var targetAddress, out _, out _))
            {
                if (_infoMap.TryGetValue((int) targetAddress, out var data) && data.NameIndex != 0)
                {
                    //Certain symbols have enhanced names, whereas others
                    //like ImageThunkData exist in pairs: the IAT one might not
                    //have a name, but we do want to show the name from the ILT one!
                    //We don't currently do that

                    name = _names[data.NameIndex - 1];

                    Debug.Assert(name.Length > 0);
                    displacement = 0;
                    return true;
                }
                else
                {
                    //Maybe it's a displacement (e.g. a jump within a function). We prefer to ask the infoMap directly where possible
                    //as this would presumably be faster than having to do a whole lookup

                    //Symbols should have already been discovered prior to us trying to get symbols
                    if (_symbolAccessor.TryGetNameFromAddress(rva, out var symName, out displacement))
                    {
                        name = symName;
                        return true;
                    }
                }
            }

            name = default;
            displacement = default;
            return false;
        }

        public override bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, int targetAddress, out int rva)
        {
            if (_wantVirtual)
            {
                rva = targetAddress; //SectionAccessor.StartAddress is an RVA, which means address (which is an offset against StartAddress)
            }
            else
            {
                if (sectionAccessor.SectionIndex == -1)
                {
                    rva = default;
                    return false;
                }

                ref var section = ref PEFile.SectionHeaders[sectionAccessor.SectionIndex];
                rva = section.VirtualAddress + (targetAddress - sectionAccessor.StartAddress);
            }

            return true;
        }

        internal override ISectionDataAccessor CreateThreadLocalSectionDataAccessor() =>
            new PEFileThreadLocalSectionDataAccessor(PEFile, _lookupCache._wantVirtual);

        public override void Dispose()
        {
            if (OwnsPEFile)
                PEFile.Dispose();

            base.Dispose();
        }

        internal override ISymbolAccessor GetSymbolAccessor(
            bool load,
            LocatorHttpPolicy httpPolicy,
            ILocatorProgress? progress,
            CancellationToken cancellationToken = default)
        {
            if (_symbolAccessor == null)
            {
                if (load)
                    _symbolAccessor = PEFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
            }

            return _symbolAccessor ?? NullSymbolAccessor.Instance;
        }
    }
}
