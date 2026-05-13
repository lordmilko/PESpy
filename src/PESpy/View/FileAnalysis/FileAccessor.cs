using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ClrDebug;
using PESpy.PDB;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy.View
{
    /// <summary>
    /// Provides facilities for accessing the bytes of a non-specific file type.
    /// </summary>
    public abstract unsafe class FileAccessor : IDisposable
    {
        public static FileAccessor Create(IFile file)
        {
            return file.Kind switch
            {
                FileKind.PE          => new PEFileAccessor((PEFile) file, ViewMode.Default),
                FileKind.NE          => new NEFileAccessor((NEFile) file),
                FileKind.LE          => new LEFileAccessor((LEFile) file),
                FileKind.DOS         => new DOSFileAccessor((DOSFile) file),
                FileKind.DBG         => new DBGFileAccessor((DBGFile) file),
                FileKind.PDB           => CreatePDBFileAccessor((PDBFile) file),
                FileKind.PortablePDB => new PortablePDBFileAccessor((PortablePDBFile) file),
                FileKind.OBJ         => new OBJFileAccessor((OBJFile) file),
                FileKind.LIB         => new LIBFileAccessor((LIBFile) file),
                FileKind.OMF         => new OMFFileAccessor((OMFFile) file),
                FileKind.OMFLIB      => new OMFLIBFileAccessor((OMFLIBFile) file),
                FileKind.OMFDBG      => new OMFDBGFileAccessor((OMFDBGFile) file),
                FileKind.SYM         => new SYMFileAccessor((SYMFile) file),
                _ => throw new NotImplementedException($"Don't know how to open a file of type '{file.Kind}'")
            };

            static FileAccessor CreatePDBFileAccessor(PDBFile pdbFile)
            {
                switch (pdbFile.PDBKind)
                {
                    case PDBFileKind.V1:
                        return new PDB1FileAccessor((PDB1File) pdbFile);

                    case PDBFileKind.V2:
                    case PDBFileKind.V7:
                        return new PDBFileAccessor(pdbFile);

                    default:
                        Debug.Assert(false);
                        return null;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct ViewInfo
        {
            //I tried reordering this to make it 14 bytes with pack 2 but that didn't make any difference
            public ViewKind ViewKind;
            public SpanAllocatorHandle XRefs;
        }

        /// <summary>
        /// Gets the bitness of the code contained in this file, or the bitness of the code this file is associated with.
        /// </summary>
        public int Bitness { get; }

        public SectionAccessor[] SectionAccessors { get; protected set; }

        //todo: this doesnt make sense for all file types, but our disasm needs this
        public long ImageBase { get; protected set; }

        public virtual bool IsLoaded => false;

        /// <summary>
        /// Gets the total length of the file.
        /// </summary>
        public long Length { get; protected set; }

        public abstract IFile File { get; }

        private RegionBuilder[] _topLevelDirectories;
        private RegionBuilder[] _firstDirectoryByAddress;
        private Dictionary<long, int> _directoryByAddressLookup;

        internal RegionBuilder[] TopLevelDirectories => _topLevelDirectories;

        private RegionBuilder[] _topLevelRegions; //Contains the hierarchy of regions
        private RegionBuilder[] _firstRegionByAddress; //Contains the highest region at each address. If two regions share an address, you only get the first one. Not sorted
        private Dictionary<long, int> _regionByAddressLookup;

        internal RegionBuilder[] TopLevelRegions => _topLevelRegions;

        internal NestedFileRange[] NestedFileRanges;

        internal Dictionary<long, int> LargeAddresses;

        protected object _overview;
        protected bool _isManaged;
        private SpanAllocator<XRef> _xrefs;

        private long[] _stringAddresses;

        public object Overview => _overview ??= CreateOverview();

        //I tried SegmentedDictionary but the performance was _way_ worse
        internal Dictionary<long, ViewInfo> _infoMap = new Dictionary<long, ViewInfo>();
        private bool _disposed;

        protected abstract ViewKind FileViewKind { get; }

        protected FileAccessor(int bitness)
        {
            switch (bitness)
            {
                case 16:
                case 32:
                case 64:
                    Bitness = bitness;
                    break;

                default:
                    throw new NotSupportedException($"Bitness '{bitness}' is not a supported bitness");
            }

            SectionAccessors = null!;
        }

        protected abstract object CreateOverview();

        //We do a two phase load of files in the UI: get a FileAccessor up and running as quickly as possible
        //to show something in the UI, and then let analysis/external symbol loading run in the background.
        //When analysis has completed, we may have now located an external symbol file, in which case we should
        //refresh the symbols contained in our overview
        protected virtual void RefreshOverviewSymbols()
        {
        }

        protected static int GetBitness(IMAGE_FILE_MACHINE machine)
        {
            switch (machine)
            {
                case IMAGE_FILE_MACHINE_I386:
                case IMAGE_FILE_MACHINE_UNKNOWN: //Assume 32-bit
                case IMAGE_FILE_MACHINE_CEE: //MSIL. You can have this in a PDB for a .NET Framework app. Just assume 32-bit
                    return 32;

                case IMAGE_FILE_MACHINE_AMD64:
                    return 16;

                default:
                    throw new NotImplementedException($"Don't know how to get the bitness of machine '{machine}'");
            }
        }

        public abstract bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex);

        public ViewEntity GetEntity(long address)
        {
            var pViewByte = GetViewByte(address, out var sectionIndex);

            return GetEntity(address, pViewByte, sectionIndex);
        }

        public ViewEntity GetEntity(ViewByte* pViewByte)
        {
            for (var i = 0; i < SectionAccessors.Length; i++)
            {
                ref var accessor = ref SectionAccessors[i];

                if (accessor.Contains(pViewByte))
                {
                    return GetEntity(pViewByte, i);
                }
            }

            throw new InvalidOperationException($"The specified {nameof(ViewByte)} does not belong to any section of the current {nameof(FileAccessor)}");
        }

        public ViewEntity GetEntity(ViewByte* pViewByte, int sectionAccessorIndex)
        {
            ref var accessor = ref SectionAccessors[sectionAccessorIndex];

            var offset = (int) (pViewByte - accessor.pViewBytes);

            var targetAddress = accessor.StartAddress + offset;

            var pBytes = GetRawSectionData(accessor);

            return GetEntity(sectionAccessorIndex, targetAddress, pViewByte, accessor, pBytes);
        }

        public ViewEntity GetEntity(long address, ViewByte* pViewByte, int sectionAccessorIndex)
        {
            ref var accessor = ref SectionAccessors[sectionAccessorIndex];

            var pBytes = GetRawSectionData(accessor);

            return new ViewEntity(
                this,
                sectionAccessorIndex,
                accessor,
                address,
                pViewByte,
                accessor.pViewBytes,
                accessor.pViewBytesEnd,
                pBytes,
                _infoMap,
                LargeAddresses
            );
        }

        //For when you already have all the various pieces
        internal unsafe ViewEntity GetEntity(
            int sectionAccessorIndex,
            long targetAddress,
            ViewByte* pViewByte,
            in SectionAccessor sectionAccessor,
            IntPtr pBytes)
        {
            return new ViewEntity(
                this,
                sectionAccessorIndex,
                sectionAccessor,
                targetAddress,
                pViewByte,
                sectionAccessor.pViewBytes,
                sectionAccessor.pViewBytesEnd,
                pBytes,
                _infoMap,
                LargeAddresses
            );
        }

        public ViewEntity GetEntity(int address, int sectionIndex) //_not_ a sectionAccessorIndex
        {
            ref var accessor = ref SectionAccessors[sectionIndex + 1]; //The first section is the header

            var pBytes = GetRawSectionData(accessor);

            var relativeOffset = address - accessor.StartAddress;
            Debug.Assert(relativeOffset >= 0);
            Debug.Assert(!accessor.IsEmpty);

            var pViewByte = &accessor.pViewBytes[relativeOffset];

            return new ViewEntity(
                this,
                sectionIndex + 1,
                accessor,
                address,
                pViewByte,
                accessor.pViewBytes,
                accessor.pViewBytesEnd,
                pBytes,
                _infoMap,
                LargeAddresses
            );
        }

        public FileView GetFileView() =>
            GetFileView(ViewMode.Default);

        internal FileView GetFileView(ViewMode viewMode)
        {
            var sectionAccessors = SectionAccessors;

            if (sectionAccessors.Length == 1)
            {
                ref var sectionAccessor = ref sectionAccessors[0];

                if (sectionAccessor.Kind == SectionAccessorKind.Header)
                {
                    //Flatten the hierarchy
                    var childProvider = new GlobalViewProvider(0, this);

                    return new FileView(ViewMode.Physical, File, childProvider, GetViewWriter(), sectionAccessor.Length, FileViewKind);
                }
            }

            var sectionViews = new IView[sectionAccessors.Length];

            var nextIndex = 0;

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                if (sectionAccessor.IsEmpty)
                    continue;

                IView view;

                switch (sectionAccessor.Kind)
                {
                    case SectionAccessorKind.Header:
                        view = new HeaderView(i, sectionAccessor, this, GetViewWriter());
                        break;

                    case SectionAccessorKind.Page:
                    case SectionAccessorKind.Section:
                        view = new SectionView(i, sectionAccessor, this, GetViewWriter());
                        break;

                    case SectionAccessorKind.Overlay:
                        view = new OverlayView(i, sectionAccessor, this, GetViewWriter());
                        break;

                    default:
                        throw new NotImplementedException();
                }

                sectionViews[nextIndex] = view;
                nextIndex++;
            }

            if (nextIndex != sectionViews.Length)
                Array.Resize(ref sectionViews, nextIndex);

            if (viewMode == ViewMode.Default)
            {
                if (FileViewKind == ViewKind.PEFile)
                    viewMode = ((PEFileAccessor) this).IsLoaded ? ViewMode.Virtual : ViewMode.Physical;
                else
                    viewMode = ViewMode.Physical;
            }

            return new FileView(viewMode, File, sectionViews, GetViewWriter(), FileViewKind);
        }

        public ViewEntity[] Entities => EnumerateEntities().ToArray();

        public IEnumerable<ViewEntity> EnumerateEntities()
        {
            var symbolAccessor = GetSymbolAccessor();

            for (var i = 0; i < SectionAccessors.Length; i++)
            {
                var sectionAccessor = SectionAccessors[i];
                var sectionLength = sectionAccessor.Length;

                var pBytes = GetRawSectionData(sectionAccessor);

                var j = 0;

                while (j < sectionLength)
                {
                    //We can't use unsafe in an iterator, so we need to put all the logic in the FileEntity ctor
                    var entity = new ViewEntity(
                        this,
                        i,
                        sectionAccessor,
                        j,
                        sectionLength,
                        pBytes,
                        _infoMap,
                        LargeAddresses
                    );

                    j += entity.Length;

                    yield return entity;
                }
            }
        }

        public ViewEntity[] GetEntities(int sectionAccessorIndex)
        {
            var iterator = EnumerateEntities(sectionAccessorIndex);

            using var list = new ValueList<ViewEntity>();

            while (iterator.MoveNext())
            {
                list.Add(iterator.Current);
            }

            return list.ToArray();
        }

        public ViewEntityIterator EnumerateEntities(int sectionAccessorIndex)
        {
            var symbolAccessor = GetSymbolAccessor();

            var sectionAccessor = SectionAccessors[sectionAccessorIndex];

            var pBytes = GetRawSectionData(sectionAccessor);

            return new ViewEntityIterator(
                this,
                0,
                symbolAccessor,
                sectionAccessor,
                sectionAccessorIndex,
                sectionAccessor.Length,
                pBytes,
                _infoMap,
                LargeAddresses
            );
        }

        public IntPtr GetRawSectionData(in SectionAccessor sectionAccessor)
        {
            GetRawSectionData(sectionAccessor, out var pByte, out _, out _);

            return (IntPtr) pByte;
        }

        public abstract unsafe void GetRawSectionData(in SectionAccessor sectionAccessor, out byte* pByte, out int rva, out int remainingLength);

        //Must specifically be an RVA
        internal abstract MemoryChunk GetMemoryChunkFromRVA(int rva);

        //Could either be an RVA or an offset (depends if we're a loaded image or not)
        internal abstract void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter);

        #region GetStructView

        protected abstract ViewWriter GetViewWriter();

        //For when the type of view you're after may not be top level. When it's top level
        //it is possible to ask the info map what the ViewKind is
        public unsafe IView GetStructView(long targetAddress, ViewKind viewKind)
        {
            var pViewByte = GetViewByte(targetAddress, out var sectionAccessorIndex);

            ref var sectionAccessor = ref SectionAccessors[sectionAccessorIndex];
            var limit = sectionAccessor.pViewBytesEnd;

            if (pViewByte->Kind == ViewByteKind.Body)
            {
                return GetNestedStructView(pViewByte, limit, targetAddress, viewKind);
            }
            else
            {
                GetMemoryChunkFromAddress(targetAddress, out var chunk, out var viewWriter);

                var structView = ViewProvider.CreateStructView(viewKind, pViewByte->GetLength(limit), chunk, viewWriter);

                return structView;
            }
        }

        /// <summary>
        /// Creates a view around the entity at a given target address.
        /// </summary>
        /// <param name="targetAddress">The address of the entity to resolve. If this address is partway
        /// into an entity, the field that the address lies within will be resolved. If the address points
        /// to the start of the first field, the parent struct will be returned instead.</param>
        /// <returns>A view that encapsulates the entity located at the given address</returns>
        public IView GetView(int targetAddress)
        {
            var entity = GetEntity(targetAddress);

            if (entity.ViewByte->Kind == ViewByteKind.Body)
            {
                var head = entity.GetHead(this, out _);

                if (head.Kind == 0)
                    throw new InvalidOperationException("Don't know how to handle an address partway into an entity that doesn't have a kind");

                var parent = GetViewFromEntity(head);

                var @continue = true;

                while (@continue)
                {
                    @continue = false;

                    var childIndex = 0;

                    foreach (var child in parent.Children)
                    {
                        if (child.Contains(targetAddress))
                        {
                            if (child.IsContainer())
                            {
                                parent = child;

                                @continue = true;
                                break;
                            }

                            if (childIndex == 0)
                            {
                                //This is the first field; return the parent struct instead
                                return parent;
                            }

                            //The xref had better be at the start of the value!
                            Debug.Assert(child.Offset == targetAddress);
                            return child;
                        }

                        childIndex++;
                    }
                }
            }

            return GetViewFromEntity(entity);
        }

        public IView GetViewFromEntity(ViewEntity entity)
        {
            if (entity.Kind != 0)
            {
                GetMemoryChunkFromAddress(entity.TargetAddress, out var chunk, out var viewWriter);
                return ViewProvider.CreateStructView(entity.Kind, entity.Length, chunk, viewWriter, entity.IsSplit); //The ViewWriter is cached so this is OK
            }
                switch (entity.ViewByte->Kind)
                {
                    case ViewByteKind.Data:
                        switch (entity.ViewByte->DataKind)
                        {
                            case ViewByteDataKind.Padding:
                                if (entity.IsSplit)
                                    throw new NotImplementedException("Splitting padding is not implemented"); //Just get the bytes before the split?

                                return new ByteBlobView(entity.TargetAddress, entity.Bytes, null, this);

                            case ViewByteDataKind.String:
                                if (entity.IsSplit)
                                    throw new NotImplementedException("Splitting a string is not implemented"); //Just get the bytes before the split?

                                //Note that even if it _is_ null terminated, we _do_ still want to include the null terminator
                                //in the name so we can print it properly
                                if (entity.ViewByte->IsWide)
                                {
                                    var str = new FixedUtf16String((char*) (byte*) entity.Bytes, entity.Length / 2);
                                    return new ValueView<FixedUtf16String>(entity.TargetAddress, str, entity.Length, ViewKind.Utf16String, this, entity.Name);
                                }
                                else
                                {
                                    var str = new FixedUtf8String((byte*) entity.Bytes, entity.Length);
                                    return new ValueView<FixedUtf8String>(entity.TargetAddress, str, entity.Length, ViewKind.Utf8String, this, entity.Name);
                                }

                            case ViewByteDataKind.Unknown:
                                if (entity.IsSplit)
                                    throw new NotImplementedException("Splitting unknown data is not implemented"); //Just get the bytes before the split?

                                return new ByteBlobView(entity.TargetAddress, entity.Bytes, null, this, entity.Name);

                            case ViewByteDataKind.Decimal:
                                if (entity.IsSplit)
                                    throw new NotImplementedException("Splitting a decimal is not implemented"); //Just get the bytes before the split?

                                if (entity.Length == 4)
                                    return new ValueView<float>(entity.TargetAddress, *(float*) (byte*) entity.Bytes, entity.Length, ViewKind.Decimal, this, entity.Name);
                                else
                                    return new ValueView<double>(entity.TargetAddress, *(double*) (byte*) entity.Bytes, entity.Length, ViewKind.Decimal, this, entity.Name);

                            default:
                                throw new NotImplementedException();
                        }

                    case ViewByteKind.Code:
                        if (entity.IsSplit)
                            throw new NotImplementedException("Splitting code is not implemented"); //Just get the bytes before the split?
                        var range = new AsmRange<object>(startOffset: (int) entity.TargetAddress, startRVA: (int) entity.TargetAddress, functionRVA: (int) (entity.TargetAddress - entity.Displacement), entity.Name);
                        range.EndOffset = (int) entity.TargetAddress + entity.Length;

                        return new AsmView<object>((int) entity.TargetAddress, (byte) Bitness, range, this, entity.ViewByte->IsIL ? ViewKind.IL : ViewKind.Assembly);

                    case ViewByteKind.Unknown:
                        if (entity.IsSplit)
                            throw new NotImplementedException("Splitting unknown is not implemented"); //Just get the bytes before the split?

                        return new ByteBlobView(entity.TargetAddress, entity.Bytes, null, this);

                    case ViewByteKind.Body:
                        Debug.Assert(entity.ViewByte->BodyKind == ViewByteBodyKind.SplitHead);

                        //We want to create a view that just encapsulates the portion that this body encapsulates.
                        //First, we need to rewind to get the head

                        var origin = entity.GetSplitHeadOrigin(this, out var bytesRewound);

                        if (origin.Kind != 0)
                        {
                            //We use origin.TargetAddress to get the head; we're then going to split it at the point we're actually after
                            GetMemoryChunkFromAddress(origin.TargetAddress, out var chunk, out var viewWriter);

                            //The length of the origin is just the length up to the end of the current page. However, we need
                            //to be constructing a split against the data contained in the entire entity, so we need to get the true
                            //length of the entity so that things like arrays can correctly compute the total number of elements they
                            //should have
                            var fullLength = origin.GetFullLength(this);

                            var baseView = ViewProvider.CreateStructView(origin.Kind, fullLength, chunk, viewWriter, origin.IsSplit);

                            if (baseView is ISplittableView sv)
                            {
                                var (first, second) = sv.Split(entity.TargetAddress, baseView.Offset + bytesRewound);

                                var pageSize = ((PDBFile) File).PageSize;

                                //Second now contains the remainder; but the question now is what is contiguous region
                                //within second that doesn't require a split as well
                                var numPages = SI.DivideUp((int) second.Size, pageSize);

                                if (numPages == 1)
                                    return second; //The right hand side fits within a single page, no need to consider further splitting

                                //Second is now at the start of a frame; check all pages but the last one to see whether there's actually
                                //a SplitTail in there. If so, we need to split. If not, implicitly the last tail (which may not have a full
                                //page's worth) is part of the value and we don't need to split

                                var pPageStart = entity.ViewByte + pageSize - 1;

                                for (var i = 0; i < numPages - 1; i++)
                                {
                                    if (pPageStart->BodyKind == ViewByteBodyKind.SplitTail)
                                    {
                                        IView third;

                                        var secondEnd = (int) entity.TargetAddress + (pPageStart - entity.ViewByte) + 1;

                                        var thirdTargetAddress = ((PDBFileAccessor) this).GetNextPageOffset(secondEnd - 1);

                                        (second, third) = ((ISplittableView) second).Split(thirdTargetAddress, secondEnd);

                                        return second;
                                    }

                                    pPageStart += pageSize;
                                }

                                //No need to split, just return second
                                return second;
                            }
                            else
                                throw new InvalidOperationException("We should not have a ViewEntity around a body unless that body is splittable");
                        }
                        else
                            throw new NotImplementedException("Don't know how to split an entity that doesn't have a kind");

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        //Not related to being in a nested file
        private IStructView GetNestedStructView(ViewByte* pViewByte, ViewByte* limit, long offset, ViewKind viewKind)
        {
            //Rewind

            var pStartViewByte = pViewByte;

            do
            {
                if (pStartViewByte->BodyKind == ViewByteBodyKind.SplitHead)
                    throw new NotImplementedException();

                pStartViewByte--;
            } while (pStartViewByte->Kind == ViewByteKind.Body);

            //Get the struct at the head
            var headOffset = (offset - (pViewByte - pStartViewByte));

            var structKind = GetStructKind(headOffset);
            GetMemoryChunkFromAddress(headOffset, out var chunk, out var viewWriter);
            var headStructView = (IStructView) ViewProvider.CreateStructView(structKind, pStartViewByte->GetLength(limit), chunk, viewWriter);

            //Traverse the struct until we find the struct we were looking for

            var current = headStructView;

            var loop = true;

            while (loop)
            {
                loop = false;

                var children = current.Children;

                foreach (var child in children)
                {
                    if (child.Contains(offset))
                    {
                        if (child is IStructFieldView s)
                        {
                            if (s.Kind == viewKind)
                                return s.Value;

                            current = s.Value;
                            loop = true;
                            break;
                        }
                        else if (child is IStructArrayFieldView a)
                            throw new NotImplementedException();
                        else
                            throw new NotImplementedException();
                    }
                }
            }
        }

        #endregion
        #region ViewByte

        /// <summary>
        /// Gets the <see cref="ViewByte"/> item that is pointed to by a given address and section. This is the fastest
        /// way of retrieving the <see cref="ViewByte"/> that is associated with a given address.
        /// </summary>
        /// <param name="address">The target relative address to lookup.</param>
        /// <param name="sectionIndex">The 0-based index of a non-header section to retrieve. This method will do +1 to whatever value is specified to account for the header section that is always listed first.</param>
        /// <returns>The <see cref="ViewByte"/> that is pointed to by the specified address and section.</returns>
        public ViewByte* GetViewByteForSection(int address, int sectionIndex)
        {
            ref var accessor = ref SectionAccessors[sectionIndex + 1]; //The first section is the header

            var relativeOffset = address - accessor.StartAddress;
            Debug.Assert(relativeOffset >= 0);
            Debug.Assert(relativeOffset < accessor.Length);
            Debug.Assert(!accessor.IsEmpty);

            return &accessor.pViewBytes[relativeOffset];
        }
        public ViewByte* GetViewByte(long targetAddress, out int sectionAccessorIndex)
        {
            if (!TryGetViewByte(targetAddress, out var pViewByte, out sectionAccessorIndex))
                throw new InvalidOperationException($"Failed to locate the {nameof(ViewByte)} associated with address 0x{targetAddress:X}");

            return pViewByte;
        }

        public bool TryGetViewByte(long targetAddress, out ViewByte* pViewByte, out int sectionAccessorIndex)
        {
            var low = 0;
            var high = SectionAccessors.Length - 1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;

                ref var current = ref SectionAccessors[mid];

                if (targetAddress >= current.StartAddress)
                {
                    if (targetAddress < current.EndAddress) //I think EndRVA could potentially be equal to the start of the next section, so we need to do < and not <=, since we did start+length to get the end
                    {
                        var relativeOffset = targetAddress - current.StartAddress;

                        //todo: test having a section whose end is right next to the next section, and we stick ourselves in the middle
                        //and cause problems by sharing an address with the startaddress of the section
                        Debug.Assert(!current.IsEmpty);

                        sectionAccessorIndex = mid;
                        pViewByte = &current.pViewBytes[relativeOffset];
                        return true;
                    }
                    else
                    {
                        low = mid + 1;
                    }
                }
                else
                {
                    high = mid - 1;
                }
            }

            pViewByte = default;
            sectionAccessorIndex = default;
            return false;
        }

        #endregion
        #region View Info

        internal ViewByte* SetIsFunction(int targetAddress, int sectionIndex)
        {
            var pViewByte = GetViewByteForSection(targetAddress, sectionIndex);

            pViewByte->IsFunction = true;

            return pViewByte;
        }

        internal bool TryAddData(
            int targetAddress,
            int sectionIndex,
            ViewByteDataKind dataKind,
            int length,
            out ViewByte* pViewByte)
        {
            pViewByte = GetViewByteForSection(targetAddress, sectionIndex);

            //CodeFlags will only return if we're code or unknown, so this check is safe
            if (pViewByte->IsFunction)
            {
                //Watch out; in NativeAOT, there is a symbol [S_GDATA32] RhpAssignRefAVLocation in globals
                //which points to the address of a function. The symbol in publics clearly states its a function,
                //so we need to have logic to say, if we're trying to set something as data that we already
                //known to be a function, ignore the data request
                return false;
            }

            //In devenv.exe we got a completely bogus symbol telling us that __guard_xfg_dispatch_icall_fptr is shortly
            //after the beginning of the load config table, and in sqlncli11.dll we got a symbol that said it was halfway
            //into a RUNTIME_FUNCTION

            //We should not be thinking that something was code and then erroneously declaring that actually it's data
            Debug.Assert(pViewByte->Kind == ViewByteKind.Unknown || pViewByte->Kind == ViewByteKind.Data || pViewByte->Kind == ViewByteKind.Body);

            if (pViewByte->DataKind != ViewByteDataKind.Unknown)
                return true; //We already know about this byte

            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = dataKind;

            var pEnd = pViewByte + length;

            for (var i = pViewByte + 1; i < pEnd; i++)
            {
                Debug.Assert(i->Kind == ViewByteKind.Unknown);
                i->Kind = ViewByteKind.Body;
            }

            return true;
        }

        internal ViewByte* AddString(int targetAddress, int sectionIndex, bool isWide, int numBytes)
        {
            var pViewByte = GetViewByteForSection(targetAddress, sectionIndex);

            Debug.Assert(pViewByte->Kind == ViewByteKind.Unknown || pViewByte->Kind == ViewByteKind.Data);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.String;
            pViewByte->IsWide = isWide;

            var pEnd = pViewByte + numBytes;

            for (var i = pViewByte + 1; i < pEnd; i++)
            {
                i->Kind = ViewByteKind.Body;
            }

            return pViewByte;
        }

        #region Struct

        internal bool AddStruct(FileAnalyzer fileAnalyzer, int targetAddress, int sectionIndex, ViewKind kind, int length)
        {
            var pViewByte = GetViewByteForSection(targetAddress, sectionIndex);

            if (pViewByte->Kind == ViewByteKind.Data)
                return false; //Perhaps we got a duplicate symbol at this address

            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Struct;
            CheckName(targetAddress);
            pViewByte->HasName = true;
            AddStructKind(targetAddress, kind);

            var pEnd = pViewByte + length;

            for (var i = pViewByte + 1; i < pEnd; i++)
            {
                Debug.Assert(i->Kind == ViewByteKind.Unknown);
                i->Kind = ViewByteKind.Body;
            }

            return true;
        }

        internal void AddStructKind(long targetAddress, ViewKind kind)
        {
#if NET9_0_OR_GREATER
            ref var data = ref CollectionsMarshal.GetValueRefOrAddDefault(_infoMap, targetAddress, out _);
            data.ViewKind = kind;
#else
            if (!_infoMap.TryGetValue(targetAddress, out var data))
            {
                data = new ViewInfo();
            }

            data.ViewKind = kind;

            _infoMap[targetAddress] = data;
#endif
        }

        public ViewKind GetStructKind(long targetAddress) => _infoMap[targetAddress].ViewKind;

        public bool TryGetStructKind(long targetAddress, out ViewKind kind)
        {
            if (_infoMap.TryGetValue(targetAddress, out var value))
            {
                kind = value.ViewKind;
                return true;
            }

            kind = default;
            return false;
        }

        #endregion
        #region XRef

        public Span<XRef> GetXRefs(long targetAddress)
        {
            if (_xrefs == null)
                return default;

            if (!_infoMap.TryGetValue(targetAddress, out var value))
                return default; 

            var handle = value.XRefs;

            if (handle.IsEmpty)
                return default;

            return _xrefs.GetSpan(handle);
        }

        internal XRef[] GetXRefBuffer() => _xrefs._buffer;

        internal SpanAllocatorHandle GetXRefsHandle(long targetAddress)
        {
            if (_xrefs == null)
                throw new InvalidOperationException("XRefs were not tracked by this analysis. Ensure that trackXRefs: true is specified");

            if (!_infoMap.TryGetValue(targetAddress, out var value))
                return default;

            return value.XRefs;
        }

        internal XRef GetXRef(SpanAllocatorHandle handle, int index) => _xrefs.GetSpan(handle)[index];

        #endregion
        #endregion

        public abstract bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, long targetAddress, out int rva);

        internal abstract ISectionDataAccessor CreateThreadLocalSectionDataAccessor();

        internal abstract ISymbolAccessor GetSymbolAccessor(
            bool load = false,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default);

        internal FixedUtf8String GetNameFromViewByte(long targetAddress, int sectionAccessorIndex, ViewByte* pViewByte)
        {
            Debug.Assert(pViewByte->HasName);

            if (pViewByte->Kind == ViewByteKind.Data && pViewByte->DataKind == ViewByteDataKind.Struct)
            {
                return ViewProvider.GetName(GetStructKind(targetAddress));
            }

            ref var sectionAccessor = ref SectionAccessors[sectionAccessorIndex];

            if (TryGetCodeName(sectionAccessor, targetAddress, allowDisplacement: false, out var name, out _))
                return name;

            throw new InvalidOperationException($"Failed to get the name of the byte at address 0x{targetAddress:X}. We've been told it has a name, so the name must come from somewhere we don't handle yet");
        }

        internal bool TryGetCodeName(
            in SectionAccessor sectionAccessor,
            long targetAddress,
            bool allowDisplacement,
            out FixedUtf8String name,
            out int displacement)
        {
            name = default;
            displacement = default;

            if (TryGetVirtualAddress(sectionAccessor, targetAddress, out var rva))
            {
                if (_isManaged)
                {
                    //Managed RVAs can erroneously match against the COM+ Entry Point symbol which reports itself as having an off/seg
                    //0x06000001/0, which our symbol matcher will accept since it's prior to section 1 that we're likely asking for.
                    //As such, we need to try for a managed symbol first

                    var peFileAccessor = (PEFileAccessor) this;

                    if (peFileAccessor._rvaToMethodDefMap.TryGetValue(rva, out var methodDef))
                    {
                        var row = peFileAccessor.PEFile.EcmaMetadata.CompressedModelHeap.MethodDefTable[methodDef];

                        name = (FixedUtf8String) row.Name.GetString();
                        return true;
                    }
                }

                //If displacement is not 0, this can't be where the name came from
                if (GetSymbolAccessor().TryGetNameFromAddress(rva, out var symName, out displacement))
                {
                    if (allowDisplacement || displacement == 0)
                    {
                        name = symName;
                        return true;
                    }
                }

                //Must be an export
                if (File.Kind == FileKind.PE)
                {
                    var peFile = (PEFile) File;

                    var exportTable = peFile.ExportTable;

                    if (exportTable != null)
                    {
                        foreach (var export in exportTable.Exports)
                        {
                            if (!export.ForwardOrAddress.IsForward && export.ForwardOrAddress.Address == rva)
                            {
                                name = (FixedUtf8String) export.Name;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        //We used PooledStringBuilder here because the principal usage of this is printing to the UI, which uses PooledStringBuilder
        internal bool GetFullCodeName(
            long targetAddress,
            int sectionAccessorIndex,
            ViewByte* pViewByte,
            ref ValueStringBuilder builder)
        {
            ref var sectionAccessor = ref SectionAccessors[sectionAccessorIndex];

            if (TryGetVirtualAddress(sectionAccessor, targetAddress, out var rva))
            {
                if (_isManaged)
                {
                    //Managed RVAs can erroneously match against the COM+ Entry Point symbol which reports itself as having an off/seg
                    //0x06000001/0, which our symbol matcher will accept since it's prior to section 1 that we're likely asking for.
                    //As such, we need to try for a managed symbol first

                    var peFileAccessor = (PEFileAccessor) this;

                    if (peFileAccessor._rvaToMethodDefMap.TryGetValue(rva, out var methodDef))
                    {
                        var row = peFileAccessor.PEFile.EcmaMetadata.CompressedModelHeap.MethodDefTable[methodDef];

                        var utf8Builder = new Utf8StringBuilder(256);

                        try
                        {
                            row.ToString(ref utf8Builder);

                            builder.Append(utf8Builder.AsSpan());
                        }
                        finally
                        {
                            utf8Builder.Dispose();
                        }

                        return true;
                    }
                }

                //If displacement is not 0, this can't be where the name came from
                if (GetSymbolAccessor().TryGetNameFromAddress(rva, out var symName, out var displacement))
                {
                    builder.Append(symName);

                    if (displacement != 0)
                    {
                        builder.Append(displacement < 0 ? "-0x" : "+0x");
                        builder.AppendHex((ulong) Math.Abs(displacement));
                    }

                    return true;
                }

                //Must be an export
                if (File.Kind == FileKind.PE)
                {
                    var peFile = (PEFile) File;

                    var exportTable = peFile.ExportTable;

                    if (exportTable != null)
                    {
                        foreach (var export in exportTable.Exports)
                        {
                            if (!export.ForwardOrAddress.IsForward && export.ForwardOrAddress.Address == rva)
                            {
                                builder.Append((FixedUtf8String) export.Name);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal bool TryGetNameFromAddress(long targetAddress, out FixedUtf8String name)
        {
            var pViewByte = GetViewByte(targetAddress, out var sectionAccessorIndex);

            ref var sectionAccessor = ref SectionAccessors[sectionAccessorIndex];

            if (TryGetVirtualAddress(sectionAccessor, targetAddress, out var rva))
            {
                //Functions can be split all over the place. We rely on our symbol accessor to be able
                //to make sense of such shenanigans. And if we don't have any symbols, we should be synthesizing them!
                if (GetSymbolAccessor().TryGetNameFromAddress(rva, out var symName, out _))
                {
                    name = symName;
                    return true;
                }
            }

            while (pViewByte->HasFlow)
            {
                do
                {
                    pViewByte--;
                } while (pViewByte->Kind == ViewByteKind.Body);

                Debug.Assert(pViewByte->Kind == ViewByteKind.Code);

                if (pViewByte->IsFunction)
                {
                    entity = GetEntity(pViewByte, sectionAccessorIndex);
                    return true;
                }
            }
        internal void InstallDataDirectories(RegionBuilder[] topLevelDirectories, RegionBuilder[] firstDirectoryByAddress)
        {
            //Enable fast lookup of directories based on offset
            var directoryLookup = new Dictionary<long, int>(firstDirectoryByAddress.Length);

            for (var i = 0; i < firstDirectoryByAddress.Length; i++)
            {
                ref var item = ref firstDirectoryByAddress[i];

                Debug.Assert(item.Length > 0);

                directoryLookup[item.Start] = i;
            }

            _directoryByAddressLookup = directoryLookup;
            _topLevelDirectories = topLevelDirectories;
            _firstDirectoryByAddress = firstDirectoryByAddress;
        }

        internal bool TryGetDirectory(long targetAddress, int depth, out RegionBuilder directory) =>
            TryGetRegionInternal(targetAddress, depth, _directoryByAddressLookup, _firstDirectoryByAddress, out directory);

        internal void InstallRegions(
            List<RegionBuilder> topLevelRegions,
            List<RegionBuilder> firstRegionByAddress,
            List<RegionBuilder> extraRegions)
        {            
            if (extraRegions.Count > 0)
            {
                //Add extraRegions into the master list so we can go about building up the final hierarchy. If there's
                //no extra regions, we're all good because we already handle the hierarchy properly during ViewWriter
                //processing
                topLevelRegions.AddRange(extraRegions);

                topLevelRegions.Sort((a, b) =>
                {
                    var diff = a.Start.CompareTo(b.Start);

                    if (diff != 0)
                        return diff;

                    return b.End.CompareTo(a.End); //Larger items should be listed first
                });

                var parentStack = new Stack<RegionBuilder>();

                var toRemove = new HashSet<RegionBuilder>();

                foreach (var region in topLevelRegions)
                {
                    while (parentStack.Count > 0 && parentStack.Peek().End < region.End)
                        parentStack.Pop();

                    if (parentStack.Count > 0)
                    {
                        var parent = parentStack.Peek();

                        if (parent.Children == null)
                            parent.Children = new List<RegionBuilder>();

                        region.Depth = parent.Depth + 1;

                        parent.Children.Add(region);
                        toRemove.Add(region);
                    }

                    parentStack.Push(region);
                }

                topLevelRegions.RemoveAll(v => toRemove.Contains(v));
            }

            //Enable fast lookup of directories based on offset
            var regionLookup = new Dictionary<long, int>(firstRegionByAddress.Count);

            for (var i = 0; i < firstRegionByAddress.Count; i++)
            {
                var item = firstRegionByAddress[i];

                Debug.Assert(item.Length > 0);

                regionLookup[item.Start] = i;
            }

            //Now merge the extra regions in
            foreach (var region in extraRegions)
            {
                if (regionLookup.TryGetValue(region.Start, out var index))
                {
                    var existingRegion = firstRegionByAddress[index];

                    if (region.Depth < existingRegion.Depth)
                        firstRegionByAddress[index] = region;
                }
                else
                {
                    regionLookup[region.Start] = firstRegionByAddress.Count;
                    firstRegionByAddress.Add(region);
                }
            }

            _regionByAddressLookup = regionLookup;
            _topLevelRegions = topLevelRegions.ToArray();
            _firstRegionByAddress = firstRegionByAddress.ToArray(); //Doesn't need to be sorted
        }

        internal void InstallNestedFileRanges(ViewByteViewWriter viewWriter)
        {
            var rawRanges = viewWriter._nestedFileRanges;

            var results = new NestedFileRange[rawRanges.Count];

            for (var i = 0; i < rawRanges.Count; i++)
            {
                var item = rawRanges[i];

                //If a given nested file ends with padding, and then there's an unallocated area of padding between two nested files,
                //this is going to cause an issue, because the entity right after the end of the first nested file is the "body"
                //of the padding that started inside of the nested file.

                //This is the same sort of logic that we employ in FileAnalyzer.DiscoverDirectories

                results[i] = new NestedFileRange(item.start, item.end, item.file);
            }
            Array.Sort(results, (a, b) => a.StartOffset.CompareTo(b.EndOffset));

            NestedFileRanges = results;
        }

        internal bool TryGetNestedFileRange(long targetAddress, out NestedFileRange range)
        {
            var nestedFileRanges = NestedFileRanges;

            if (nestedFileRanges == null)
            {
                range = default;
                return false;
            }

            var lo = 0;
            var hi = nestedFileRanges.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                ref var candidate = ref nestedFileRanges[mid];

                if (targetAddress < candidate.StartOffset)
                    hi = mid - 1;
                else if (targetAddress >= candidate.EndOffset)
                    lo = mid + 1;
                else
                {
                    if (candidate.NestedWriter == null)
                        candidate.NestedWriter = GetViewWriter().CreateNestedWriter(candidate.File);

                    range = candidate;
                    return true;
                }
            }

            range = default;
            return false;
        }

        internal bool TryGetRegion(long targetAddress, int depth, out RegionBuilder region) =>
            TryGetRegionInternal(targetAddress, depth, _regionByAddressLookup, _firstRegionByAddress, out region);

        private bool TryGetRegionInternal(
            long targetAddress,
            int depth,
            Dictionary<long, int> dict,
            RegionBuilder[] list,
            out RegionBuilder region)
        {
            if (dict == null)
            {
                region = default;
                return false;
            }

            if (dict.TryGetValue(targetAddress, out var regionIndex))
            {
                region = list[regionIndex];

                while (depth > 0)
                {
                    if (region.Children == null)
                        return false;

                    var childRegion = region.Children[0];

                    if (childRegion.Start != region.Start)
                        return false;

                    region = childRegion;

                    depth--;
                }

                return true;
            }

            region = default;
            return false;
        }

        internal void Finalize(
            List<XRef>? xrefs,
            long[] stringAddresses)
        {
            var infoMap = _infoMap;

#if NET
            infoMap.TrimExcess();
#endif
            if (xrefs != null)
            {
                xrefs.Sort((a, b) => a.Self.CompareTo(b.Self));

                var array = xrefs.ToArray();

                var i = 0;

                while (i < array.Length)
                {
                    ref var item = ref array[i];

                    var startAddr = item.Self;

                    var j = i + 1;

                    for (; j < array.Length; j++)
                    {
                        ref var nextItem = ref array[j];

                        if (nextItem.Self != startAddr)
                            break;
                    }

                    var handle = new SpanAllocatorHandle(i, j - i);

                    if (!_infoMap.TryGetValue(startAddr, out var data))
                    {
                        data = new ViewInfo
                        {
                            XRefs = handle
                        };
                        _infoMap[startAddr] = data;

                        //This is the first time we're adding xrefs to this entity, so we need to mark
                        //it as having xrefs
                        var pViewByte = GetViewByte(startAddr, out _);
                        pViewByte->HasXRefs = true;
                    }
                    else
                    {
                        data.XRefs = handle;
                    }

                    _infoMap[startAddr] = data;

                    i = j;
                }

                _xrefs = new SpanAllocator<XRef>(array);
            }

            _stringAddresses = stringAddresses;
        }

        [Conditional("DEBUG")]
        internal void CheckName(long targetAddress)
        {
            //Set a breakpoint here to debug an issue with a given target address
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            if (SectionAccessors != null)
            {
                for (var i = 0; i < SectionAccessors.Length; i++)
                {
                    ref var sectionAccessor = ref SectionAccessors[i];

                    sectionAccessor.Dispose();
                }

                SectionAccessors = default!;
            }

            _disposed = true;
        }
    }
}
