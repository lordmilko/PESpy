using System;
using PESpy.View;

namespace PESpy.VB
{
    //RegInfo
    public readonly struct RegInfo : IViewableValue
    {
        private const int bNextObjectOffset = 0;
        private const int bObjectNameOffset = 4;
        private const int bObjectDescriptionOffset = 8;
        private const int dwInstancingOffset = 12;
        private const int dwObjectIdOffset = 16;
        private const int uuidObjectOffset = 20;
        private const int fIsInterfaceOffset = 36;
        private const int bUuidObjectIFaceOffset = 40;
        private const int bUuidEventsIFaceOffset = 44;
        private const int fHasEventsOffset = 48;
        private const int dwMiscStatusOffset = 52;
        private const int fClassTypeOffset = 56;
        private const int fObjectTypeOffset = 57;
        private const int wToolboxBitmap32Offset = 58;
        private const int wDefaultIconOffset = 60;
        private const int fIsDesignerOffset = 62;
        private const int bDesignerDataOffset = 64;

        /// <summary>
        /// Offset to COM Interfaces Info
        /// </summary>
        public int bNextObject => chunk.PeekInt32(bNextObjectOffset);

        /// <summary>
        /// Offset to Object Name
        /// </summary>
        public int bObjectName => chunk.PeekInt32(bObjectNameOffset);

        /// <summary>
        /// Offset to Object Description
        /// </summary>
        public int bObjectDescription => chunk.PeekInt32(bObjectDescriptionOffset);

        /// <summary>
        /// Instancing Mode
        /// </summary>
        public int dwInstancing => chunk.PeekInt32(dwInstancingOffset);

        /// <summary>
        /// Current Object ID in the Project
        /// </summary>
        public int dwObjectId => chunk.PeekInt32(dwObjectIdOffset);

        /// <summary>
        /// CLSID of Object
        /// </summary>
        public Guid uuidObject => chunk.PeekGuid(uuidObjectOffset);

        /// <summary>
        /// Specifies if the next CLSID is valid
        /// </summary>
        public int fIsInterface => chunk.PeekInt32(fIsInterfaceOffset);

        /// <summary>
        /// Offset to CLSID of Object Interface
        /// </summary>
        public int bUuidObjectIFace => chunk.PeekInt32(bUuidObjectIFaceOffset);

        /// <summary>
        /// Offset to CLSID of Events Interface
        /// </summary>
        public int bUuidEventsIFace => chunk.PeekInt32(bUuidEventsIFaceOffset);

        /// <summary>
        /// Specifies if the CLSID above is valid
        /// </summary>
        public int fHasEvents => chunk.PeekInt32(fHasEventsOffset);

        public OLEMISC dwMiscStatus => (OLEMISC) chunk.PeekUInt32(dwMiscStatusOffset);

        /// <summary>
        /// Class Type
        /// </summary>
        public byte fClassType => chunk.PeekByte(fClassTypeOffset);

        /// <summary>
        /// Flag identifying the Object Type
        /// </summary>
        public VBObjectType fObjectType => (VBObjectType) chunk.PeekByte(fObjectTypeOffset);

        /// <summary>
        /// Control Bitmap ID in Toolbox
        /// </summary>
        public short wToolboxBitmap32 => chunk.PeekInt16(wToolboxBitmap32Offset);

        /// <summary>
        /// Minimized Icon of Control Window
        /// </summary>
        public short wDefaultIcon => chunk.PeekInt16(wDefaultIconOffset);

        /// <summary>
        /// Specifies whether this is a Designer
        /// </summary>
        public short fIsDesigner => chunk.PeekInt16(fIsDesignerOffset);

        /// <summary>
        /// Offset to Designer Data
        /// </summary>
        public int bDesignerData => chunk.PeekInt32(bDesignerDataOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //bNextObject
            sizeof(int) + //bObjectName
            sizeof(int) + //bObjectDescription
            sizeof(int) + //dwInstancing
            sizeof(int) + //dwObjectId
            16 + //uuidObject
            sizeof(int) + //fIsInterface
            sizeof(int) + //bUuidObjectIFace
            sizeof(int) + //bUuidEventsIFace
            sizeof(int) + //fHasEvents
            sizeof(int) + //dwMiscStatus
            sizeof(byte) + //fClassType
            sizeof(byte) + //fObjectType
            sizeof(short) + //wToolboxBitmap32
            sizeof(short) + //wDefaultIcon
            sizeof(short) + //fIsDesigner
            sizeof(int); //bDesignerData

        private readonly MemoryChunk chunk;

        internal RegInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            throw new NotImplementedException();
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            throw new NotImplementedException();

        int IViewable.NumChildren() => throw new NotImplementedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            throw new NotImplementedException();
        }
    }
}
