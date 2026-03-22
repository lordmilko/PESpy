using System;
using System.Collections.Generic;
using static PESpy.ImageSymbol;

namespace PESpy
{
    internal class ImageSymbolBuilder
    {
        public string Name { get; set; }

        public uint Value { get; set; }

        public ushort SectionNumber {  get; set; }

        public IMAGE_SYM_TYPE Type {  get; set; }

        public IMAGE_SYM_TYPE BasicType
        {
            get => (IMAGE_SYM_TYPE) ((ushort) Type & N_BTMASK);
            set => Type = (IMAGE_SYM_TYPE) (((ushort) Type & ~N_BTMASK) | ((ushort) value & N_BTMASK));
        }

        public IMAGE_SYM_DTYPE DerivedType
        {
            get => (IMAGE_SYM_DTYPE) (((ushort) Type & N_TMASK) >> N_BTSHIFT);
            set => Type = (IMAGE_SYM_TYPE) (((ushort) Type & ~N_TMASK) | (((ushort) value << N_BTSHIFT) & N_TMASK));
        }

        public IMAGE_SYM_CLASS StorageClass {  get; set; }

        //NumberOfAuxSymbols will be computed

        public List<ImageAuxSymbolBuilder> AuxSymbols { get; }

        internal uint LongNameOffset = uint.MaxValue;

        public ImageSymbolBuilder(ImageSymbol imageSymbol)
        {
            Name = imageSymbol.Name.ToString();
            Value = imageSymbol.Value;
            SectionNumber = imageSymbol.SectionNumber;
            Type = imageSymbol.Type;
            BasicType = imageSymbol.BasicType;
            DerivedType = imageSymbol.DerivedType;
            StorageClass = imageSymbol.StorageClass;

            if (imageSymbol.AuxSymbols.Length > 0)
            {
                AuxSymbols = new List<ImageAuxSymbolBuilder>(imageSymbol.AuxSymbols.Length);

                foreach (var auxSymbol in imageSymbol.AuxSymbols)
                    AuxSymbols.Add(new ImageAuxSymbolBuilder(auxSymbol));
            }
            else
                AuxSymbols = new List<ImageAuxSymbolBuilder>();

            if (imageSymbol.Name.Short == 0)
                LongNameOffset = (uint) imageSymbol.Name.Long;
        }

        public void WriteTo(FileWriter writer, Dictionary<string, int> nameOffsetMap)
        {
            if (Name.Length > 8)
            {
                writer.WriteUInt32(0); //Short
                writer.WriteUInt32((uint) nameOffsetMap[Name]); //Long
            }
            else
                writer.WriteNullPaddedUtf8(Name, 8);

            writer.WriteUInt32(Value);
            writer.WriteUInt16(SectionNumber);
            writer.WriteUInt16((ushort) Type);
            writer.WriteByte((byte) StorageClass);
            writer.WriteByte((byte) AuxSymbols.Count);

            if (AuxSymbols.Count > 0)
            {
                foreach (var auxSymbol in AuxSymbols)
                    auxSymbol.WriteTo(writer);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
