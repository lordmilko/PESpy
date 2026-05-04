using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public struct ImageFunctionOverrideDynamicRelocation : IValue, IViewable
    {
        private const int OriginalRvaOffset = 0;
        private const int BDDOffsetOffset = 4;
        private const int RvaSizeOffset = 8;
        private const int BaseRelocSizeOffset = 12;
        private const int RVAsOffset = 16;

        /// <summary>
        /// RVA of original function
        /// </summary>
        public int OriginalRva => chunk.PeekInt32(OriginalRvaOffset);

        /// <summary>
        /// Offset into the BDD region
        /// </summary>
        public int BDDOffset => chunk.PeekInt32(BDDOffsetOffset);

        /// <summary>
        /// Size in bytes taken by RVAs. Must be multiple of sizeof(int).
        /// </summary>
        public int RvaSize => chunk.PeekInt32(RvaSizeOffset);

        /// <summary>
        /// Size in bytes taken by BaseRelocs
        /// </summary>
        public int BaseRelocSize => chunk.PeekInt32(BaseRelocSizeOffset);

        /// <summary>
        /// Array containing overriding func RVAs.
        /// </summary>
        public NativeSpan<int> RVAs => chunk.PeekNativeSpan<int>(RVAsOffset, RvaSize / sizeof(int));

        private ImageBaseRelocation[]? baseRelocs;

        public ImageBaseRelocation[] BaseRelocs
        {
            get
            {
                if (baseRelocs == null)
                {
                    var read = 16 + RvaSize;
                    var end = BaseRelocSize + read;

                    using var results = new PooledList<ImageBaseRelocation>();

                    // IMAGE_BASE_RELOCATION  BaseRelocs[ANYSIZE_ARRAY]; // Base relocations (RVA + Size + TO)
                    // Padded with extra TOs for 4B alignment
                    // BaseRelocSize size in bytes
                    while (read < end)
                    {
                        var item = new ImageBaseRelocation(chunk.Slice(read));
                        read += item.SizeOfBlock;
                        results.Add(item);

                        //Must be 32-bit aligned
                        read = (read + 3) & ~3;
                    }

                    Debug.Assert(read == end);

                    baseRelocs = results.ToArray();
                }

                return baseRelocs;
            }
        }

        public long Offset => chunk.AbsoluteOffset;
        internal int StructSize => 16 + RvaSize + BaseRelocSize;

        private readonly MemoryChunk chunk;

        internal ImageFunctionOverrideDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            baseRelocs = null;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageFunctionOverrideDynamicRelocation, StructSize);

        int IViewable.NumChildren() => 5 + BaseRelocs.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(OriginalRva), OriginalRvaOffset, OriginalRva);
                    break;

                case 1:
                    structWriter.WriteField(nameof(BDDOffset), BDDOffsetOffset, BDDOffset);
                    break;

                case 2:
                    structWriter.WriteField(nameof(RvaSize), RvaSizeOffset, RvaSize);
                    break;

                case 3:
                    structWriter.WriteField(nameof(BaseRelocSize), BaseRelocSizeOffset, BaseRelocSize);
                    break;

                case 4:
                    structWriter.WriteField(nameof(RVAs), RVAsOffset, RVAs);
                    break;

                default:
                    structWriter.WriteInline(BaseRelocs[index - 5]);
                    break;
            }
        }
    }
}
