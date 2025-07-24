using ClrDebug;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a the <see cref="IMAGE_FILE_HEADER"/> structure that describes the COFF header format.
    /// </summary>
    public class ImageFileHeader : IValue, IViewable //Structs return copies from properties, and ref properties don't display properly in the debugger
    {
        /// <summary>
        /// The type of target machine.
        /// </summary>
#if PEFAST
        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(0);
#else
        public IMAGE_FILE_MACHINE Machine { get; init; }
#endif

        /// <summary>
        /// The number of sections. This indicates the size of the section table, which immediately follows the headers.
        /// </summary>
#if PEFAST
        public short NumberOfSections => chunk.PeekInt16(2);
#else
        public short NumberOfSections { get; init; }
#endif

        /// <summary>
        /// The low 32 bits of the number of seconds since 00:00 January 1, 1970, that indicates when the file was created.
        /// </summary>
#if PEFAST
        public uint TimeDateStamp => chunk.PeekUInt32(4);
#else
        public uint TimeDateStamp { get; init; }
#endif

        /// <summary>
        /// The file pointer to the COFF symbol table, or zero if no COFF symbol table is present.
        /// This value should be zero for a PE image.
        /// </summary>
#if PEFAST
        private VA<CoffSymbolTable> pointerToSymbolTable;

        public VA<CoffSymbolTable> PointerToSymbolTable
        {
            get
            {
                if (pointerToSymbolTable.ListedAddress == 0)
                {
                    var offset = chunk.PeekInt32(8);

                    if (offset != 0)
                    {
                        if (ImageSectionHeader.TryGetHeaderChunk(chunk, offset, out var headerChunk))
                        {
                            pointerToSymbolTable = new VA<CoffSymbolTable>(offset, headerChunk.AbsoluteOffset, new CoffSymbolTable(headerChunk, NumberOfSymbols));
                        }
                    }
                    else
                        pointerToSymbolTable = new VA<CoffSymbolTable>(offset);
                }

                return pointerToSymbolTable;
            }
        }
#else
        public VA<CoffSymbolTable> PointerToSymbolTable { get; init; }
#endif

        /// <summary>
        /// The number of entries in the symbol table. This data can be used to locate the string table,
        /// which immediately follows the symbol table. This value should be zero for a PE image.
        /// </summary>
#if PEFAST
        public int NumberOfSymbols => chunk.PeekInt32(12);
#else
        public int NumberOfSymbols { get; init; }
#endif

        /// <summary>
        /// The size of the optional header, which is required for executable files but not for object files.
        /// This value should be zero for an object file.
        /// </summary>
#if PEFAST
        public short SizeOfOptionalHeader => chunk.PeekInt16(16);
#else
        public short SizeOfOptionalHeader { get; init; }
#endif

        /// <summary>
        /// The flags that indicate the attributes of the file.
        /// </summary>
#if PEFAST
        public ImageFile Characteristics => (ImageFile) chunk.PeekUInt16(18);
#else
        public ImageFile Characteristics { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(short) + // Machine
            sizeof(short) + // NumberOfSections
            sizeof(int) +   // TimeDateStamp:
            sizeof(int) +   // PointerToSymbolTable
            sizeof(int) +   // NumberOfSymbols
            sizeof(short) + // SizeOfOptionalHeader:
            sizeof(ushort); // Characteristics

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageFileHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageFileHeader(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Machine = (IMAGE_FILE_MACHINE) reader.ReadUInt16();
            NumberOfSections = reader.ReadInt16();
            TimeDateStamp = reader.ReadUInt32();
            var pointerToSymbolTable = reader.ReadInt32();
            NumberOfSymbols = reader.ReadInt32();
            SizeOfOptionalHeader = reader.ReadInt16();
            Characteristics = (ImageFile) reader.ReadUInt16();

            if (pointerToSymbolTable != 0)
            {
                //The symbols are usually in the overlay. Not sure whether attempting to seek
                //to the symbols will cause an issue when the image is loaded into memory (at which point
                //the overlay won't exist)

                var oldOffset = reader.Position;

                reader.Seek(pointerToSymbolTable);

                PointerToSymbolTable = new VA<CoffSymbolTable>(
                    pointerToSymbolTable,
                    pointerToSymbolTable,
                    new CoffSymbolTable(reader, NumberOfSymbols)
                );

                reader.Seek(oldOffset);
            }
            else
                PointerToSymbolTable = default;
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            if (PointerToSymbolTable.IsValid)
                writer.WriteUniqueGlobal(PointerToSymbolTable.Value); //ImageCoffSymbolsHeader can declare the Coff Symbol Table as well
        }

        IView? IViewable.WriteStruct(PESpy.View.ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_FILE_HEADER, this, ViewKind.ImageFileHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Machine), Machine, sizeof(short));
            s.WriteField(nameof(NumberOfSections), NumberOfSections);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);

            s.WriteField(nameof(PointerToSymbolTable), (int) PointerToSymbolTable.ListedAddress);

            s.WriteField(nameof(NumberOfSymbols), NumberOfSymbols);
            s.WriteField(nameof(SizeOfOptionalHeader), SizeOfOptionalHeader);
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(short));

            return s.ToArray();
        }
    }
}
