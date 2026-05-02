using System;

namespace PESpy.ISO
{
    internal class VolumeDescriptor
    {
        public VolumeDescriptorType Type { get; }

        public string Identifier { get; }

        public byte Version { get; }

        internal VolumeDescriptor(ref ISOByteReader reader)
        {
            Type = (VolumeDescriptorType) reader.ReadByte();
            Identifier = reader.ReadString(5);
            Version = reader.ReadByte();

            //We only support ISO 9660
            if (Identifier != "CD001")
                throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Identifier} ({Type})";
        }
    }
}
