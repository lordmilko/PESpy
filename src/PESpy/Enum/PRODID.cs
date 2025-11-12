namespace PESpy
{
    //In msobj140-msvcrt.lib under VS2019 there is an obj file convert.obj
    //Within this there is a lfEnum symbol PRODID containing a lfFieldList with these values
    //The master list of PRODID info is in prodids.h
    public enum PRODID : ushort
    {
        //Apparently rich header info was not present prior to VS97 SP3 (Visual C++ 5 under the hood)

        /* Version Map
         * -----------
         * 
         * Each item in the PRODID enumeration describes the version of a particular _tool_. The version of that tool
         * can then be used to deduce the toolset that that tool was found in.
         * 
         * UTC is the Universal Tuple Compiler, and is the backend component found in c2.dll
         * https://devblogs.microsoft.com/cppblog/optimizing-c-code-overview/
         * 
         * CVTPGD items seem to basically be the same version as UTC, just without the UTC prefix. Potentially the version seen
         * in CVTPGD is _MSC_VER? CVTPGD was first introduced in VS2002
         * 
         * Product | Version | Utc      | MajorMinor | CVTPGD
         * --------|---------|----------|------------|-----------------|
         * 97 SP3  | 5.00    | Utc11    | 500        |
         * 97 SP3  | 5.01    | Utc11    | 501        |
         * 97 SP3  | 5.10    | Utc11    | 510        |
         * 97 SP3  | 5.11    | Utc11    | 511        |
         * 97 SP3  | 5.12    | Utc11    | 512        |
         * 98      | 6.0     | Utc12    | 600 / 60   |
         * 98      | 6.01    | Utc12    | 601        |
         * 98      | 6.10    | Utc12_1  | 610        |
         * 98      | 6.13    | Utc12_1  | 613        |
         * 98      | 6.14    | Utc12_1  | 614        |
         * 98      | 6.15    | Utc12_1  | 615        |
         * 98      | 6.20    | Utc12_2  | 620        |
         * 98      | 6.21    | Utc12_2  | 621        |
         * 98      | 6.22    | Utc12_2  | 622        |
         * 98      | 6.24    | Utc12_2  | 624        |
         * 2002    | 7.00    | Utc13    | 700 / 70   | 1300
         * 2003    | 7.10    | Utc1310  | 710        | 1310
         * 2003    | 7.10p   | Utc1310p | 710p       | 1310p
         * 2005    | 8.0     | Utc1400  | 800        | 1400
         * 2008    | 9.0     | Utc1500  | 900        | 1500
         * 2010    | 10.00   | Utc1600  | 1000       | 1600
         * 2010    | 10.10   | Utc1610  | 1010       | 1610
         * 2012    | 11.00   | Utc1700  | 1100       | 1700
         * 2013    | 12.00   | Utc1800  | 1200       | 1800
         * 2013    | 12.10   | Utc1810  | 1210       | 1810
         * 2015+   | 14.x    | Utc1900  | 1400       | 1900
         */

        //LINK   - linker
        //CVTOMF - Linking using OMF to COFF file converter tool in VS98
        //CVTRES - resource converter tool in VS98
        //CVTPGD - it's a tool, but I don't know what it does yet

        /* The information listed below lists a "version" that does not strictly always make sense
         * For tools in the early days that hardcode their version, we might have the version listed there,
         * however for tools included as part of Visual Studio, we have the Visual Studio version listed.
         * But that's not the same thing as the version of the tool! ProdItem.ToolInfo.cs contains a mapping
         * of PRODID values to the actual versions of each tool */

                                           // VS      | Product        | Version | Microsoft Description
                                           //---------|----------------|---------|-----------------------|
        prodidUnknown = 0,                 //         |                |         |
        prodidImport0               = 1,   //         | Import         |         | Linker generated import object version 0
        prodidLinker510             = 2,   // 97 SP3  | LINK           | 5.10    | LINK 5.10 (Visual Studio 97 SP3)
        prodidCvtomf510             = 3,   // 97 SP3  | LINK (CVTOMF)  | 5.10    | LINK 5.10 (Visual Studio 97 SP3) OMF to COFF conversion
        prodidLinker600             = 4,   // 98      | LINK           | 6.00    | LINK 6.00 (Visual Studio 98)
        prodidCvtomf600             = 5,   // 98      | CVTOMF         | 6.00    | LINK 6.00 (Visual Studio 98) OMF to COFF conversion
        prodidCvtres500             = 6,   // 97 SP3  | CVTRES         | 5.00    | CVTRES 5.00
        prodidUtc11_Basic           = 7,   // 97 SP3  | VB             | 5.0     | VB 5.0 native code
        prodidUtc11_C               = 8,   // 97 SP3  | C/C++          | 5.0     | VC++ 5.0 C/C++
        prodidUtc12_Basic           = 9,   // 98      | VB             | 6.0     | VB 6.0 native code
        prodidUtc12_C               = 10,  // 98      | C              | 6.0     | VC++ 6.0 C
        prodidUtc12_CPP             = 11,  // 98      | C++            | 6.0     | VC++ 6.0 C++
        prodidAliasObj60            = 12,  // 98      | ALIASOBJ       | 6.0     | ALIASOBJ.EXE (CRT Tool that builds OLDNAMES.LIB)
        prodidVisualBasic60         = 13,  // 98      | VB             | 6.0     | VB 6.0 generated object
        prodidMasm613               = 14,  // 98      | MASM           | 6.13    | MASM 6.13
        prodidMasm710               = 15,  // 2003    | MASM           | 7.10    | MASM 7.01 | I think Microsoft has a typo, because modern lib files call it 7.10
        prodidLinker511             = 16,  // 97 SP3  | LINK           | 5.11    | LINK 5.11
        prodidCvtomf511             = 17,  // 97 SP3  | LINK (CVTOMF)  | 5.11    | LINK 5.11 OMF to COFF conversion
        prodidMasm614               = 18,  // 98      | MASM           | 6.14    | MASM 6.14 (MMX2 support)
        prodidLinker512             = 19,  // 97 SP3  | LINK           | 5.12    | LINK 5.12
        prodidCvtomf512             = 20,  // 97 SP3  | LINK (CVTOMF)  | 5.12    | 5.12 OMF to COFF conversion
        
        prodidUtc12_C_Std           = 21,  // 98      | C (Std)        | 6.0     |
        prodidUtc12_CPP_Std         = 22,  // 98      | C++ (Std)      | 6.0     |
        prodidUtc12_C_Book          = 23,  // 98      | C (Book)       | 6.0     |
        prodidUtc12_CPP_Book        = 24,  // 98      | C++ (Book)     | 6.0     |
        prodidImplib700             = 25,  // 2002    | Import         | 7.00    |
        prodidCvtomf700             = 26,  // 2002    | CVTOMF         | 7.00    |
        prodidUtc13_Basic           = 27,  // 2002    | VB             | 7.00    |
        prodidUtc13_C               = 28,  // 2002    | C              | 7.00    |
        prodidUtc13_CPP             = 29,  // 2002    | C++            | 7.00    |
        prodidLinker610             = 30,  // 98      | LINK           | 6.10    |
        prodidCvtomf610             = 31,  // 98      | CVTOMF         | 6.10    |
        prodidLinker601             = 32,  // 98      | LINK           | 6.01    |
        prodidCvtomf601             = 33,  // 98      | CVTOMF         | 6.01    |
        prodidUtc12_1_Basic         = 34,  // 98      | VB             | 6.10    |
        prodidUtc12_1_C             = 35,  // 98      | C              | 6.10    |
        prodidUtc12_1_CPP           = 36,  // 98      | C++            | 6.10    |
        prodidLinker620             = 37,  // 98      | LINK           | 6.20    |
        prodidCvtomf620             = 38,  // 98      | CVTOMF         | 6.20    |
        prodidAliasObj70            = 39,  // 2002    | ALIASOBJ       | 7.0     |
        prodidLinker621             = 40,  // 98      | LINK           | 6.21    |
        prodidCvtomf621             = 41,  // 98      | CVTOMF         | 6.21    |
        prodidMasm615               = 42,  // 98      | MASM           | 6.15    |
        prodidUtc13_LTCG_C          = 43,  // 2002    | C (LTCG)       | 7.00    |
        prodidUtc13_LTCG_CPP        = 44,  // 2002    | C++ (LTCG)     | 7.00    |
        prodidMasm620               = 45,  // 98      | MASM           | 6.20    |
        prodidILAsm100              = 46,  //         | ILASM          |         | ?
        prodidUtc12_2_Basic         = 47,  // 98      | VB             | 6.20    |
        prodidUtc12_2_C             = 48,  // 98      | C              | 6.20    |
        prodidUtc12_2_CPP           = 49,  // 98      | C++            | 6.20    |
        prodidUtc12_2_C_Std         = 50,  // 98      | C (Std)        | 6.20    |
        prodidUtc12_2_CPP_Std       = 51,  // 98      | C++ (Book)     | 6.20    |
        prodidUtc12_2_C_Book        = 52,  // 98      | C (Std)        | 6.20    |
        prodidUtc12_2_CPP_Book      = 53,  // 98      | C++ (Book)     | 6.20    |
        prodidImplib622             = 54,  // 98      | Import         | 6.22    |
        prodidCvtomf622             = 55,  // 98      | CVTOMF         | 6.22    |
        prodidCvtres501             = 56,  // 97 SP3  | CVTRES         | 5.01    |
        prodidUtc13_C_Std           = 57,  // 2002    | C (Std)        | 7.00    |
        prodidUtc13_CPP_Std         = 58,  // 2002    | C++ (Std)      | 7.00    |
        prodidCvtpgd1300            = 59,  // 2002    | CVTPGD         | 7.00    |
        prodidLinker622             = 60,  // 98      | LINK           | 6.22    |
        prodidLinker700             = 61,  // 2002    | LINK           | 7.00    |
        prodidExport622             = 62,  // 98      | Export         | 6.22    |
        prodidExport700             = 63,  // 2002    | Export         | 7.00    |
        prodidMasm700               = 64,  // 2002    | MASM           | 7.00    |
        prodidUtc13_POGO_I_C        = 65,  // 2002    | C (POGO I)     | 7.00    |
        prodidUtc13_POGO_I_CPP      = 66,  // 2002    | C++ (POGO I)   | 7.00    |
        prodidUtc13_POGO_O_C        = 67,  // 2002    | C (POGO O)     | 7.00    |
        prodidUtc13_POGO_O_CPP      = 68,  // 2002    | C++ (POGO O)   | 7.00    |
        prodidCvtres700             = 69,  // 2002    | CVTRES         | 7.00    |
        prodidCvtres710p            = 70,  // 2003    | CVTRES         | 7.10p   |
        prodidLinker710p            = 71,  // 2003    | LINK           | 7.10p   |
        prodidCvtomf710p            = 72,  // 2003    | CVTOMF         | 7.10p   |
        prodidExport710p            = 73,  // 2003    | Export         | 7.10p   |
        prodidImplib710p            = 74,  // 2003    | Import         | 7.10p   |
        prodidMasm710p              = 75,  // 2003    | MASM           | 7.10p   |
        prodidUtc1310p_C            = 76,  // 2003    | C              | 7.10p   |
        prodidUtc1310p_CPP          = 77,  // 2003    | C++            | 7.10p   |
        prodidUtc1310p_C_Std        = 78,  // 2003    | C (Std)        | 7.10p   |
        prodidUtc1310p_CPP_Std      = 79,  // 2003    | C++ (Std)      | 7.10p   |
        prodidUtc1310p_LTCG_C       = 80,  // 2003    | C (LTCG)       | 7.10p   |
        prodidUtc1310p_LTCG_CPP     = 81,  // 2003    | C++ (LTCG)     | 7.10p   |
        prodidUtc1310p_POGO_I_C     = 82,  // 2003    | C (POGO I)     | 7.10p   |
        prodidUtc1310p_POGO_I_CPP   = 83,  // 2003    | C++ (POGO I)   | 7.10p   |
        prodidUtc1310p_POGO_O_C     = 84,  // 2003    | C (POGO O)     | 7.10p   |
        prodidUtc1310p_POGO_O_CPP   = 85,  // 2003    | C++ (POGO O)   | 7.10p   |
        prodidLinker624             = 86,  // 98      | LINK           | 6.24    |
        prodidCvtomf624             = 87,  // 98      | CVTOMF         | 6.24    |
        prodidExport624             = 88,  // 98      | Export         | 6.24    |
        prodidImplib624             = 89,  // 98      | Import         | 6.24    |
        prodidLinker710             = 90,  // 2003    | LINK           | 7.10    |
        prodidCvtomf710             = 91,  // 2003    | CVTOMF         | 7.10    |
        prodidExport710             = 92,  // 2003    | Export         | 7.10    |
        prodidImplib710             = 93,  // 2003    | Import         | 7.10    |
        prodidCvtres710             = 94,  // 2003    | CVTRES         | 7.10    |
        prodidUtc1310_C             = 95,  // 2003    | C              | 7.10    |
        prodidUtc1310_CPP           = 96,  // 2003    | C++            | 7.10    |
        prodidUtc1310_C_Std         = 97,  // 2003    | C (Std)        | 7.10    |
        prodidUtc1310_CPP_Std       = 98,  // 2003    | C++ (Std)      | 7.10    |
        prodidUtc1310_LTCG_C        = 99,  // 2003    | C (LTCG)       | 7.10    |
        prodidUtc1310_LTCG_CPP      = 100, // 2003    | C++ (LTCG)     | 7.10    |
        prodidUtc1310_POGO_I_C      = 101, // 2003    | C (POGO I)     | 7.10    |
        prodidUtc1310_POGO_I_CPP    = 102, // 2003    | C++ (POGO I)   | 7.10    |
        prodidUtc1310_POGO_O_C      = 103, // 2003    | C (POGO O)     | 7.10    |
        prodidUtc1310_POGO_O_CPP    = 104, // 2003    | C++ (POGO O)   | 7.10    |
        prodidAliasObj710           = 105, // 2003    | ALIASOBJ       | 7.10    |
        prodidAliasObj710p          = 106, // 2003    | ALIASOBJ       | 7.10p   |
        prodidCvtpgd1310            = 107, // 2003    | CVTPGD         | 7.10    |
        prodidCvtpgd1310p           = 108, // 2003    | CVTPGD         | 7.10p   |
        prodidUtc1400_C             = 109, // 2005    | C              | 8.0     |
        prodidUtc1400_CPP           = 110, // 2005    | C++            | 8.0     |
        prodidUtc1400_C_Std         = 111, // 2005    | C (Std)        | 8.0     |
        prodidUtc1400_CPP_Std       = 112, // 2005    | C++ (Std)      | 8.0     |
        prodidUtc1400_LTCG_C        = 113, // 2005    | C (LTCG)       | 8.0     |
        prodidUtc1400_LTCG_CPP      = 114, // 2005    | C++ (LTCG)     | 8.0     |
        prodidUtc1400_POGO_I_C      = 115, // 2005    | C (POGO I)     | 8.0     |
        prodidUtc1400_POGO_I_CPP    = 116, // 2005    | C++ (POGO I)   | 8.0     |
        prodidUtc1400_POGO_O_C      = 117, // 2005    | C (POGO O)     | 8.0     |
        prodidUtc1400_POGO_O_CPP    = 118, // 2005    | C++ (POGO O)   | 8.0     |
        prodidCvtpgd1400            = 119, // 2005    | CVTPGD         | 8.0     |
        prodidLinker800             = 120, // 2005    | LINK           | 8.0     |
        prodidCvtomf800             = 121, // 2005    | CVTOMF         | 8.0     |
        prodidExport800             = 122, // 2005    | Export         | 8.0     |
        prodidImplib800             = 123, // 2005    | Import         | 8.0     |
        prodidCvtres800             = 124, // 2005    | CVTRES         | 8.0     |
        prodidMasm800               = 125, // 2005    | MASM           | 8.0     |
        prodidAliasObj800           = 126, // 2005    | ALIASOBJ       | 8.0     |
        prodidPhoenixPrerelease     = 127, //         | Phoenix        |         | ?
        prodidUtc1400_CVTCIL_C      = 128, // 2005    | C (CVTCIL)     | 8.0     |
        prodidUtc1400_CVTCIL_CPP    = 129, // 2005    | C++ (CVTCIL)   | 8.0     |
        prodidUtc1400_LTCG_MSIL     = 130, // 2005    | MSIL (LTCG)    | 8.0     |
        prodidUtc1500_C             = 131, // 2008    | C              | 9.0     |
        prodidUtc1500_CPP           = 132, // 2008    | C++            | 9.0     |
        prodidUtc1500_C_Std         = 133, // 2008    | C (Std)        | 9.0     |
        prodidUtc1500_CPP_Std       = 134, // 2008    | C++ (Std)      | 9.0     |
        prodidUtc1500_CVTCIL_C      = 135, // 2008    | C (CVTCIL)     | 9.0     |
        prodidUtc1500_CVTCIL_CPP    = 136, // 2008    | C++ (CVTCIL)   | 9.0     |
        prodidUtc1500_LTCG_C        = 137, // 2008    | C (LTCG)       | 9.0     |
        prodidUtc1500_LTCG_CPP      = 138, // 2008    | C++ (LTCG)     | 9.0     |
        prodidUtc1500_LTCG_MSIL     = 139, // 2008    | MSIL (LTCG)    | 9.0     |
        prodidUtc1500_POGO_I_C      = 140, // 2008    | C (POGO I)     | 9.0     |
        prodidUtc1500_POGO_I_CPP    = 141, // 2008    | C++ (POGO I)   | 9.0     |
        prodidUtc1500_POGO_O_C      = 142, // 2008    | C (POGO O)     | 9.0     |
        prodidUtc1500_POGO_O_CPP    = 143, // 2008    | C++ (POGO O)   | 9.0     |
        prodidCvtpgd1500            = 144, // 2008    | CVTPGD         | 9.0     |
        prodidLinker900             = 145, // 2008    | LINK           | 9.0     |
        prodidExport900             = 146, // 2008    | Export         | 9.0     |
        prodidImplib900             = 147, // 2008    | Import         | 9.0     |
        prodidCvtres900             = 148, // 2008    | CVTRES         | 9.0     |
        prodidMasm900               = 149, // 2008    | MASM           | 9.0     |
        prodidAliasObj900           = 150, // 2008    | ALIASOBJ       | 9.0     |
        prodidResource              = 151, //         | Resource       |         | ?
        prodidAliasObj1000          = 152, // 2010    | ALIASOBJ       | 10.00   |
        prodidCvtpgd1600            = 153, // 2010    | CVTPGD         | 10.00   |
        prodidCvtres1000            = 154, // 2010    | CVTRES         | 10.00   |
        prodidExport1000            = 155, // 2010    | Export         | 10.00   |
        prodidImplib1000            = 156, // 2010    | Import         | 10.00   |
        prodidLinker1000            = 157, // 2010    | LINK           | 10.00   |
        prodidMasm1000              = 158, // 2010    | MASM           | 10.00   |
        prodidPhx1600_C             = 159, // Phoenix | C              | 10.00   |
        prodidPhx1600_CPP           = 160, // Phoenix | C++            | 10.00   |
        prodidPhx1600_CVTCIL_C      = 161, // Phoenix | C (CVTCIL)     | 10.00   |
        prodidPhx1600_CVTCIL_CPP    = 162, // Phoenix | C++ (CVTCIL)   | 10.00   |
        prodidPhx1600_LTCG_C        = 163, // Phoenix | C (LTCG)       | 10.00   |
        prodidPhx1600_LTCG_CPP      = 164, // Phoenix | C++ (LTCG)     | 10.00   |
        prodidPhx1600_LTCG_MSIL     = 165, // Phoenix | MSIL (LTCG)    | 10.00   |
        prodidPhx1600_POGO_I_C      = 166, // Phoenix | C (POGO I)     | 10.00   |
        prodidPhx1600_POGO_I_CPP    = 167, // Phoenix | C++ (POGO I)   | 10.00   |
        prodidPhx1600_POGO_O_C      = 168, // Phoenix | C (POGO O)     | 10.00   |
        prodidPhx1600_POGO_O_CPP    = 169, // Phoenix | C++ (POGO O)   | 10.00   |
        prodidUtc1600_C             = 170, // 2010    | C              | 10.00   |
        prodidUtc1600_CPP           = 171, // 2010    | C++            | 10.00   |
        prodidUtc1600_CVTCIL_C      = 172, // 2010    | C (CVTCIL)     | 10.00   |
        prodidUtc1600_CVTCIL_CPP    = 173, // 2010    | C++ (CVTCIL)   | 10.00   |
        prodidUtc1600_LTCG_C        = 174, // 2010    | C (LTCG)       | 10.00   |
        prodidUtc1600_LTCG_CPP      = 175, // 2010    | C++ (LTCG)     | 10.00   |
        prodidUtc1600_LTCG_MSIL     = 176, // 2010    | MSIL (LTCG)    | 10.00   |
        prodidUtc1600_POGO_I_C      = 177, // 2010    | C (POGO I)     | 10.00   |
        prodidUtc1600_POGO_I_CPP    = 178, // 2010    | C++ (POGO I)   | 10.00   |
        prodidUtc1600_POGO_O_C      = 179, // 2010    | C (POGO O)     | 10.00   |
        prodidUtc1600_POGO_O_CPP    = 180, // 2010    | C++ (POGO O)   | 10.00   |
        prodidAliasObj1010          = 181, // 2010    | ALIASOBJ       | 10.10   |
        prodidCvtpgd1610            = 182, // 2010    | CVTPGD         | 10.10   |
        prodidCvtres1010            = 183, // 2010    | CVTRES         | 10.10   |
        prodidExport1010            = 184, // 2010    | Export         | 10.10   |
        prodidImplib1010            = 185, // 2010    | Import         | 10.10   |
        prodidLinker1010            = 186, // 2010    | LINK           | 10.10   |
        prodidMasm1010              = 187, // 2010    | MASM           | 10.10   |
        prodidUtc1610_C             = 188, // 2010    | C              | 10.10   |
        prodidUtc1610_CPP           = 189, // 2010    | C++            | 10.10   |
        prodidUtc1610_CVTCIL_C      = 190, // 2010    | C (CVTCIL)     | 10.10   |
        prodidUtc1610_CVTCIL_CPP    = 191, // 2010    | C++ (CVTCIL)   | 10.10   |
        prodidUtc1610_LTCG_C        = 192, // 2010    | C (LTCG)       | 10.10   |
        prodidUtc1610_LTCG_CPP      = 193, // 2010    | C++ (LTCG)     | 10.10   |
        prodidUtc1610_LTCG_MSIL     = 194, // 2010    | MSIL (LTCG)    | 10.10   |
        prodidUtc1610_POGO_I_C      = 195, // 2010    | C (POGO I)     | 10.10   |
        prodidUtc1610_POGO_I_CPP    = 196, // 2010    | C++ (POGO I)   | 10.10   |
        prodidUtc1610_POGO_O_C      = 197, // 2010    | C (POGO O)     | 10.10   |
        prodidUtc1610_POGO_O_CPP    = 198, // 2010    | C++ (POGO O)   | 10.10   |
        prodidAliasObj1100          = 199, // 2012    | ALIASOBJ       | 11.00   |
        prodidCvtpgd1700            = 200, // 2012    | CVTPGD         | 11.00   |
        prodidCvtres1100            = 201, // 2012    | CVTRES         | 11.00   |
        prodidExport1100            = 202, // 2012    | Export         | 11.00   |
        prodidImplib1100            = 203, // 2012    | Import         | 11.00   |
        prodidLinker1100            = 204, // 2012    | LINK           | 11.00   |
        prodidMasm1100              = 205, // 2012    | MASM           | 11.00   |
        prodidUtc1700_C             = 206, // 2012    | C              | 11.00   |
        prodidUtc1700_CPP           = 207, // 2012    | C++            | 11.00   |
        prodidUtc1700_CVTCIL_C      = 208, // 2012    | C (CVTCIL)     | 11.00   |
        prodidUtc1700_CVTCIL_CPP    = 209, // 2012    | C++ (CVTCIL)   | 11.00   |
        prodidUtc1700_LTCG_C        = 210, // 2012    | C (LTCG)       | 11.00   |
        prodidUtc1700_LTCG_CPP      = 211, // 2012    | C++ (LTCG)     | 11.00   |
        prodidUtc1700_LTCG_MSIL     = 212, // 2012    | MSIL (LTCG)    | 11.00   |
        prodidUtc1700_POGO_I_C      = 213, // 2012    | C (POGO I)     | 11.00   |
        prodidUtc1700_POGO_I_CPP    = 214, // 2012    | C++ (POGO I)   | 11.00   |
        prodidUtc1700_POGO_O_C      = 215, // 2012    | C (POGO O)     | 11.00   |
        prodidUtc1700_POGO_O_CPP    = 216, // 2012    | C++ (POGO O)   | 11.00   |
        prodidAliasObj1200          = 217, // 2013    | ALIASOBJ       | 12.00   |
        prodidCvtpgd1800            = 218, // 2013    | CVTPGD         | 12.00   |
        prodidCvtres1200            = 219, // 2013    | CVTRES         | 12.00   |
        prodidExport1200            = 220, // 2013    | Export         | 12.00   |
        prodidImplib1200            = 221, // 2013    | Import         | 12.00   |
        prodidLinker1200            = 222, // 2013    | LINK           | 12.00   |
        prodidMasm1200              = 223, // 2013    | MASM           | 12.00   |
        prodidUtc1800_C             = 224, // 2013    | C              | 12.00   |
        prodidUtc1800_CPP           = 225, // 2013    | C++            | 12.00   |
        prodidUtc1800_CVTCIL_C      = 226, // 2013    | C (CVTCIL)     | 12.00   |
        prodidUtc1800_CVTCIL_CPP    = 211, // 2013    | C++ (CVTCIL)   | 12.00   |
        prodidUtc1800_LTCG_C        = 228, // 2013    | C (LTCG)       | 12.00   |
        prodidUtc1800_LTCG_CPP      = 229, // 2013    | C++ (LTCG)     | 12.00   |
        prodidUtc1800_LTCG_MSIL     = 230, // 2013    | MSIL (LTCG)    | 12.00   |
        prodidUtc1800_POGO_I_C      = 231, // 2013    | C (POGO I)     | 12.00   |
        prodidUtc1800_POGO_I_CPP    = 232, // 2013    | C++ (POGO I)   | 12.00   |
        prodidUtc1800_POGO_O_C      = 233, // 2013    | C (POGO O)     | 12.00   |
        prodidUtc1800_POGO_O_CPP    = 234, // 2013    | C++ (POGO O)   | 12.00   |
        prodidAliasObj1210          = 235, // 2013    | ALIASOBJ       | 12.10   |
        prodidCvtpgd1810            = 236, // 2013    | CVTPGD         | 12.10   |
        prodidCvtres1210            = 237, // 2013    | CVTRES         | 12.10   |
        prodidExport1210            = 238, // 2013    | Export         | 12.10   |
        prodidImplib1210            = 239, // 2013    | Import         | 12.10   |
        prodidLinker1210            = 240, // 2013    | LINK           | 12.10   |
        prodidMasm1210              = 241, // 2013    | MASM           | 12.10   |
        prodidUtc1810_C             = 242, // 2013    | C              | 12.10   |
        prodidUtc1810_CPP           = 243, // 2013    | C++            | 12.10   |
        prodidUtc1810_CVTCIL_C      = 244, // 2013    | C (CVTCIL)     | 12.10   |
        prodidUtc1810_CVTCIL_CPP    = 245, // 2013    | C++ (CVTCIL)   | 12.10   |
        prodidUtc1810_LTCG_C        = 246, // 2013    | C (LTCG)       | 12.10   |
        prodidUtc1810_LTCG_CPP      = 247, // 2013    | C++ (LTCG)     | 12.10   |
        prodidUtc1810_LTCG_MSIL     = 248, // 2013    | MSIL (LTCG)    | 12.10   |
        prodidUtc1810_POGO_I_C      = 249, // 2013    | C (POGO I)     | 12.10   |
        prodidUtc1810_POGO_I_CPP    = 250, // 2013    | C++ (POGO I)   | 12.10   |
        prodidUtc1810_POGO_O_C      = 251, // 2013    | C (POGO O)     | 12.10   |
        prodidUtc1810_POGO_O_CPP    = 252, // 2013    | C++ (POGO O)   | 12.10   |
        prodidAliasObj1400          = 253, // 2015+   | ALIASOBJ       | 14.0+   |
        prodidCvtpgd1900            = 254, // 2015+   | CVTPGD         | 14.0+   |
        prodidCvtres1400            = 255, // 2015+   | CVTRES         | 14.0+   |
        prodidExport1400            = 256, // 2015+   | Export         | 14.0+   |
        prodidImplib1400            = 257, // 2015+   | Import         | 14.0+   |
        prodidLinker1400            = 258, // 2015+   | LINK           | 14.0+   |
        prodidMasm1400              = 259, // 2015+   | MASM           | 14.0+   |
        prodidUtc1900_C             = 260, // 2015+   | C              | 14.0+   |
        prodidUtc1900_CPP           = 261, // 2015+   | C++            | 14.0+   |
        prodidUtc1900_CVTCIL_C      = 262, // 2015+   | C (CVTCIL)     | 14.0+   |
        prodidUtc1900_CVTCIL_CPP    = 263, // 2015+   | C++ (CVTCIL)   | 14.0+   |
        prodidUtc1900_LTCG_C        = 264, // 2015+   | C (LTCG)       | 14.0+   |
        prodidUtc1900_LTCG_CPP      = 265, // 2015+   | C++ (LTCG)     | 14.0+   |
        prodidUtc1900_LTCG_MSIL     = 266, // 2015+   | MSIL (LTCG)    | 14.0+   |
        prodidUtc1900_POGO_I_C      = 267, // 2015+   | C (POGO I)     | 14.0+   |
        prodidUtc1900_POGO_I_CPP    = 268, // 2015+   | C++ (POGO I)   | 14.0+   |
        prodidUtc1900_POGO_O_C      = 269, // 2015+   | C (POGO O)     | 14.0+   |
        prodidUtc1900_POGO_O_CPP    = 270  // 2015+   | C++ (POGO O)   | 14.0+   |
    }
}
