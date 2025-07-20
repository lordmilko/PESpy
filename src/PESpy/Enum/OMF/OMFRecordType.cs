namespace PESpy.OMF
{
    //https://www.azillionmonkeys.com/qed/Omfg.pdf
    public enum OMFRecordType : byte //Name made up
    {
        /// <summary>
        /// Translator header record
        /// </summary>
        THEADR = 0x80,

        /// <summary>
        /// Library module header record
        /// </summary>
        LHEADR = 0x82,

        /// <summary>
        /// Comment record
        /// </summary>
        COMENT = 0x88,

        /// <summary>
        /// Module end record
        /// </summary>
        MODEND = 0x8a,

        /// <summary>
        /// External names definition record
        /// </summary>
        EXTDEF = 0x8c,

        /// <summary>
        /// Public names definition record
        /// </summary>
        PUBDEF = 0x90,

        /// <summary>
        /// Line numbers record
        /// </summary>
        LINNUM = 0x94,

        /// <summary>
        /// List of names record
        /// </summary>
        LNAMES = 0x96,

        /// <summary>
        /// Segment definition record
        /// </summary>
        SEGDEF = 0x98,

        /// <summary>
        /// Group definition record
        /// </summary>
        GRPDEF = 0x9a,

        /// <summary>
        /// Fixup record
        /// </summary>
        FIXUPP = 0x9c,

        FIXUP2 = 0x9d,

        /// <summary>
        /// Logical enumerated data record
        /// </summary>
        LEDATA = 0xa0,

        /// <summary>
        /// Logical iterated data record
        /// </summary>
        LIDATA = 0xa2,

        /// <summary>
        /// Communal names definition record
        /// </summary>
        COMDEF = 0xb0,

        /// <summary>
        /// Backpatch record
        /// </summary>
        BAKPAT = 0xb2,

        /// <summary>
        /// Local external names definition record
        /// </summary>
        LEXTDEF = 0xb4,

        /// <summary>
        /// Local public names definition record
        /// </summary>
        LPUBDEF = 0xb6,

        /// <summary>
        /// Local communal names definition record
        /// </summary>
        LCOMDEF = 0xb8,

        /// <summary>
        /// COMDAT external names definition record
        /// </summary>
        CEXTDEF = 0xbc,

        /// <summary>
        /// Initialized communal data record
        /// </summary>
        COMDAT = 0xc2,

        /// <summary>
        /// Symbol line numbers record
        /// </summary>
        LINSYM = 0xc4,

        /// <summary>
        /// Named backpatch record
        /// </summary>
        NBKPAT = 0xc8, //Also called NBAKPAT

        /// <summary>
        /// Local logical names definition record
        /// </summary>
        LLNAMES = 0xca,

        //Extra

        /// <summary>
        /// Block definition record
        /// </summary>
        BLKDEF = 0x7a,

        /// <summary>
        /// Type definition record
        /// </summary>
        TYPDEF = 0x8e,

        /// <summary>
        /// Alias definition record
        /// </summary>
        ALIAS = 0xc6,

        /// <summary>
        /// Library header record
        /// </summary>
        LIBHDR = 0xf0,

        /// <summary>
        /// Dictionary header record.<para/>
        /// Found after the last object module in a library, and marks the beginning of the library dictionary
        /// </summary>
        DICHDR = 0xf1,

        /// <summary>
        /// Extended dictionary record.
        /// </summary>
        LIBEXD = 0xf2
    }
}
