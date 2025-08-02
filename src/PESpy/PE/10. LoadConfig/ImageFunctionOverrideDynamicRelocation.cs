using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public struct ImageFunctionOverrideDynamicRelocation : IValue, IViewable
    {
        /// <summary>
        /// RVA of original function
        /// </summary>
        public int OriginalRva => chunk.PeekInt32(0);

        /// <summary>
        /// Offset into the BDD region
        /// </summary>
        public int BDDOffset => chunk.PeekInt32(4);

        /// <summary>
        /// Size in bytes taken by RVAs. Must be multiple of sizeof(int).
        /// </summary>
        public int RvaSize => chunk.PeekInt32(8);

        /// <summary>
        /// Size in bytes taken by BaseRelocs
        /// </summary>
        public int BaseRelocSize => chunk.PeekInt32(12);

        /// <summary>
        /// Array containing overriding func RVAs.
        /// </summary>
        public NativeSpan<int> RVAs => chunk.PeekNativeSpan<int>(16, RvaSize / sizeof(int));

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

        public int Offset => chunk.AbsoluteOffset;
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
            writer.NewStruct(Strings.IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION, this, ViewKind.ImageFunctionOverrideDynamicRelocation, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(OriginalRva), OriginalRva);
            s.WriteField(nameof(BDDOffset), BDDOffset);
            s.WriteField(nameof(RvaSize), RvaSize);
            s.WriteField(nameof(BaseRelocSize), BaseRelocSize);
            s.WriteField(nameof(RVAs), RVAs);
            s.WriteInline(BaseRelocs);

            return s.ToArray();
        }
    }
}
