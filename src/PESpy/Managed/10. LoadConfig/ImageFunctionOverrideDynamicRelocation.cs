using System.Collections.Generic;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageFunctionOverrideDynamicRelocation : IValue, IViewable
    {
        /// <summary>
        /// RVA of original function
        /// </summary>
        public int OriginalRva { get; }

        /// <summary>
        /// Offset into the BDD region
        /// </summary>
        public int BDDOffset { get; }

        /// <summary>
        /// Size in bytes taken by RVAs. Must be multiple of sizeof(int).
        /// </summary>
        public int RvaSize { get; }

        /// <summary>
        /// Size in bytes taken by BaseRelocs
        /// </summary>
        public int BaseRelocSize { get; }

        /// <summary>
        /// Array containing overriding func RVAs.
        /// </summary>
        public int[] RVAs { get; }

        public ImageBaseRelocation[] BaseRelocs { get; }

        public int Offset { get; }

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

            var baseRelocs = new List<ImageBaseRelocation>();

            // IMAGE_BASE_RELOCATION  BaseRelocs[ANYSIZE_ARRAY]; // Base relocations (RVA + Size + TO)
            //  Padded with extra TOs for 4B alignment
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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION), this, ViewKind.ImageFunctionOverrideDynamicRelocation);

            s.WriteField(nameof(OriginalRva), OriginalRva);
            s.WriteField(nameof(BDDOffset), BDDOffset);
            s.WriteField(nameof(RvaSize), RvaSize);
            s.WriteField(nameof(BaseRelocSize), BaseRelocSize);
            s.WriteField(nameof(RVAs), RVAs);
            s.WriteInline(BaseRelocs);
        }
    }
}
