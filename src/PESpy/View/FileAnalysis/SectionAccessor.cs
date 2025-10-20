using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
#if NETSTANDARD
using System.Runtime.CompilerServices;
#endif

namespace PESpy.View
{
    public enum SectionAccessorKind
    {
        Header,
        Section,
        Overlay
    }

    /// <summary>
    /// Provides facilities for accessing information about the bytes contained in a particular section of a file.
    /// </summary>
    [DebuggerDisplay("0x{StartAddress.ToString(\"X\"),nq}-0x{EndAddress.ToString(\"X\"),nq} {Name,nq}")]
    public unsafe struct SectionAccessor : IDisposable
    {
        public ViewByte* pViewBytes;
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _mma;

        public readonly int StartAddress;
        public readonly int EndAddress;
        public readonly SectionAccessorKind Kind;
        public readonly int SectionIndex;
        public readonly string Name;

        public int Length => EndAddress - StartAddress;

        public Span<ViewByte> Bytes => new Span<ViewByte>(pViewBytes, Length);

        public SectionAccessor(
            int startAddress,
            int endAddress,
            SectionAccessorKind kind,
            int sectionIndex,
            string name,
            MemoryMappedFile mmf)
        {
            StartAddress = startAddress;
            EndAddress = endAddress;
            Kind = kind;
            SectionIndex = sectionIndex;
            Name = name;
            _mmf = mmf;
            _mma = mmf.CreateViewAccessor();

#if NETSTANDARD
            RuntimeHelpers.PrepareConstrainedRegions();
#endif

            try
            {
                //Empty; needed to make constrained region work
            }
            finally
            {
                //While MMA does have some helper methods on it that can be used to read certain value types,
                //it acquires/releases the pointer after each value read, inside of a try/finally block, which I feel
                //adds a bit of overhead
                byte* p = default;
                _mma.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
                pViewBytes = (ViewByte*) p;
            }
        }

        public bool IsEmpty => StartAddress == EndAddress;

        //Special ctor in the case a section is empty
        public SectionAccessor(SectionAccessorKind kind, int sectionIndex, int startAddress, string name)
        {
            Kind = kind;
            SectionIndex = sectionIndex;
            Name = name;

            //We don't have any data, but we still need to exist in the list of section accessors so that people can pass in
            //a sectionIndex and we can do a lookup based on it. So we instead do +store the end address of the previous section,
            //and then have special logic when doing a binary search to ignore empty accessors
            StartAddress = startAddress;
            EndAddress = startAddress;
            pViewBytes = default;
            _mma = default!;
            _mmf = default!;
        }

        public void Dispose()
        {
            if (pViewBytes != (byte*) 0 && _mma != null) //If mma is null, it's a fake MMF
            {
#if NETSTANDARD
                RuntimeHelpers.PrepareConstrainedRegions();
#endif

                try
                {
                    //Empty
                }
                finally
                {
                    _mma.SafeMemoryMappedViewHandle.ReleasePointer();
                    pViewBytes = (ViewByte*) 0;
                }
            }

            _mma?.Dispose();
            _mmf?.Dispose();

            _mma = null!;
            _mmf = null!;
        }
    }
}
