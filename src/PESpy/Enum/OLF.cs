namespace PESpy
{
    //From NT 4: Old C6 Leaf Indices. There is a slightly different list in defines.h than typeinfo.h.
    //Some of the items in defines.h overlap with each other, so we're using the list from typeinfo.h instead,
    //which is waht cvdump uses

    public enum OLF : byte
    {
        /* Leaves used to build types */
        OLF_BITFIELD      = 92,
        OLF_NEWTYPE       = 93,
        OLF_HUGE          = 94,

        OLF_OFFSETSIZE32  = 90,     /* OLF_POINTER */
        OLF_NOPOP32       = 91,     /* OLF_LONG_NOPOP */
        OLF_NOPOP16       = 92,     /* OLF_SHORT_NOPOP */
        OLF_POP32         = 93,     /* OLF_LONG_POP */
        OLF_POP16         = 94,     /* OLF_SHORT_POP */
        OLF_FAR32         = 95,     /* procs (?) */
        OLF_NEAR32        = 96,     /* labels (OLF_NEAR), procs (?) */

        OLF_PLSTRUCTURE   = 97,
        OLF_PLARRAY       = 98,
        OLF_SHORT_NOPOP   = 99,
        OLF_LONG_NOPOP    = 100,
        OLF_SELECTOR      = 101,
        OLF_INTERRUPT     = 102,
        OLF_FILE          = 103,
        OLF_PACKED        = 104,
        OLF_UNPACKED      = 105,
        OLF_SET           = 106,
        OLF_CHAMELEON     = 107,
        OLF_BOOLEAN       = 108,
        OLF_TRUE          = 109,
        OLF_FALSE         = 110,
        OLF_CHAR          = 111,
        OLF_INTEGER       = 112,
        OLF_CONST         = 113,
        OLF_LABEL         = 114,
        OLF_FAR           = 115,
        OLF_LONG_POP      = 115,
        OLF_NEAR          = 116,
        OLF_SHORT_POP     = 116,
        OLF_PROCEDURE     = 117,
        OLF_PARAMETER     = 118,
        OLF_DIMENSION     = 119,
        OLF_ARRAY         = 120,
        OLF_STRUCTURE     = 121,
        OLF_POINTER       = 122,
        OLF_SCALAR        = 123,
        OLF_UNSINT        = 124,
        OLF_SGNINT        = 125,
        OLF_REAL          = 126,
        OLF_LIST          = 127,

        /* Nice and easy null leaves defined */
        OLF_EASY          = 128,
        OLF_NICE          = 129,

        /* Prefixes to other leaves */
        OLF_STRING        = 130,
        OLF_INDEX         = 131,
        OLF_REPEAT        = 132,

        /* Prefixes for constants */
        OLF_2_UNSIGNED    = 133,
        OLF_4_UNSIGNED    = 134,
        OLF_8_UNSIGNED    = 135,
        OLF_1_SIGNED      = 136,
        OLF_2_SIGNED      = 137,
        OLF_4_SIGNED      = 138,
        OLF_8_SIGNED      = 139,

        /* Fortran string & array bounds */
        OLF_BARRAY        = 140,
        OLF_FSTRING       = 141,
        OLF_FARRIDX       = 142,

        /* incremental compilation support */
        OLF_SKIP          = 144,

        /* base pointer support */
        OLF_BASEPTR       = 145,
        OLF_BASESEG       = 146,
        OLF_BASEVAL       = 147,
        OLF_BASESEGVAL    = 148,
        OLF_BASEADR       = 151,
        OLF_BASESEGADR    = 152,

        /* fastcall return type leaves */
        OLF_NFASTCALL     = 149,
        OLF_FFASTCALL     = 150,

        /* c++ support */
        OLF_C7PTR         = 143,
        OLF_MODIFIER      = 153,
        OLF_BASECLASS     = 154,
        OLF_VBASECLASS    = 155,
        OLF_FRIENDCLASS   = 156,
        OLF_MEMBER        = 157,
        OLF_STATICMEMBER  = 158,
        OLF_VTABPTR       = 159,
        OLF_METHOD        = 160,
        OLF_CLASS         = 161,
        OLF_C7STRUCTURE   = 162,
        OLF_UNION         = 163,
        OLF_MEMBERFUNC    = 164,

        OLF_DEFARG        = 165,

        /* reserved for COBOL */

        OLF_COBOLTYPREF   = 166,
        OLF_COBOL         = 167,

        /* used for enumerate types */
        OLF_ENUM          = 168,
        OLF_ENUMERATE     = 169,

        OLF_VTSHAPE       = 170,
        OLF_NESTDEF       = 171,
        OLF_DERIVLIST     = 172,
        OLF_FIELDLIST     = 173,
        OLF_ARGLIST       = 174,
        OLF_METHODLIST    = 175,
        OLF_VBCLASS       = 176,
        OLF_IVBCLASS      = 177,
    }
}
