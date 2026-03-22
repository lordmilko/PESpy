using ClrDebug;

namespace PESpy
{
    internal class ImageFileHeaderBuilder
    {
        public IMAGE_FILE_MACHINE Machine { get; set; }

        internal ushort NumberOfSections { get; set; }

        public Timestamp TimeDateStamp { get; set; }

        public CoffSymbolTableBuilder PointerToSymbolTable { get; set; }

        public short SizeOfOptionalHeader { get; set; }

        public IMAGE_FILE Characteristics { get; set; }

        public ImageFileHeaderBuilder(ImageFileHeader fileHeader)
        {
            Machine = fileHeader.Machine;
            NumberOfSections = fileHeader.NumberOfSections;
            TimeDateStamp = fileHeader.TimeDateStamp;

            if (fileHeader.PointerToSymbolTable.IsValid)
                PointerToSymbolTable = new CoffSymbolTableBuilder(fileHeader.PointerToSymbolTable.Value);

            SizeOfOptionalHeader = fileHeader.SizeOfOptionalHeader;
            Characteristics = fileHeader.Characteristics;
        }

        public void WriteTo(FileWriter writer, int numberOfSymbols)
        {
            writer.WriteUInt16((ushort) Machine);
            writer.WriteUInt16(NumberOfSections);
            writer.WriteUInt32((uint) TimeDateStamp);
            writer.Skip(sizeof(int)); //Make room for PointerToSymbolTable
            writer.WriteUInt32((uint) numberOfSymbols);
            writer.WriteUInt16((ushort) SizeOfOptionalHeader);
            writer.WriteUInt16((ushort) Characteristics);
        }
    }
}
