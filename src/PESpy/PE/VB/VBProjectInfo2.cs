using System;
using PESpy.View;

namespace PESpy.VB
{
    public readonly struct VBProjectInfo2 : IViewableValue
    {
        private const int lpHeapLinkOffset = 0;
        private const int lpObjectTableOffset = 4;
        private const int dwReservedOffset = 8;
        private const int dwUnusedOffset = 12;
        private const int lpObjectListOffset = 16;
        private const int dwUnused2Offset = 20;
        private const int szProjectDescriptionOffset = 24;
        private const int szProjectHelpFileOffset = 28;
        private const int dwReserved2Offset = 32;
        private const int dwHelpContextIdOffset = 36;

        /// <summary>
        /// Unused after compilation, always 0.
        /// </summary>
        public int lpHeapLink => chunk.PeekInt32(lpHeapLinkOffset);

        /// <summary>
        /// Back-Pointer to the Object Table.
        /// </summary>
        public int lpObjectTable => chunk.PeekInt32(lpObjectTableOffset);

        /// <summary>
        /// Always set to -1 after compiling. Unused
        /// </summary>
        public int dwReserved => chunk.PeekInt32(dwReservedOffset);

        /// <summary>
        /// Not written or read in any case.
        /// </summary>
        public int dwUnused => chunk.PeekInt32(dwUnusedOffset);

        /// <summary>
        /// Pointer to Object Descriptor Pointers.
        /// </summary>
        public int lpObjectList => chunk.PeekInt32(lpObjectListOffset);

        /// <summary>
        /// Not written or read in any case.
        /// </summary>
        public int dwUnused2 => chunk.PeekInt32(dwUnused2Offset);

        /// <summary>
        /// Pointer to Project Description
        /// </summary>
        public int szProjectDescription => chunk.PeekInt32(szProjectDescriptionOffset);

        /// <summary>
        /// Pointer to Project Help File
        /// </summary>
        public int szProjectHelpFile => chunk.PeekInt32(szProjectHelpFileOffset);

        /// <summary>
        /// Always set to -1 after compiling. Unused
        /// </summary>
        public int dwReserved2 => chunk.PeekInt32(dwReserved2Offset);

        /// <summary>
        /// Help Context ID set in Project Settings
        /// </summary>
        public int dwHelpContextId => chunk.PeekInt32(dwHelpContextIdOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //lpHeapLink
            sizeof(int) + //lpObjectTable
            sizeof(int) + //dwReserved
            sizeof(int) + //dwUnused
            sizeof(int) + //lpObjectList
            sizeof(int) + //dwUnused2
            sizeof(int) + //szProjectDescription
            sizeof(int) + //szProjectHelpFile
            sizeof(int) + //dwReserved2
            sizeof(int); //dwHelpContextId

        private readonly MemoryChunk chunk;

        internal VBProjectInfo2(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            //This is just a backref; doesn't point to any new data
            writer.WriteVAXRef(offset, lpObjectTableOffset, lpObjectTable);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBProjectInfo2, StructSize);

        int IViewable.NumChildren() => 10;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(lpHeapLink), lpHeapLinkOffset, lpHeapLink);
                    break;

                case 1:
                    structWriter.WriteField(nameof(lpObjectTable), lpObjectTableOffset, lpObjectTable);
                    break;

                case 2:
                    structWriter.WriteField(nameof(dwReserved), dwReservedOffset, dwReserved);
                    break;

                case 3:
                    structWriter.WriteField(nameof(dwUnused), dwUnusedOffset, dwUnused);
                    break;

                case 4:
                    structWriter.WriteField(nameof(lpObjectList), lpObjectListOffset, lpObjectList);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwUnused2), dwUnused2Offset, dwUnused2);
                    break;

                case 6:
                    structWriter.WriteField(nameof(szProjectDescription), szProjectDescriptionOffset, szProjectDescription);
                    break;

                case 7:
                    structWriter.WriteField(nameof(szProjectHelpFile), szProjectHelpFileOffset, szProjectHelpFile);
                    break;

                case 8:
                    structWriter.WriteField(nameof(dwReserved2), dwReserved2Offset, dwReserved2);
                    break;

                case 9:
                    structWriter.WriteField(nameof(dwHelpContextId), dwHelpContextIdOffset, dwHelpContextId);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
