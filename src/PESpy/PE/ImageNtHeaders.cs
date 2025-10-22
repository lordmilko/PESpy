using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.NE;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_NT_HEADERS"/> / <see cref="IMAGE_NT_HEADERS64"/> structure.
    /// </summary>
    public readonly struct ImageNtHeaders : IViewable, IValue
    {
        public const uint IMAGE_NT_SIGNATURE = 0x00004550; //PE00

        private const int SignatureOffset = 0;

        /// <summary>
        /// A 4-byte signature identifying the file as a PE image. The bytes are "PE\0\0".
        /// </summary>
        public int Signature => chunk.PeekInt32(SignatureOffset);

        /// <summary>
        /// An <see cref="ImageFileHeader"/> structure that specifies the file header.
        /// </summary>
        public ImageFileHeader FileHeader { get; }

        /// <summary>
        /// An <see cref="ImageOptionalHeader"/> structure that specifies the optional file header.
        /// </summary>
        public ImageOptionalHeader OptionalHeader { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize(bool is32Bit) =>
            sizeof(int) + //Signature
            ImageFileHeader.StructSize +
            OptionalHeader.StructSize(is32Bit);

        private readonly MemoryChunk chunk;

        internal ImageNtHeaders(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            var sig = chunk.PeekInt32(0);

            if (sig != IMAGE_NT_SIGNATURE)
            {
                var ne = sig & 0xFFFF; //NE header is 2 bytes not 4

                if (ne == ImageOS2Header.IMAGE_OS2_SIGNATURE) //NE
                    throw new BadImageFormatException("'New Executable' images should be opened as a NEFile, not a PEFile");

                throw new BadImageFormatException("Invalid PE signature.");
            }

            FileHeader = new ImageFileHeader(chunk.Slice(4));
            OptionalHeader = new ImageOptionalHeader(chunk.Slice(24));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.RelayGlobals(FileHeader);
        }

        //We don't care about representing that there's a 64-bit version of the structure
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_NT_HEADERS, this, ViewKind.ImageNtHeaders, StructSize(((PEViewWriter) writer).Is32Bit));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Signature), SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteInline(FileHeader);
                    break;

                case 2:
                    structWriter.WriteInline(OptionalHeader);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
