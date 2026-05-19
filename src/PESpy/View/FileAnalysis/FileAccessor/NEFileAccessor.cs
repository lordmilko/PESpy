using System;
using System.Diagnostics;
using PESpy.NE;

namespace PESpy.View
{
    internal unsafe class NEFileAccessor : FileAccessor, ISectionDataAccessor
    {
        public NEFile NEFile { get; }

        //osdev.org suggests that when the OS is 4, this means it's for Win32s and contains 32-bit code. But
        //Windows says that 4 is DEV_386 and is still 16-bit code, so we need to test that if we ever encounter
        //a NE_DEV386 file
        public NEFileAccessor(NEFile neFile) : base(neFile, bitness: 16) //Apparently NE _can_ be 32-bit hybrid. Don't know how to detect this
        {
            Debug.Assert(neFile.OS2Header.ne_exetyp != NewOperatingSystem.NE_DEV386, "Check if this executable contains 32-bit code");

            NEFile = neFile;
            FileViewKind = ViewKind.NEFile;
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

        internal override ISectionDataAccessor CreateThreadLocalSectionDataAccessor()
        {
            throw new NotImplementedException();
        }
    }
}
