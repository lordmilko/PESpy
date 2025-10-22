using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="CLR_DEBUG_RESOURCE"/> structure that describes the DBI/DAC module that is associated
    /// with a CLR build. This structure is found in coreclr.dll <see cref="ResourceType.RCData"/> entries whose names begin with "CLRDEBUGINFO".
    /// </summary>
    public class ClrDebugResource : IValue, IViewable //This gets boxed in an IValue anyway, so is a class
    {
        /// <summary>
        /// This guid first appears in version 4.0 of CLR on x86 and amd64 - earlier versions had no resource
        /// </summary>
        public static readonly Guid CLR_ID_V4_DESKTOP = new Guid("267F3989-D786-4b9a-9AF6-D19E42D557EC");

        /// <summary>
        /// This guid has been set aside for CoreCLR usage - at present CoreCLR doesn't use it though
        /// </summary>
        public static readonly Guid CLR_ID_CORECLR = new Guid("8CB8E075-0A91-408E-9228-D66E00A3BFF6");

        /// <summary>
        /// This guid first appears in the CoreCLR port to Windows Phone 8 - note that it is separate from the CoreCLR id because it will
        /// potentially have a different versioning lineage than CoreCLR
        /// </summary>
        public static readonly Guid CLR_ID_PHONE_CLR = new Guid("E7237E9C-31C0-488C-AD48-324D3E7ED92A");

        /// <summary>
        /// This guid first appears 8/19/14 as CoreCLR evolves to OneCore, ProjectK, and versions of Phone after PhoneBlue
        /// The new guid intentionally creates a breaking change so we can simplify the file naming on mscordaccore.dll and mscordbi.dll
        /// in xplat hosting scenarios. Old versions of dbgshim.dll will not be able to support this.
        /// </summary>
        public static readonly Guid CLR_ID_ONECORE_CLR = new Guid("B1EE760D-6C4A-4533-BA41-6F4F661FABAF");

        private const int VersionOffset = 0;
        private const int SignatureOffset = 4;
        private const int DacTimeStampOffset = 20;
        private const int DacSizeOfImageOffset = 24;
        private const int DbiTimeStampOffset = 28;
        private const int DbiSizeOfImageOffset = 32;

        public int Version => chunk.PeekInt32(VersionOffset);
        public Guid Signature => chunk.PeekGuid(SignatureOffset);
        public int DacTimeStamp => chunk.PeekInt32(DacTimeStampOffset);
        public int DacSizeOfImage => chunk.PeekInt32(DacSizeOfImageOffset);
        public int DbiTimeStamp => chunk.PeekInt32(DbiTimeStampOffset);
        public int DbiSizeOfImage => chunk.PeekInt32(DbiSizeOfImageOffset);

        public int Offset => chunk.AbsoluteOffset;

        public const int StructSize =
            sizeof(int) + //Version
            16 + //Signature
            sizeof(int) + //DacTimeStamp
            sizeof(int) + //DacSizeOfImage
            sizeof(int) + //DbiTimeStamp
            sizeof(int); //DbiSizeOfImage

        private readonly MemoryChunk chunk;

        internal ClrDebugResource(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CLR_DEBUG_RESOURCE, this, ViewKind.ClrDebugResource, StructSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("dwVersion", VersionOffset, Version);
                    break;

                case 1:
                    structWriter.WriteField("signature", SignatureOffset, Signature);
                    break;

                case 2:
                    structWriter.WriteField("dwDacTimeStamp", DacTimeStampOffset, DacTimeStamp);
                    break;

                case 3:
                    structWriter.WriteField("dwDacSizeOfImage", DacSizeOfImageOffset, DacSizeOfImage);
                    break;

                case 4:
                    structWriter.WriteField("dwDbiTimeStamp", DbiTimeStampOffset, DbiTimeStamp);
                    break;

                case 5:
                    structWriter.WriteField("dwDbiSizeOfImage", DbiSizeOfImageOffset, DbiSizeOfImage);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
