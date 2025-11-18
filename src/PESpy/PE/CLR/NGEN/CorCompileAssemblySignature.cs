using System;
using System.Diagnostics;

namespace PESpy
{
    //CORCOMPILE_ASSEMBLY_SIGNATURE
    [Source(SourceKind.corcompile_h)]
    [DebuggerDisplay("mvid = {mvid}, timeStamp = {timeStamp}, ilImageSize = {ilImageSize}")]
    public readonly struct CorCompileAssemblySignature : IValue
    {
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

        public int Offset => chunk.AbsoluteOffset;

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
