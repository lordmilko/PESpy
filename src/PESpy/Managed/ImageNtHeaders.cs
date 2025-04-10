using System;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_NT_HEADERS"/> / <see cref="IMAGE_NT_HEADERS64"/> structure.
    /// </summary>
    public readonly struct ImageNtHeaders : IViewable, IValue
    {
        public const uint PESignature = 0x00004550;    //PE00

        /// <summary>
        /// A 4-byte signature identifying the file as a PE image. The bytes are "PE\0\0".
        /// </summary>
        public int Signature { get; init; }

        /// <summary>
        /// An <see cref="ImageFileHeader"/> structure that specifies the file header.
        /// </summary>
        public ImageFileHeader FileHeader { get; init; }

        /// <summary>
        /// An <see cref="ImageOptionalHeader"/> structure that specifies the optional file header.
        /// </summary>
        public ImageOptionalHeader OptionalHeader { get; init; }

        public RawOffset Offset { get; }

        internal static int StructSize(bool is32Bit) =>
            sizeof(int) + //Signature
            ImageFileHeader.StructSize +
            ImageOptionalHeader.StructSize(is32Bit);

        internal ImageNtHeaders(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            //If e_lfanew points to garbage, this will fail
            if (!reader.TryReadInt32(out var signature))
                throw new BadImageFormatException("e_lfanew does not point to a PE Header");

            Signature = signature;

            if (Signature != PESignature)
            {
                var ne = Signature & 0xFFFF;

                if (ne == 0x454e) //NE
                    throw new BadImageFormatException("'New Executable' files are not supported");

                throw new BadImageFormatException("Invalid PE signature.");
            }

            FileHeader = new ImageFileHeader(reader);
            OptionalHeader = new ImageOptionalHeader(reader);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            //We don't care about representing that there's a 64-bit version of the structure
            using var s = writer.CreateStruct(nameof(IMAGE_NT_HEADERS), this, ViewKind.ImageNtHeaders);

            s.WriteField(nameof(Signature), Signature);
            s.WriteInline(FileHeader);
            s.WriteInline(OptionalHeader);
        }
    }
}
