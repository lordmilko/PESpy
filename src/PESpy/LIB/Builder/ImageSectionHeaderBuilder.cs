using System.Collections.Generic;
using ClrDebug;

namespace PESpy
{
    internal class ImageSectionHeaderBuilder
    {
        public string Name { get; set; }

        public int VirtualSize { get; set; }

        public int VirtualAddress { get; set; }

        public int SizeOfRawData { get; set; }

        public int PointerToRawData { get; set; }

        public List<ImageRelocationBuilder> PointerToRelocations { get; set; }

        public List<ImageLineNumberBuilder> PointerToLineNumbers { get; set; }

        //NumberOfRelocations is computed
        //NumberOfLineNumbers is computed

        public IMAGE_SCN Characteristics { get; set; }

        public IValue SectionData { get; }

        //You can have a bogus number of relocations and/or line numbers. If that's the case, we should restore the original bogus state
        private int _originalListedNumberOfRelocations;
        private int _originalActualNumberOfRelocations;

        private int _originalListedNumberOfLineNumbers;
        private int _originalActualNumberOfLineNumbers;

        private int _originalListedPointerToRelocations;
        private int _originalListedPointerToLineNumbers;

        public ImageSectionHeaderBuilder(ImageSectionHeader sectionHeader, IValue sectionData)
        {
            Name = sectionHeader.Name.ToString();
            VirtualSize = sectionHeader.VirtualSize; //todo: should we be really exposing this?
            VirtualAddress = sectionHeader.VirtualAddress; //todo: should we be really exposing this?
            SizeOfRawData = sectionHeader.SizeOfRawData; //todo: should we be really exposing this?
            PointerToRawData = sectionHeader.PointerToRawData; //todo: should we be really exposing this?

            _originalListedNumberOfRelocations = sectionHeader.NumberOfRelocations;
            _originalListedNumberOfLineNumbers = sectionHeader.NumberOfLineNumbers;
            _originalListedPointerToRelocations = (int) sectionHeader.PointerToRelocations.ListedAddress;
            _originalListedPointerToLineNumbers = (int) sectionHeader.PointerToLineNumbers.ListedAddress;

            if (sectionHeader.PointerToRelocations.IsValid)
            {
                PointerToRelocations = new List<ImageRelocationBuilder>(sectionHeader.PointerToRelocations.Value.Length);

                foreach (var imageRelocation in sectionHeader.PointerToRelocations.Value)
                    PointerToRelocations.Add(new ImageRelocationBuilder(imageRelocation));

                _originalActualNumberOfRelocations = PointerToRelocations.Count;
            }
            else
                PointerToRelocations = new List<ImageRelocationBuilder>();

            if (sectionHeader.PointerToLineNumbers.IsValid)
            {
                PointerToLineNumbers = new List<ImageLineNumberBuilder>(sectionHeader.PointerToLineNumbers.Value.Length);

                foreach (var imageLineNumber in sectionHeader.PointerToLineNumbers.Value)
                    PointerToLineNumbers.Add(new ImageLineNumberBuilder(imageLineNumber));

                _originalActualNumberOfRelocations = PointerToLineNumbers.Count;
            }
            else
                PointerToLineNumbers = new List<ImageLineNumberBuilder>();

            Characteristics = sectionHeader.Characteristics;

            //todo: make editable
            SectionData = sectionData;
        }

        public void WriteTo(FileWriter writer)
        {
            writer.WriteNullPaddedUtf8(Name, 8);
            writer.WriteUInt32((uint) VirtualSize);
            writer.WriteUInt32((uint) VirtualAddress);
            writer.WriteUInt32((uint) SizeOfRawData);
            writer.WriteUInt32((uint) PointerToRawData);

            if (PointerToRelocations.Count == 0)
                writer.WriteUInt32((uint) _originalListedPointerToRelocations); //This may be a bogus value
            else
                writer.Skip(sizeof(int));

            if (PointerToLineNumbers.Count == 0)
                writer.WriteUInt32((uint) _originalListedPointerToLineNumbers); //This may be a bogus value
            else
                writer.Skip(sizeof(int));

            if (_originalActualNumberOfRelocations == PointerToRelocations.Count)
                writer.WriteUInt16((ushort) _originalListedNumberOfRelocations); //This may be a bogus value
            else
                writer.WriteUInt16((ushort) PointerToRelocations.Count);

            if (_originalActualNumberOfLineNumbers == PointerToLineNumbers.Count)
                writer.WriteUInt16((ushort) _originalListedNumberOfLineNumbers); //This may be a bogus value
            else
                writer.WriteUInt16((ushort) PointerToLineNumbers.Count);

            writer.WriteUInt32((uint) Characteristics);
        }
    }
}
