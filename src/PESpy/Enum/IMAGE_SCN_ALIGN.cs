namespace PESpy
{
    public enum IMAGE_SCN_ALIGN
    {
        //These fields are also defined in the IMAGE_SCN enum,
        //however IMAGE_TLS_DIRECTORY.Characteristics specifically
        //only targets the IMAGE_SCN_ALIGN_* values

        ALIGN_1BYTES = 0x00100000,
        ALIGN_2BYTES = 0x00200000,
        ALIGN_4BYTES = 0x00300000,
        ALIGN_8BYTES = 0x00400000,
        ALIGN_16BYTES = 0x00500000,
        ALIGN_32BYTES = 0x00600000,
        ALIGN_64BYTES = 0x00700000,
        ALIGN_128BYTES = 0x00800000,
        ALIGN_256BYTES = 0x00900000,
        ALIGN_512BYTES = 0x00a00000,
        ALIGN_1024BYTES = 0x00b00000,
        ALIGN_2048BYTES = 0x00c00000,
        ALIGN_4096BYTES = 0x00d00000,
        ALIGN_8192BYTES = 0x00e00000,
    }
}
