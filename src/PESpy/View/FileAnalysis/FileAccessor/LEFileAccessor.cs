using System;

namespace PESpy.View
{
    internal unsafe class LEFileAccessor : FileAccessor, ISectionDataAccessor
    {
        public LEFile LEFile { get; }

        public LEFileAccessor(LEFile leFile) : base(leFile, bitness: 32)
        {
            LEFile = leFile;
            FileViewKind = ViewKind.LEFile;
        }

        protected override object CreateOverview()
        {
            throw new NotImplementedException();
        }

        public override bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex)
        {
            throw new NotImplementedException();
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        bool ISectionDataAccessor.TryGetOffSeg(int rva, out int off, out ushort seg)
        {
            throw new NotImplementedException();
        }

        void ISectionDataAccessor.GetRawSectionData(int targetAddress, out byte* pByte, out int remainingLength)
        {
            throw new NotImplementedException();
        }

        void ISectionDataAccessor.GetRawSectionData(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, long targetAddress, out int rva)
        {
            //For now, we don't support reporting on the "true" virtual addresses in each segment
            rva = default;
            return false;
        }
    }
}
