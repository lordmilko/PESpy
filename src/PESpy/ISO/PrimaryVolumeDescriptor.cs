using System;
using System.Text;

namespace PESpy.ISO
{
    //We use this for both primary and supplementary
    internal class PrimaryVolumeDescriptor : VolumeDescriptor
    {
        public string SystemIdentifier { get; }

        public string VolumeIdentifier { get; }

        public uint VolumeSpaceSize { get; }

        public NativeSpan<byte> EscapeSequences { get; } //Ignored in Primary; defines the encoding to use in supplementary

        public ushort VolumeSetSize { get; }

        public ushort VolumeSequenceNumber { get; }

        public ushort LogicalBlockSize { get; }

        public uint PathTableSize { get; }

        public uint TypeLPathTableLocation { get; }

        public uint OptionalTypeLPathTableLocation { get; }

        public uint TypeMPathTableLocation { get; }

        public uint OptionalTypeMPathTableLocation { get; }

        public DirectoryRecord RootDirectory { get; }

        public string VolumeSetIdentifier { get; }

        public string PublisherIdentifier { get; }

        public string DataPreparerIdentifier { get; }

        public string ApplicationIdentifier { get; }

        public string CopyrightFileIdentifier { get; }

        public string AbstractFileIdentifier { get; }

        public string BibliographicFileIdentifier { get; }

        public DateTime? VolumeCreationDateTime { get; }

        public DateTime? VolumeModificationDateTime { get; }

        public DateTime? VolumeExpirationDateTime { get; }

        public DateTime? VolumeEffectiveDateTime { get; }

        public byte FileStructureVersion { get; }

        public unsafe PrimaryVolumeDescriptor(ref ISOByteReader reader, byte* pISO) : base(ref reader)
        {
            //ECMA 119 page 34

            /* Ways of reading numbers
             * 
             * 16-bit
             * - 8.2.2: LSB
             * - 8.2.3: MSB
             * - 8.2.4: Both
             * 
             * 32-bit
             * - 8.3.2: LSB
             * - 8.3.3: MSB
             * - 8.3.4: Both
             */
            EscapeSequences = new NativeSpan<byte>(reader._buffer + 88 - 7, 32);

            Encoding encoding;

            if (Type == VolumeDescriptorType.Primary)
                encoding = Encoding.ASCII;
            else
            {
                //Joliet is 0x25, 0x2F followed by 0x40 (Level 1), 0x43 (Level 2) or 0x45 (Level 3)

                encoding = Encoding.BigEndianUnicode;
            }

            reader.SkipByte(); //Unused field
            SystemIdentifier                = reader.ReadChars(start: 9, end: 40, encoding); //9.4.6
            VolumeIdentifier                = reader.ReadChars(start: 41, end: 72, encoding); //9.4.7
            reader.SkipByte(start: 73, end: 80); //Unused field
            VolumeSpaceSize                 = reader.ReadBothUInt32(start: 81, end: 88); //9.4.9 (8.3.4)
            reader.SkipByte(start: 89, end: 120); //Ignored in primary; only used in supplementary (and eagerly read above)
            VolumeSetSize                   = reader.ReadBothUInt16(start: 121, end: 124); //9.4.11 (8.2.4)
            VolumeSequenceNumber            = reader.ReadBothUInt16(start: 125, end: 128); //9.4.12 (8.2.4)
            LogicalBlockSize                = reader.ReadBothUInt16(start: 129, end: 132); //9.4.13 (8.2.4)
            PathTableSize                   = reader.ReadBothUInt32(start: 133, end: 140); //9.4.14 (8.3.4)
            TypeLPathTableLocation          = reader.ReadLittleEndianUInt32(start: 141, end: 144); //9.4.15 (8.3.2)
            OptionalTypeLPathTableLocation  = reader.ReadLittleEndianUInt32(start: 145, end: 148); //9.4.16 (8.3.2)
            TypeMPathTableLocation          = reader.ReadBigEndianUInt32(start: 149, end: 152); //9.4.17 (8.3.3)
            OptionalTypeMPathTableLocation  = reader.ReadBigEndianUInt32(start: 153, end: 156); //9.4.18 (8.3.3)

            var context = new ISOContext
            {
                pISO = pISO,
                LogicalBlockSize = LogicalBlockSize,
                Encoding = encoding
            };

            RootDirectory                   = new DirectoryRecord(ref reader, context, 0/*, start: 157, end:  190 */); //9.4.19

            VolumeSetIdentifier             = reader.ReadChars(start: 191, end: 318, encoding); //9.4.20
            PublisherIdentifier             = reader.ReadChars(start: 319, end: 446, encoding); //9.4.21
            DataPreparerIdentifier          = reader.ReadChars(start: 447, end: 574, encoding); //9.4.22
            ApplicationIdentifier           = reader.ReadChars(start: 575, end: 702, encoding); //9.4.23
            CopyrightFileIdentifier         = reader.ReadChars(start: 703, end: 739, encoding); //9.4.24
            AbstractFileIdentifier          = reader.ReadChars(start: 740, end: 776, encoding); //9.4.25
            BibliographicFileIdentifier     = reader.ReadChars(start: 777, end: 813, encoding); //9.4.26
            VolumeCreationDateTime          = reader.ReadDateTime(start: 814, end: 830); //9.4.27
            VolumeModificationDateTime      = reader.ReadDateTime(start: 831, end: 847); //9.4.28
            VolumeExpirationDateTime        = reader.ReadDateTime(start: 848, end: 864); //9.4.29
            VolumeEffectiveDateTime         = reader.ReadDateTime(start: 865, end: 881); //9.4.30
            FileStructureVersion            = reader.ReadByte(); //9.4.31
        }
    }
}
