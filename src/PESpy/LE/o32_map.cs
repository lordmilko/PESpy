using System.Diagnostics;

namespace PESpy.LE
{
    //exe_vhd.h
    //o32_map
    [DebuggerDisplay("[{o32_pageflags}] 0x{o32_pageidx.ToString(\"X\"),nq}")]
    public readonly struct o32_map
    {
        /// <summary>
        /// 24-bit page # in .EXE file
        /// </summary>
        public int o32_pageidx => chunk.PeekInt24(0);

        /// <summary>
        /// Per-Page attributes
        /// </summary>
        public PageMapAttributes o32_pageflags => (PageMapAttributes) chunk.PeekByte(3);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize = sizeof(int);

        private readonly MemoryChunk chunk;

        internal o32_map(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
