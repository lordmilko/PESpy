using System;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="CLR_DEBUG_RESOURCE"/> structure that describes the DBI/DAC module that is associated
    /// with a CLR build. This structure is found in coreclr.dll <see cref="ResourceType.RCData"/> entries whose names begin with "CLRDEBUGINFO".
    /// </summary>
    public class ClrDebugResource : IValue, IViewable //This gets boxed in an IValue anyway, so is a class
    {
        public static readonly Guid CLR_ID_ONECORE_CLR = new Guid("B1EE760D-6C4A-4533-BA41-6F4F661FABAF");

#if PEFAST
        public int Version => chunk.PeekInt32(0);
#else
        public int Version { get; }
#endif
#if PEFAST
        public Guid Signature => chunk.PeekGuid(4);
#else
        public Guid Signature { get; }
#endif
#if PEFAST
        public int DacTimeStamp => chunk.PeekInt32(20);
#else
        public int DacTimeStamp { get; }
#endif
#if PEFAST
        public int DacSizeOfImage => chunk.PeekInt32(24);
#else
        public int DacSizeOfImage { get; }
#endif
#if PEFAST
        public int DbiTimeStamp => chunk.PeekInt32(28);
#else
        public int DbiTimeStamp { get; }
#endif
#if PEFAST
        public int DbiSizeOfImage => chunk.PeekInt32(32);
#else
        public int DbiSizeOfImage { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        public const int StructSize =
            sizeof(int) + //Version
            16 + //Signature
            sizeof(int) + //DacTimeStamp
            sizeof(int) + //DacSizeOfImage
            sizeof(int) + //DbiTimeStamp
            sizeof(int); //DbiSizeOfImage

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ClrDebugResource(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ClrDebugResource(int version, Guid signature, IFileReader reader)
        {
            Offset = (int) reader.Position - 20;

            Version = version;
            Signature = signature;
            DacTimeStamp = reader.ReadInt32();
            DacSizeOfImage = reader.ReadInt32();
            DbiTimeStamp = reader.ReadInt32();
            DbiSizeOfImage = reader.ReadInt32();
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(CLR_DEBUG_RESOURCE), this, ViewKind.ClrDebugResource);

            s.WriteField("dwVersion", Version);
            s.WriteField("signature", Signature);
            s.WriteField("dwDacTimeStamp", DacTimeStamp);
            s.WriteField("dwDacSizeOfImage", DacSizeOfImage);
            s.WriteField("dwDbiTimeStamp", DbiTimeStamp);
            s.WriteField("dwDbiSizeOfImage", DbiSizeOfImage);
        }
    }
}
