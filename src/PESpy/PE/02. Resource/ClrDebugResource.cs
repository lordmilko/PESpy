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

        public int Version => chunk.PeekInt32(0);
        public Guid Signature => chunk.PeekGuid(4);
        public int DacTimeStamp => chunk.PeekInt32(20);
        public int DacSizeOfImage => chunk.PeekInt32(24);
        public int DbiTimeStamp => chunk.PeekInt32(28);
        public int DbiSizeOfImage => chunk.PeekInt32(32);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("dwVersion", Version);
            s.WriteField("signature", Signature);
            s.WriteField("dwDacTimeStamp", DacTimeStamp);
            s.WriteField("dwDacSizeOfImage", DacSizeOfImage);
            s.WriteField("dwDbiTimeStamp", DbiTimeStamp);
            s.WriteField("dwDbiSizeOfImage", DbiSizeOfImage);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
