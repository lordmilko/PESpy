using System;
using System.Collections.Generic;
using System.IO;

namespace PESpy.ISO
{
    public class ISOReader : IDisposable
    {
        public DirectoryRecord Root { get; }

        private MemoryMappedFileHolder _mmf;

        public unsafe ISOReader(string path)
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            //todo: use suppressdispose pattern
            var mmf = new MemoryMappedFileHolder(stream);
            _mmf = mmf;

            var pISO = mmf.Address;

            //An ISO begins with a "system" area (32,768 bytes), which aren't used
            //in ISO 9660, so skip over those
            var pVolumeDescriptors = pISO + 0x8000;

            //Following the system area is the data area. The data area begins with a collection of "volume descriptors"

            const int sectorSize = 2048;

            var volumeDescriptors = new List<VolumeDescriptor>();

            PrimaryVolumeDescriptor primaryVolumeDescriptor = null;

            var @continue = true;

            while (@continue)
            {
                var buffer = new NativeSpan<byte>(pVolumeDescriptors, sectorSize);
                pVolumeDescriptors += sectorSize;

                var type = (VolumeDescriptorType) buffer[0];

                var reader = new ISOByteReader(buffer, sectorSize);

                //ECMA-119 section 9 (pdf page 30)
                switch (type)
                {
                    //9.2
                    case VolumeDescriptorType.Boot:
                        //Boot has additional fields which should be read into a
                        //BootVolumeDescriptor, but we don't currently care about those
                        volumeDescriptors.Add(new VolumeDescriptor(ref reader));
                        break;

                    //9.
                    case VolumeDescriptorType.SetTerminator:
                        //No special fields
                        volumeDescriptors.Add(new VolumeDescriptor(ref reader));
                        @continue = false;
                        break;

                    //9.4
                    case VolumeDescriptorType.Primary:
                        primaryVolumeDescriptor = new PrimaryVolumeDescriptor(ref reader, pISO);
                        volumeDescriptors.Add(primaryVolumeDescriptor);
                        break;

                    case VolumeDescriptorType.Supplementary:
                        /* When we have supplementary that means we're Joliet. We need to use this descriptor
                         * instead of the regular primary descriptor, because the default ISO 9660 implementation
                         * is quite strict with regards to what characters are allowed in name. Something as simple
                         * as a $ will be encoded as _. This is no good; replace the "default" primary with the
                         * supplementary one, which is exactly the same except specifies an encoding to use */
                        primaryVolumeDescriptor = new PrimaryVolumeDescriptor(ref reader, pISO);
                        volumeDescriptors.Add(primaryVolumeDescriptor);
                        break;

                    default:
                        //No special fields
                        volumeDescriptors.Add(new VolumeDescriptor(ref reader));
                        break;
                }
            }

            if (primaryVolumeDescriptor == null)
                throw new NotImplementedException();

            //There are several variations on the standard ISO 9660 format, including
            //Rock Ridge and Joliet. We can be Joliet if we have a Supplementary descriptor.
            //We currently just support basic ISO files
            Root = primaryVolumeDescriptor.RootDirectory;
        }

        public void Dispose()
        {
            _mmf.Dispose();
        }
    }
}
