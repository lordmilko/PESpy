using System;
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

            return s.ToArray();
        }
    }
}
