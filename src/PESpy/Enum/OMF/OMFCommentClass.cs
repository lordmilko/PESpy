namespace PESpy.OMF
{
    public enum OMFCommentClass : byte //Name made up
    {
        Translator = 0,
        IntelCopyright = 1,

        //2 - 9B: Intel Reserved

        LibrarySpecifier = 0x81,
        MsDosVersion = 0x9C,

        MemoryModel = 0x9D,

        DosSeg = 0x9E,

        DefaultLibrarySearchName = 0x9F,

        OMFExtensions = 0xA0,
        NewOMFExtension = 0xA1,

        LinkPassSeparator = 0xA2,
        LibMod = 0xA3,
        ExeStr = 0xA4,
        IncErr = 0xA6,
        NoPad = 0xA7,
        WkExt = 0xA8,
        LzExt = 0xA9,
        Comment = 0xDA,
        Compiler = 0xDB,
        Date = 0xDC,
        Timestamp = 0xDD,
        User = 0xDF,
        DependencyFile = 0xE9,
        CommandLine = 0xFF

        //C0 - FF are reserved for user defined comment classes
    }
}
