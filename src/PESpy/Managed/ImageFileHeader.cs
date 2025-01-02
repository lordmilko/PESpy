using System.Diagnostics;
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
        public IMAGE_FILE_MACHINE Machine { get; init; }

        /// <summary>
        /// The number of sections. This indicates the size of the section table, which immediately follows the headers.
        /// </summary>
        public short NumberOfSections { get; init; }

        /// <summary>
        /// The low 32 bits of the number of seconds since 00:00 January 1, 1970, that indicates when the file was created.
        /// </summary>
        public uint TimeDateStamp { get; init; }

        /// <summary>
        /// The file pointer to the COFF symbol table, or zero if no COFF symbol table is present.
        /// This value should be zero for a PE image.
        /// </summary>
        public int PointerToSymbolTable { get; init; }

        /// <summary>
        /// The number of entries in the symbol table. This data can be used to locate the string table,
        /// which immediately follows the symbol table. This value should be zero for a PE image.
        /// </summary>
        public int NumberOfSymbols { get; init; }

        /// <summary>
        /// The size of the optional header, which is required for executable files but not for object files.
        /// This value should be zero for an object file.
        /// </summary>
        public short SizeOfOptionalHeader { get; init; }

        /// <summary>
        /// The flags that indicate the attributes of the file.
        /// </summary>
        public ImageFile Characteristics { get; init; }

        public RawOffset Offset { get; }

        internal const int StructSize =
            sizeof(short) + // Machine
            sizeof(short) + // NumberOfSections
            sizeof(int) +   // TimeDateStamp:
            sizeof(int) +   // PointerToSymbolTable
            sizeof(int) +   // NumberOfSymbols
            sizeof(short) + // SizeOfOptionalHeader:
            sizeof(ushort); // Characteristics

        internal ImageFileHeader(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Machine = (IMAGE_FILE_MACHINE) reader.ReadUInt16();
            NumberOfSections = reader.ReadInt16();
            TimeDateStamp = reader.ReadUInt32();
            PointerToSymbolTable = reader.ReadInt32();
            NumberOfSymbols = reader.ReadInt32();
            SizeOfOptionalHeader = reader.ReadInt16();
            Characteristics = (ImageFile) reader.ReadUInt16();

            Debug.Assert(PointerToSymbolTable == 0, "Need to add support for legacy symbol table");
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_FILE_HEADER), this, ViewKind.ImageFileHeader);

            s.WriteField(nameof(Machine), Machine, sizeof(short));
            s.WriteField(nameof(NumberOfSections), NumberOfSections);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(PointerToSymbolTable), PointerToSymbolTable);
            s.WriteField(nameof(NumberOfSymbols), NumberOfSymbols);
            s.WriteField(nameof(SizeOfOptionalHeader), SizeOfOptionalHeader);
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(short));
        }
    }
}
