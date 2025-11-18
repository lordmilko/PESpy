using System;
using System.Diagnostics;
using ClrDebug;

namespace PESpy
{
    //CORCOMPILE_DEPENDENCY
    [Source(SourceKind.corcompile_h)]
    [DebuggerDisplay("dwAssemblyRef = {dwAssemblyRef}, dwAssemblyDef = {dwAssemblyDef}, signNativeImage = {signNativeImage}, IsHardBound = {IsHardBound}")]
    public readonly struct CorCompileDepepdency : IValue
    {
        public static readonly Guid INVALID_NGEN_SIGNATURE = new Guid("DB15CD8C-1378-4963-9DF3-14D97E95D1A1");

        private const int dwAssemblyRefOffset = 0;
        private const int dwAssemblyDefOffset = 4;
        private const int signAssemblyDefOffset = 8;
        private const int signNativeImageOffset = 8 + CorCompileAssemblySignature.StructSize;

        /// <summary>
        /// Pre-bind Ref
        /// </summary>
        public mdAssemblyRef dwAssemblyRef => chunk.PeekInt32(dwAssemblyRefOffset);

        /// <summary>
        /// Post-bind Def
        /// </summary>
        public mdAssemblyRef dwAssemblyDef => chunk.PeekInt32(dwAssemblyDefOffset); //The name says def but the type should be ref

        public CorCompileAssemblySignature signAssemblyDef => new CorCompileAssemblySignature(chunk.Slice(signAssemblyDefOffset));

        // INVALID_NGEN_SIGNATURE if this a soft-bound dependency
        public Guid signNativeImage => chunk.PeekGuid(signNativeImageOffset);

        public bool IsHardBound => signNativeImage != INVALID_NGEN_SIGNATURE;

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //dwAssemblyRef
            sizeof(int) + //dwAssemblyDef
            CorCompileAssemblySignature.StructSize + //signAssemblyDef
            16; //signNativeImage

        private readonly MemoryChunk chunk;

        internal CorCompileDepepdency(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
