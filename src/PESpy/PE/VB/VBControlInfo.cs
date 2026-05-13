using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.VB
{
    public struct VBControlInfo : IViewableValue
    {
        private const int fControlTypeOffset = 0;
        private const int wEventcountOffset = 4;
        private const int bWEventsOffsetOffset = 6;
        private const int lpGuidOffset = 8;
        private const int dwIndexOffset = 12;
        private const int dwNullOffset = 16;
        private const int dwNull2Offset = 20;
        private const int lpEventTableOffset = 24;
        private const int lpIdeDataOffset = 28;
        private const int lpszNameOffset = 32;
        private const int dwIndexCopyOffset = 36;

        /// <summary>
        /// Type of control.
        /// </summary>
        public int fControlType => chunk.PeekInt32(fControlTypeOffset);

        /// <summary>
        /// Number of Event Handlers supported.
        /// </summary>
        public short wEventcount => chunk.PeekInt16(wEventcountOffset);

        /// <summary>
        /// Offset in to Memory struct to copy Events.
        /// </summary>
        public short bWEventsOffset => chunk.PeekInt16(bWEventsOffsetOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<Guid> objectGuid;

        /// <summary>
        /// Pointer to GUID of this Control.
        /// </summary>
        public VA<Guid> lpGuid => ExeProjectInfo.ReadGuid(ref objectGuid, chunk, lpGuidOffset);

        /// <summary>
        /// Index ID of this Control.
        /// </summary>
        public int dwIndex => chunk.PeekInt32(dwIndexOffset);

        /// <summary>
        /// Unused.
        /// </summary>
        public int dwNull => chunk.PeekInt32(dwNullOffset);

        /// <summary>
        /// Unused.
        /// </summary>
        public int dwNull2 => chunk.PeekInt32(dwNull2Offset);

        /// <summary>
        /// Pointer to Event Handler Table.
        /// </summary>
        public int lpEventTable => chunk.PeekInt32(lpEventTableOffset);

        /// <summary>
        /// Valid in IDE only.
        /// </summary>
        public int lpIdeData => chunk.PeekInt32(lpIdeDataOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<AnsiString> name;

        /// <summary>
        /// Name of this Control.
        /// </summary>
        public VA<AnsiString> lpszName => ExeProjectInfo.ReadVAAnsiString(ref name, chunk, lpszNameOffset);

        /// <summary>
        /// Secondary Index ID of this Control.
        /// </summary>
        public int dwIndexCopy => chunk.PeekInt32(dwIndexCopyOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //fControlType
            sizeof(short) + //wEventcount
            sizeof(short) + //bWEventsOffset
            sizeof(int) + //lpGuid
            sizeof(int) + //dwIndex
            sizeof(int) + //dwNull
            sizeof(int) + //dwNull2
            sizeof(int) + //lpEventTable
            sizeof(int) + //lpIdeData
            sizeof(int) + //lpszName
            sizeof(int); //dwIndexCopy

        private readonly MemoryChunk chunk;

        internal VBControlInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteVAPointerField(lpGuid, ViewKind.Guid, offset, lpGuidOffset);
            writer.WriteVAAnsiNullTerminatedField(lpszName, ViewKind.AnsiString, offset, lpszNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBControlInfo, StructSize);

        int IViewable.NumChildren() => 11;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(fControlType), fControlTypeOffset, fControlType);
                    break;

                case 1:
                    structWriter.WriteField(nameof(wEventcount), wEventcountOffset, wEventcount);
                    break;

                case 2:
                    structWriter.WriteField(nameof(bWEventsOffset), bWEventsOffsetOffset, bWEventsOffset);
                    break;

                case 3:
                    structWriter.WriteVAPointerField(nameof(lpGuid), lpGuidOffset, lpGuid);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dwIndex), dwIndexOffset, dwIndex);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwNull), dwNullOffset, dwNull);
                    break;

                case 6:
                    structWriter.WriteField(nameof(dwNull2), dwNull2Offset, dwNull2);
                    break;

                case 7:
                    structWriter.WriteField(nameof(lpEventTable), lpEventTableOffset, lpEventTable);
                    break;

                case 8:
                    structWriter.WriteField(nameof(lpIdeData), lpIdeDataOffset, lpIdeData);
                    break;

                case 9:
                    structWriter.WriteVAAnsiNullTerminatedField(nameof(lpszName), lpszNameOffset, lpszName);
                    break;

                case 10:
                    structWriter.WriteField(nameof(dwIndexCopy), dwIndexCopyOffset, dwIndexCopy);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return lpszName.ToString();
        }
    }
}
