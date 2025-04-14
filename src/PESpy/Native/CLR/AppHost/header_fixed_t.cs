namespace PESpy.Native
{
    //AppHost Bundle Manifest
    //https://github.com/dotnet/runtime/blob/main/src/native/corehost/bundle/header.h

    internal struct header_fixed_t
    {
        public int major_version;
        public int minor_version;
        public int num_embedded_files;

        //Bundle ID: 7 bit string length + UTF 8 encoded string
    }
}
