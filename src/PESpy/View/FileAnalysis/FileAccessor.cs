using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ClrDebug;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy.View
{
    /// <summary>
    /// Provides facilities for accessing the bytes of a non-specific file type.
    /// </summary>
    public abstract unsafe class FileAccessor : IDisposable
    {
        /* When we were storing ViewKind, FixedUtf8String and SpanAllocatorHandle (total: 24 bytes)
         * in our ViewInfo, in a stress test against in msedge.dll (which is over 300mb) there were
         * 16.9m entries in the _infoMap, which meant our _infoMap required 405mb just to store all the
         * Only 2 million (12%) of these records actually had a name, and only 1 million actually had a
         * distinct name, meaning we were paying 202mb just to store names when we only needed to be paying
         * 12mb. If we move the 12 byte FixedUtf8String records out into their own separate array, only storing
         * a single distinct record per name, and then give each ViewInfo record a 4 byte index into this array,
         * we can cut down the memory usage required to store names down from 202mb to 79.9mb
         * 
         * Using this memory that we've freed up, we can implement a reverse "name to things that use that name" lookup.
         * Alongside our new FixedUtf8String[] we implement a SpanAllocatorHandle[] and a temporary Dictionary<FixedUtf8String, int>
         * to be used during construction. The SpanAllocator holds all of the entities that refer to a given name, and the
         * FixedUtf8String[] / SpanAllocatorHandle[] arrays are parallel arrays that provide a mechanism of resolving a given name
         */
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct ViewInfo
        {
            //I tried reordering this to make it 14 bytes with pack 2 but that didn't make any difference
            public ViewKind ViewKind;
            public int NameIndex;
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
        public int Length { get; protected set; }

        public abstract IFile File { get; }

        private object _overview;
        private SpanAllocator<XRef> _xrefs;

        protected FixedUtf8String[] _names;
        private SpanAllocatorHandle[] _nameRefHandles;
        private SpanAllocator<int> _nameRefAllocator;

        private int[] _stringAddresses;

        public object Overview => _overview ??= CreateOverview();

        internal Dictionary<int, ViewInfo> _infoMap = new Dictionary<int, ViewInfo>();
        private bool _disposed;

        protected FileAccessor(int bitness)
        {
            _xrefs = new SpanAllocator<XRef>(100);

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

        protected static int GetBitness(IMAGE_FILE_MACHINE machine)
        {
            switch (machine)
            {
                case IMAGE_FILE_MACHINE_I386:
                    return 32;

                case IMAGE_FILE_MACHINE_AMD64:
                    return 16;

                default:
                    throw new NotImplementedException($"Don't know how to get the bitness of machine '{machine}'");
            }
        }

        public abstract bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex);

        public ViewEntity GetEntity(int address)
        {
            var pViewByte = GetViewByte(address, out var sectionIndex);

            ref var accessor = ref SectionAccessors[sectionIndex];

            var pBytes = GetRawSectionData(accessor);

            return new ViewEntity(
                GetSymbolAccessor(),
                sectionIndex,
                address,
                pViewByte,
                accessor.pViewBytes,
                accessor.pViewBytes + accessor.Length,
                pBytes,
                _infoMap,
                _names
            );
        }

        //For when you already have all the various pieces
        internal unsafe ViewEntity GetEntity(
            int sectionIndex,
            int targetAddress,
            ViewByte* pViewByte,
            in SectionAccessor sectionAccessor,
            IntPtr pBytes)
        {
            return new ViewEntity(
                GetSymbolAccessor(),
                sectionIndex,
                targetAddress,
                pViewByte,
                sectionAccessor.pViewBytes,
                sectionAccessor.pViewBytes + sectionAccessor.Length,
                pBytes,
                _infoMap,
                _names
            );
        }

        public ViewEntity GetEntity(int address, int sectionIndex)
        {
            ref var accessor = ref SectionAccessors[sectionIndex + 1]; //The first section is the header

            var pBytes = GetRawSectionData(accessor);

            var relativeOffset = address - accessor.StartAddress;
            Debug.Assert(relativeOffset >= 0);
            Debug.Assert(!accessor.IsEmpty);

            var pViewByte = &accessor.pViewBytes[relativeOffset];

            return new ViewEntity(
                GetSymbolAccessor(),
                sectionIndex + 1,
                address,
                pViewByte,
                accessor.pViewBytes,
                accessor.pViewBytes + accessor.Length,
                pBytes,
                _infoMap,
                _names
            );
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
                    var entity = new ViewEntity(symbolAccessor, i, sectionAccessor, j, sectionLength, pBytes, _infoMap, _names);

                    j += entity.Length;

                    yield return entity;
                }
            }
        }

        public void EnumerateEntitiesMatchingName(FixedUtf8String utf8String, FixedUtf16String utf16String, Func<ViewEntity, int, int, bool> callback)
        {
            var local = _names;

            var utf8Span = utf8String.AsSpan();
            for (var i = 0; i < local.Length; i++)
            {
                ref var item = ref local[i];

                var index = StringHelpers.IndexOfIgnoreCase(item.AsSpan(), utf8Span);

                if (index != -1)
                {
                    var owners = _nameRefAllocator.GetSpan(_nameRefHandles[i]);

                    foreach (var owner in owners)
                    {
                        var entity = GetEntity(owner);

                        if (!callback(entity, index, utf8Span.Length))
                            return;
                    }
                }
            }

            //Now try strings (these may either be UTF8 or UTF16)

            var utf16Span = utf16String.AsSpan();

            var stringAddresses = _stringAddresses;

            for (var i = 0; i < stringAddresses.Length; i++)
            {
                var targetAddress = stringAddresses[i];

                var pViewByte = GetViewByte(targetAddress, out var sectionAccessorIndex);
                Debug.Assert(pViewByte->Kind == ViewByteKind.Data && pViewByte->DataKind == ViewByteDataKind.String);

                ref var sectionAccessor = ref SectionAccessors[sectionAccessorIndex];

                var pBytes = GetRawSectionData(sectionAccessor);

                var pEnd = sectionAccessor.pViewBytes + sectionAccessor.Length;

                var pBody = pViewByte + 1;

                while (pBody < pEnd)
                {
                    if (pBody->Kind == ViewByteKind.Body)
                    {
                        if (pBody->BodyKind == ViewByteBodyKind.SplitTail)
                            throw new NotImplementedException();

                        pBody++;
                    }
                    else
                        break;
                }

                var relativeOffset = (int) (pViewByte - sectionAccessor.pViewBytes);
                var length = (int) (pBody - pViewByte);

                if (pViewByte->IsWide)
                {
                    var item = new FixedUtf16String((char*) (pBytes + relativeOffset), length / 2);

                    var index = StringHelpers.IndexOfIgnoreCase(item.AsSpan(), utf16Span);

                    if (index != -1)
                    {
                        var entity = GetEntity(sectionAccessorIndex, targetAddress, pViewByte, sectionAccessor, pBytes);

                        if (!callback(entity, index, utf8Span.Length))
                            return;
                    }
                }
                else
                {
                    var item = new FixedUtf8String((byte*) pBytes + relativeOffset, length);

                    var index = StringHelpers.IndexOfIgnoreCase(item.AsSpan(), utf8Span);

                    if (index != -1)
                    {
                        var entity = GetEntity(sectionAccessorIndex, targetAddress, pViewByte, sectionAccessor, pBytes);

                        if (!callback(entity, index, utf8Span.Length))
                            return;
                    }
                }
            }
        }

        public IEnumerable<ViewEntity> EnumerateEntities(int sectionAccessorIndex)
        {
            var symbolAccessor = GetSymbolAccessor();

            var sectionAccessor = SectionAccessors[sectionAccessorIndex];
            var sectionLength = sectionAccessor.Length;

            var pBytes = GetRawSectionData(sectionAccessor);

            var j = 0;

            while (j < sectionLength)
            {
                //We can't use unsafe in an iterator, so we need to put all the logic in the FileEntity ctor
                var entity = new ViewEntity(symbolAccessor, sectionAccessorIndex, sectionAccessor, j, sectionLength, pBytes, _infoMap, _names);

                j += entity.Length;

                yield return entity;
            }
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
        internal abstract MemoryChunk GetMemoryChunkFromAddress(int address);

        #region GetStructView

        protected abstract ViewWriter GetViewWriter();

        public IStructView GetStructView(IViewable viewable) =>
            (IStructView) viewable.WriteStruct(GetViewWriter());

        //For when the type of view you're after may not be top level. When it's top level
        //it is possible to ask the info map what the ViewKind is
        public unsafe IStructView GetStructView(int targetAddress, ViewKind viewKind)
        {
            var pViewByte = GetViewByte(targetAddress, out _);

            if (pViewByte->Kind == ViewByteKind.Body)
            {
                return GetNestedStructView(pViewByte, targetAddress, viewKind);
            }
            else
            {
                var chunk = GetMemoryChunkFromAddress(targetAddress);
                var structView = ViewProvider.CreateStructView(viewKind, chunk, GetViewWriter());

                return structView;
            }
        }

        private IStructView GetNestedStructView(ViewByte* pViewByte, int offset, ViewKind viewKind)
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
            var headOffset = (int) (offset - (pViewByte - pStartViewByte));

            var structKind = GetStructKind(headOffset);
            var chunk = GetMemoryChunkFromAddress(headOffset);
            var headStructView = ViewProvider.CreateStructView(structKind, chunk, GetViewWriter());

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
            Debug.Assert(!accessor.IsEmpty);

            return &accessor.pViewBytes[relativeOffset];
        }
        public ViewByte* GetViewByte(int targetAddress, out int sectionAccessorIndex)
        {
            if (!TryGetViewByte(targetAddress, out var pViewByte, out sectionAccessorIndex))
                throw new InvalidOperationException($"Failed to locate the {nameof(ViewByte)} associated with address 0x{targetAddress:X}");

            return pViewByte;
        }

        public bool TryGetViewByte(int targetAddress, out ViewByte* pViewByte, out int sectionAccessorIndex)

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

        internal ViewByte* AddData(int targetAddress, int sectionIndex, ViewByteDataKind dataKind, int length)
        {
            var pViewByte = GetViewByteForSection(targetAddress, sectionIndex);

            //We should not be thinking that something was code and then erroneously declaring that actually it's data
            Debug.Assert(pViewByte->Kind == ViewByteKind.Unknown || pViewByte->Kind == ViewByteKind.Data);

            if (pViewByte->DataKind != ViewByteDataKind.Unknown)
                return pViewByte; //We already know about this byte

            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = dataKind;

            var pEnd = pViewByte + length;

            for (var i = pViewByte + 1; i < pEnd; i++)
            {
                Debug.Assert(i->Kind == ViewByteKind.Unknown);
                i->Kind = ViewByteKind.Body;
            }

            return pViewByte;
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
                Debug.Assert(i->Kind == ViewByteKind.Unknown);
                i->Kind = ViewByteKind.Body;
            }

            return pViewByte;
        }

        #region Name

        //Should only be called by the analyzer, which is responsible for doing the rest of the "real"
        //bookkeeping
        internal void AddName(int targetAddress, int nameIndex)
        {
            //We treat index 0 as "null" so the caller must always skip past it
            Debug.Assert(nameIndex != 0);

            if (!_infoMap.TryGetValue(targetAddress, out var data))
            {
                data = new ViewInfo();
            }

            data.NameIndex = nameIndex;

            _infoMap[targetAddress] = data;
        }

        public FixedUtf8String GetName(int targetAddress) => _names[_infoMap[targetAddress].NameIndex - 1];

        #endregion
        #region Struct

        internal void AddStruct(int targetAddress, int sectionIndex, FixedUtf8String name, ViewKind kind, int length)
        {
            var pViewByte = GetViewByteForSection(targetAddress, sectionIndex);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Struct;

            fileAnalyzer.AddName(targetAddress, pViewByte, name);
            AddStructKind(targetAddress, kind);

            var pEnd = pViewByte + length;

            for (var i = pViewByte + 1; i < pEnd; i++)
            {
                Debug.Assert(i->Kind == ViewByteKind.Unknown);
                i->Kind = ViewByteKind.Body;
            }
        }

        internal void AddStructKind(int targetAddress, ViewKind kind)
        {
            if (!_infoMap.TryGetValue(targetAddress, out var data))
            {
                data = new ViewInfo();
            }

            data.ViewKind = kind;

            _infoMap[targetAddress] = data;
        }

        public ViewKind GetStructKind(int targetAddress) => _infoMap[targetAddress].ViewKind;

        public bool TryGetStructKind(int targetAddress, out ViewKind kind)
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

        public unsafe void AddXRef(int source, int target)
        {
            void AddXRef(int address, XRef xref)
            {
                List<XRef> xrefs;

                if (!_infoMap.TryGetValue(address, out var data))
                {
                    var handle = _xrefs.Alloc(new Span<XRef>(&xref, 1));

                    data = new ViewInfo
                    {
                        XRefs = handle
                    };
                    _infoMap[address] = data;

                    //This is the first time we're adding xrefs to this entity, so we need to mark
                    //it as having xrefs
                    var pViewByte = GetViewByte(address, out _);
                    pViewByte->HasXRefs = true;
                }
                else
                {
                    if (data.XRefs.IsEmpty)
                    {
                        data.XRefs = _xrefs.Alloc(new Span<XRef>(&xref, 1));
                        _infoMap[address] = data;

                        //This is the first time we're adding xrefs to this entity, so we need to mark
                        //it as having xrefs
                        var pViewByte = GetViewByte(address, out _);
                        pViewByte->HasXRefs = true;
                    }
                    else
                    {
                        data.XRefs = _xrefs.Realloc(data.XRefs, new Span<XRef>(&xref, 1));
                    }
                }
            }

            AddXRef(source, new XRef(self: source, other: target, kind: XRefKind.From));
            AddXRef(target, new XRef(self: target, other: source, kind: XRefKind.To));

            //Binary search source to see if we already have this target. 
            //todo: but we're not doing that. do we need to do that?
        }

        public Span<XRef> GetXRefs(int targetAddress)
        {
            var handle = _infoMap[targetAddress].XRefs;

            Debug.Assert(!handle.IsEmpty);

            return _xrefs.GetSpan(handle);
        }

        #endregion
        #endregion

        internal abstract bool TryGetDataSymbol(ulong address, int rva, out FixedUtf8String name, out int displacement);

        public abstract bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, int targetAddress, out int rva);

        internal abstract ISectionDataAccessor CreateThreadLocalSectionDataAccessor();

        internal abstract ISymbolAccessor GetSymbolAccessor(ILocatorProgress? progress = null);

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
