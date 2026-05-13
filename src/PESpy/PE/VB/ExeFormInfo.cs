using System;
using PESpy.View;

namespace PESpy.VB
{
    //EXEFORMINFO
    //Referenced in msvbvm60!CProjitemVisual::FLoadFromExe(EXEFORMINFO)
    public readonly struct ExeFormInfo : IViewableValue
    {
        private const int dwStructSizeOffset = 0;
        private const int uuidObjectGUIOffset = 4;
        private const int dwUnknown1Offset = 20;
        private const int dwUnknown2Offset = 24;
        private const int dwUnknown3Offset = 28;
        private const int dwUnknown4Offset = 32;
        private const int lObjectIDOffset = 36;
        private const int dwUnknown5Offset = 40;
        private const int fOLEMiscOffset = 44;
        private const int uuidObjectOffset = 48;
        private const int dwSizeOfFormBodyOffset = 64;
        private const int dwUnknown7Offset = 68;
        private const int lpFormBodyOffset = 72;

        public int dwStructSize => chunk.PeekInt32(dwStructSizeOffset);
        public Guid uuidObjectGUI => chunk.PeekGuid(uuidObjectGUIOffset);
        public int dwUnknown1 => chunk.PeekInt32(dwUnknown1Offset);
        public int dwUnknown2 => chunk.PeekInt32(dwUnknown2Offset);
        public int dwUnknown3 => chunk.PeekInt32(dwUnknown3Offset);
        public int dwUnknown4 => chunk.PeekInt32(dwUnknown4Offset);
        public int lObjectID => chunk.PeekInt32(lObjectIDOffset);
        public int dwUnknown5 => chunk.PeekInt32(dwUnknown5Offset);
        public int fOLEMisc => chunk.PeekInt32(fOLEMiscOffset);
        public Guid uuidObject => chunk.PeekGuid(uuidObjectOffset);
        public int dwSizeOfFormBody => chunk.PeekInt32(dwSizeOfFormBodyOffset);
        public int dwUnknown7 => chunk.PeekInt32(dwUnknown7Offset);
        public int lpFormBody => chunk.PeekInt32(lpFormBodyOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //dwStructSize
            16 + //uuidObjectGUI
            sizeof(int) + //dwUnknown1
            sizeof(int) + //dwUnknown2
            sizeof(int) + //dwUnknown3
            sizeof(int) + //dwUnknown4
            sizeof(int) + //lObjectID
            sizeof(int) + //dwUnknown5
            sizeof(int) + //fOLEMisc
            16 + //uuidObject
            sizeof(int) + //dwSizeOfFormBody
            sizeof(int) + //dwUnknown7
            sizeof(int); //lpFormBody

        private readonly MemoryChunk chunk;

        internal ExeFormInfo(MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        //dwStructSize was 80 while the total size we've accounted for is 76. In ExeOcxInfo we encountered an issue
        //wherein the dwStructSize is way bigger than what we've accounted for, and collides with something after it.
        //As such, we won't use dwStructSize here
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ExeFormInfo, StructSize);

        int IViewable.NumChildren() => 13;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(dwStructSize), dwStructSizeOffset, dwStructSize);
                    break;

                case 1:
                    structWriter.WriteField(nameof(uuidObjectGUI), uuidObjectGUIOffset, uuidObjectGUI);
                    break;

                case 2:
                    structWriter.WriteField(nameof(dwUnknown1), dwUnknown1Offset, dwUnknown1);
                    break;

                case 3:
                    structWriter.WriteField(nameof(dwUnknown2), dwUnknown2Offset, dwUnknown2);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dwUnknown3), dwUnknown3Offset, dwUnknown3);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwUnknown4), dwUnknown4Offset, dwUnknown4);
                    break;

                case 6:
                    structWriter.WriteField(nameof(lObjectID), lObjectIDOffset, lObjectID);
                    break;

                case 7:
                    structWriter.WriteField(nameof(dwUnknown5), dwUnknown5Offset, dwUnknown5);
                    break;

                case 8:
                    structWriter.WriteField(nameof(fOLEMisc), fOLEMiscOffset, fOLEMisc);
                    break;

                case 9:
                    structWriter.WriteField(nameof(uuidObject), uuidObjectOffset, uuidObject);
                    break;

                case 10:
                    structWriter.WriteField(nameof(dwSizeOfFormBody), dwSizeOfFormBodyOffset, dwSizeOfFormBody);
                    break;

                case 11:
                    structWriter.WriteField(nameof(dwUnknown7), dwUnknown7Offset, dwUnknown7);
                    break;

                case 12:
                    structWriter.WriteField(nameof(lpFormBody), lpFormBodyOffset, lpFormBody);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
