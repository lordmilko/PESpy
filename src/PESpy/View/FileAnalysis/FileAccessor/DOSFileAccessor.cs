using System;

namespace PESpy.View
{
    internal unsafe class DOSFileAccessor : FileAccessor, ISectionDataAccessor
    {
        public DOSFile DOSFile { get; }

        public DOSFileAccessor(DOSFile dosFile) : base(dosFile, bitness: 16)
        {
            DOSFile = dosFile;
            FileViewKind = ViewKind.DOSFile;
        }

        protected override object CreateOverview()
        {
            throw new NotImplementedException();
        }

        public override bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex)
        {
            //Don't currently know how DOS addresses work
            targetAddress = default;
            sectionIndex = default;
            return false;
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
    }
}
