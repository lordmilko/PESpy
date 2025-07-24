using System;
using PESpy.Native;
using PESpy.NE;
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
        public const uint IMAGE_NT_SIGNATURE = 0x00004550; //PE00

        /// <summary>
        /// A 4-byte signature identifying the file as a PE image. The bytes are "PE\0\0".
        /// </summary>
#if PEFAST
        public int Signature => chunk.PeekInt32(0);
#else
        public int Signature { get; init; }
#endif

        /// <summary>
        /// An <see cref="ImageFileHeader"/> structure that specifies the file header.
        /// </summary>
#if PEFAST
        public ImageFileHeader FileHeader { get; }
#else
        public ImageFileHeader FileHeader { get; init; }
#endif

        /// <summary>
        /// An <see cref="ImageOptionalHeader"/> structure that specifies the optional file header.
        /// </summary>
#if PEFAST
        public ImageOptionalHeader OptionalHeader { get; }
#else
        public ImageOptionalHeader OptionalHeader { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal int StructSize(bool is32Bit) =>
            sizeof(int) + //Signature
            ImageFileHeader.StructSize +
            OptionalHeader.StructSize(is32Bit);

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageNtHeaders(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            var sig = chunk.PeekInt32(0);

            if (sig != IMAGE_NT_SIGNATURE)
            {
                var ne = sig & 0xFFFF; //NE header is 2 bytes not 4

                if (ne == ImageOS2Header.IMAGE_OS2_SIGNATURE) //NE
                    throw new BadImageFormatException("'New Executable' files are not supported");

                throw new BadImageFormatException("Invalid PE signature.");
            }

            FileHeader = new ImageFileHeader(chunk.Slice(4));
            OptionalHeader = new ImageOptionalHeader(chunk.Slice(24));
        }
#else
        internal ImageNtHeaders(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            //If e_lfanew points to garbage, this will fail
            if (!reader.TryReadInt32(out var signature))
                throw new BadImageFormatException("e_lfanew does not point to a PE Header");

            Signature = signature;

            if (Signature != IMAGE_NT_SIGNATURE)
            {
                var ne = Signature & 0xFFFF;

                if (ne == IMAGE_OS2_SIGNATURE) //NE
                    throw new BadImageFormatException("'New Executable' files are not supported");

                throw new BadImageFormatException("Invalid PE signature.");
            }

            FileHeader = new ImageFileHeader(reader);
            OptionalHeader = new ImageOptionalHeader(reader);
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.RelayGlobals(FileHeader);
        }

        //We don't care about representing that there's a 64-bit version of the structure
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_NT_HEADERS, this, ViewKind.ImageNtHeaders, StructSize(((PEViewWriter) writer).Is32Bit));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Signature), Signature);
            s.WriteInline(FileHeader);
            s.WriteInline(OptionalHeader);

            return s.ToArray();
        }
    }
}
