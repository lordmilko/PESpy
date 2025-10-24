using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;

namespace PESpy.View
{
    /// <summary>
    /// Provides facilities for accessing the bytes of a non-specific file type.
    /// </summary>
    public abstract unsafe class FileAccessor : IDisposable
    {
        internal struct ViewInfo
        {
            public ViewKind ViewKind;
            public FixedUtf8String Name;
            public List<XRef> XRefs;
        }

        /// <summary>
        /// Gets the bitness of the code contained in this file, or the bitness of the code this file is associated with.
        /// </summary>
        public int Bitness { get; }

        public SectionAccessor[] SectionAccessors { get; protected set; }

        //todo: this doesnt make sense for all file types, but our disasm needs this
        public long ImageBase { get; protected set; }

        /// <summary>
        /// Gets the total length of the file.
        /// </summary>
        public int Length { get; protected set; }

        internal Dictionary<int, ViewInfo> _infoMap = new Dictionary<int, ViewInfo>();
        private bool _disposed;

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

        protected static int GetBitness(IMAGE_FILE_MACHINE machine)
        {
            switch (machine)
            {
                case IMAGE_FILE_MACHINE.I386:
                    return 32;

                case IMAGE_FILE_MACHINE.AMD64:
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

            return new ViewEntity(
                address,
                pViewByte,
                accessor.pViewBytes,
                accessor.pViewBytes + accessor.Length,
                _infoMap
            );
        }

        public ViewEntity GetEntity(int address, int sectionIndex)
        {
            ref var accessor = ref SectionAccessors[sectionIndex + 1]; //The first section is the header

            var relativeOffset = address - accessor.StartAddress;
            Debug.Assert(relativeOffset >= 0);
            Debug.Assert(!accessor.IsEmpty);

            var pViewByte = &accessor.pViewBytes[relativeOffset];

            return new ViewEntity(
                address,
                pViewByte,
                accessor.pViewBytes,
                accessor.pViewBytes + accessor.Length,
                _infoMap
            );
        }

        public ViewEntity[] Entities => EnumerateEntities().ToArray();

        public IEnumerable<ViewEntity> EnumerateEntities()
        {
            for (var i = 0; i < SectionAccessors.Length; i++)
            {
                var sectionAccessor = SectionAccessors[i];
                var sectionLength = sectionAccessor.Length;

                var j = 0;

                while (j < sectionLength)
                {
                    //We can't use unsafe in an iterator, so we need to put all the logic in the FileEntity ctor
                    var entity = new ViewEntity(sectionAccessor, j, sectionLength, _infoMap);

                    j += entity.Length;

                    yield return entity;
                }
            }
        }

        public abstract unsafe void GetRawSectionData(in SectionAccessor sectionAccessor, out byte* pByte, out int rva, out int remainingLength);

        internal abstract MemoryChunk GetMemoryChunk(int rva);

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

        public ViewByte* GetViewByte(int address, out int sectionAccessorIndex)
        {
            var low = 0;
            var high = SectionAccessors.Length - 1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;

                ref var current = ref SectionAccessors[mid];

                if (address >= current.StartAddress)
                {
                    if (address < current.EndAddress) //I think EndRVA could potentially be equal to the start of the next section, so we need to do < and not <=, since we did start+length to get the end
                    {
                        var relativeOffset = address - current.StartAddress;

                        //todo: test having a section whose end is right next to the next section, and we stick ourselves in the middle
                        //and cause problems by sharing an address with the startaddress of the section
                        Debug.Assert(!current.IsEmpty);

                        sectionAccessorIndex = mid;
                        return &current.pViewBytes[relativeOffset];
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

            throw new InvalidOperationException($"Failed to locate the {nameof(ViewByte)} associated with address 0x{address:X}");
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

        internal void AddName(int targetAddress, ViewByte* pViewByte, FixedUtf8String name)
        {
            Debug.Assert(name.Length > 0);

            if (!_infoMap.TryGetValue(targetAddress, out var data))
            {
                data = new ViewInfo();
            }

            data.Name = name;

            _infoMap[targetAddress] = data;

            pViewByte->HasName = true;
        }

        public FixedUtf8String GetName(int targetAddress) => _infoMap[targetAddress].Name;

        #endregion
        #region Struct

        internal void AddStruct(int targetAddress, int sectionIndex, FixedUtf8String name, ViewKind kind, int length)
        {
            var pViewByte = GetViewByteForSection(targetAddress, sectionIndex);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Struct;

            AddName(targetAddress, pViewByte, name);
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

        public void AddXRef(int source, int target)
        {
            List<XRef> GetXRefs(int address)
            {
                List<XRef> xrefs;

                if (!_infoMap.TryGetValue(source, out var data))
                {
                    xrefs = new List<XRef>();
                    data = new ViewInfo
                    {
                        XRefs = xrefs
                    };
                    _infoMap[source] = data;

                    return xrefs;
                }
                else
                {
                    if (data.XRefs == null)
                    {
                        xrefs = new List<XRef>();
                        data.XRefs = xrefs;
                        _infoMap[source] = data;

                        return xrefs;
                    }
                    else
                        return data.XRefs;
                }
            }

            var sourceXRefs = GetXRefs(source);
            var destXRefs = GetXRefs(source);

            //Binary search source to see if we already have this target. 
            //todo: but we're not doing that. do we need to do that?

            sourceXRefs.Add(new XRef(self: source, other: target, kind: XRefKind.From));
            destXRefs.Add(new XRef(self: target, other: source, kind: XRefKind.To));
        }

        #endregion
        #endregion

        internal abstract ISectionDataAccessor CreateThreadLocalSectionDataAccessor();

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
