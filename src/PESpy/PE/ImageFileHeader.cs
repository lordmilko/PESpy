using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a the <see cref="IMAGE_FILE_HEADER"/> structure that describes the COFF header format.
    /// </summary>
    public struct ImageFileHeader : IViewableValue
    {
        private const int MachineOffset = 0;
        internal const int NumberOfSectionsOffset = 2;
        private const int TimeDateStampOffset = 4;
        internal const int PointerToSymbolTableOffset = 8;
        private const int NumberOfSymbolsOffset = 12;
        private const int SizeOfOptionalHeaderOffset = 16;
        private const int CharacteristicsOffset = 18;

        /// <summary>
        /// The type of target machine.
        /// </summary>
        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(MachineOffset);

        /// <summary>
        /// The number of sections. This indicates the size of the section table, which immediately follows the headers.
        /// </summary>
        public ushort NumberOfSections => chunk.PeekUInt16(NumberOfSectionsOffset);

        /// <summary>
        /// The low 32 bits of the number of seconds since 00:00 January 1, 1970, that indicates when the file was created.
        /// </summary>
        public Timestamp TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        /// <summary>
        /// The file pointer to the COFF symbol table, or zero if no COFF symbol table is present.
        /// This value should be zero for a PE image.
        /// </summary>
        private VA<CoffSymbolTable> pointerToSymbolTable;

        public VA<CoffSymbolTable> PointerToSymbolTable
        {
            get
            {
                if (pointerToSymbolTable.ListedAddress == 0)
                {
                    //If this extends beyond the length of the chunk, I feel like maybe we should throw? (which is what this does) Not sure
                    var offset = chunk.PeekInt32(PointerToSymbolTableOffset);

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

        /// <summary>
        /// The number of entries in the symbol table. This data can be used to locate the string table,
        /// which immediately follows the symbol table. This value should be zero for a PE image.
        /// </summary>
        public int NumberOfSymbols => chunk.PeekInt32(NumberOfSymbolsOffset);

        /// <summary>
        /// The size of the optional header, which is required for executable files but not for object files.
        /// This value should be zero for an object file.
        /// </summary>
        public short SizeOfOptionalHeader => chunk.PeekInt16(SizeOfOptionalHeaderOffset);

        /// <summary>
        /// The flags that indicate the attributes of the file.
        /// </summary>
        public IMAGE_FILE Characteristics => (IMAGE_FILE) chunk.PeekUInt16(CharacteristicsOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + // Machine
            sizeof(short) + // NumberOfSections
            sizeof(int) +   // TimeDateStamp:
            sizeof(int) +   // PointerToSymbolTable
            sizeof(int) +   // NumberOfSymbols
            sizeof(short) + // SizeOfOptionalHeader:
            sizeof(ushort); // Characteristics

        private readonly MemoryChunk chunk;

        internal ImageFileHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //ImageCoffSymbolsHeader can declare the Coff Symbol Table as well
            writer.WriteUniqueVAPointerField(PointerToSymbolTable, Offset, PointerToSymbolTableOffset);
        }

        IView? IViewable.WriteStruct(PESpy.View.ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageFileHeader, StructSize);

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Machine), MachineOffset, Machine, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteField(nameof(NumberOfSections), NumberOfSectionsOffset, NumberOfSections);
                    break;

                case 2:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 3:
                    structWriter.WriteField(nameof(PointerToSymbolTable), PointerToSymbolTableOffset, (int) PointerToSymbolTable.ListedAddress);
                    break;

                case 4:
                    structWriter.WriteField(nameof(NumberOfSymbols), NumberOfSymbolsOffset, NumberOfSymbols);
                    break;

                case 5:
                    structWriter.WriteField(nameof(SizeOfOptionalHeader), SizeOfOptionalHeaderOffset, SizeOfOptionalHeader);
                    break;

                case 6:
                    structWriter.WriteField(nameof(Characteristics), CharacteristicsOffset, Characteristics, sizeof(short));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
