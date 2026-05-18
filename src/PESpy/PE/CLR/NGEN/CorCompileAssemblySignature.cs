using System;
using System.Diagnostics;

namespace PESpy
{
    //CORCOMPILE_ASSEMBLY_SIGNATURE
    [Source(SourceKind.corcompile_h)]
    [DebuggerDisplay("mvid = {mvid}, timeStamp = {timeStamp}, ilImageSize = {ilImageSize}")]
    public readonly struct CorCompileAssemblySignature : IValue
    {
        /// <summary>
        /// MVID used by the metadata of all ngen images
        /// </summary>
        public static readonly Guid NGEN_IMAGE_MVID = new Guid("70E9452F-5F0A-4f0e-8E02-203992F4221C");

        /// <summary>
        /// To indicate that the dependency is not hardbound
        /// </summary>
        public static readonly Guid INVALID_NGEN_SIGNATURE = new Guid("DB15CD8C-1378-4963-9DF3-14D97E95D1A1");

        private const int mvidOffset = 0;
        private const int timeStampOffset = 16;
        private const int ilImageSizeOffset = 20;

        /// <summary>
        /// Metadata MVID.
        /// </summary>
        public Guid mvid => chunk.PeekGuid(mvidOffset);

        // timestamp and IL image size for the source IL assembly.
        // This is used for mini-dump to find matching metadata.

        public Timestamp timeStamp => chunk.PeekUInt32(timeStampOffset);

        public int ilImageSize => chunk.PeekInt32(ilImageSizeOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            16 + //mvid
            sizeof(int) + //timeStamp
            sizeof(int); //ilImageSize

        private readonly MemoryChunk chunk;

        internal CorCompileAssemblySignature(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
