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
#if PEFAST
        public int OriginalRva => chunk.PeekInt32(0);
#else
        public int OriginalRva { get; }
#endif

        /// <summary>
        /// Offset into the BDD region
        /// </summary>
#if PEFAST
        public int BDDOffset => chunk.PeekInt32(4);
#else
        public int BDDOffset { get; }
#endif

        /// <summary>
        /// Size in bytes taken by RVAs. Must be multiple of sizeof(int).
        /// </summary>
#if PEFAST
        public int RvaSize => chunk.PeekInt32(8);
#else
        public int RvaSize { get; }
#endif

        /// <summary>
        /// Size in bytes taken by BaseRelocs
        /// </summary>
#if PEFAST
        public int BaseRelocSize => chunk.PeekInt32(12);
#else
        public int BaseRelocSize { get; }
#endif

        /// <summary>
        /// Array containing overriding func RVAs.
        /// </summary>
#if PEFAST
        public NativeSpan<int> RVAs => chunk.PeekNativeSpan<int>(16, RvaSize / sizeof(int));
#else
        public int[] RVAs { get; }
#endif

#if PEFAST
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
#else
        public ImageBaseRelocation[] BaseRelocs { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif
        internal int StructSize => 16 + RvaSize + BaseRelocSize;


#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageFunctionOverrideDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            baseRelocs = null;
        }
#else
        internal ImageFunctionOverrideDynamicRelocation(IFileReader reader)
        {
            Offset = (int) reader.Position;

            OriginalRva = reader.ReadInt32();
            BDDOffset = reader.ReadInt32();
            RvaSize = reader.ReadInt32();
            BaseRelocSize = reader.ReadInt32();

            var rvas = new int[RvaSize / sizeof(int)];

            for (var i = 0; i < rvas.Length; i++)
                rvas[i] = reader.ReadInt32();

            RVAs = rvas;

            var end = reader.Position + BaseRelocSize;

            using var baseRelocs = new PooledList<ImageBaseRelocation>();

            // IMAGE_BASE_RELOCATION  BaseRelocs[ANYSIZE_ARRAY]; // Base relocations (RVA + Size + TO)
            // Padded with extra TOs for 4B alignment
            // BaseRelocSize size in bytes
            while (reader.Position < end)
            {
                baseRelocs.Add(new ImageBaseRelocation(reader));

                //Must be 32-bit aligned
                var alignedPosition = (reader.Position + 3) & ~3;

                while (reader.Position < alignedPosition)
                    reader.ReadByte();
            }

            BaseRelocs = baseRelocs.ToArray();
        }
#endif

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
