using System;

namespace PESpy
{
    partial struct ProdItem
    {
        /* The structure used by PEAnatomist has 5 members. The first member points to the name of the tool,
         * the second and third members store the major and minor version. The fourth member indexes into a list of product types (UTC CL, MASM, etc).
         * The fifth member is the buildId group. We store things differently: our third member
         * is a bitfield of the various "components" that the product describes. This could be tools, languages, or other items */

        private static ReadOnlySpan<(int majorVersion, int minorVersion, ProductKind kind, int group)> tools => new[]
        {
            (0,  0, ProductKind.None,                                        Toolset_None),               //prodidUnknown
            (0,  0, ProductKind.Import,                                      Toolset_None),               //prodidImport0
            (5,  10, ProductKind.LINK,                                       VS_1997_5_0),                //prodidLinker510
            (5,  10, ProductKind.CVTOMF,                                     VS_1997_5_0),                //prodidCvtomf510
            (6,  0,  ProductKind.LINK,                                       VS_1998_6_0),                //prodidLinker600
            (6,  0,  ProductKind.CVTOMF,                                     VS_1998_6_0),                //prodidCvtomf600
            (5,  0,  ProductKind.CVTRES,                                     VS_1997_5_0),                //prodidCvtres500
            (11, 0,  ProductKind.C2 | ProductKind.VB,                        VS_1997_5_0),                //prodidUtc11_Basic
            (11, 0,  ProductKind.C2 | ProductKind.C | ProductKind.CPP,       VS_1997_5_0),                //prodidUtc11_C
            (12, 0,  ProductKind.C2 | ProductKind.VB,                        VS_1998_6_0),                //prodidUtc12_Basic
            (12, 0,  ProductKind.C2 | ProductKind.C,                         VS_1998_6_0),                //prodidUtc12_C
            (12, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_1998_6_0),                //prodidUtc12_CPP
            (6,  0,  ProductKind.ALIASOBJ,                                   VS_1998_6_0),                //prodidAliasObj60
            (12, 0,  ProductKind.VB,                                         VS_1998_6_0),                //prodidVisualBasic60
            (6,  13, ProductKind.ASM | ProductKind.MASM,                     MASM_6_13),                  //prodidMasm613
            (7,  10, ProductKind.ASM | ProductKind.MASM,                     VS_net_2003_7_1),            //prodidMasm710
            (5,  11, ProductKind.LINK,                                       VS_1997_5_0),                //prodidLinker511
            (5,  11, ProductKind.CVTOMF,                                     VS_1997_5_0),                //prodidCvtomf511
            (6,  14, ProductKind.ASM | ProductKind.MASM,                     MASM_6_14),                  //prodidMasm614
            (5,  12, ProductKind.LINK,                                       VS_1997_5_0),                //prodidLinker512
            (5,  12, ProductKind.CVTOMF,                                     VS_1997_5_0),                //prodidCvtomf512
            (12, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.Std,     VS_1998_6_0),                //prodidUtc12_C_Std
            (12, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.Std,     VS_1998_6_0),                //prodidUtc12_CPP_Std
            (12, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.Book,    VS_1998_6_0),                //prodidUtc12_C_Book
            (12, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.Book,    VS_1998_6_0),                //prodidUtc12_CPP_Book
            (7,  0,  ProductKind.Import,                                     VS_net_2002_7_0),            //prodidImplib700
            (7,  0,  ProductKind.CVTOMF,                                     VS_net_2002_7_0),            //prodidCvtomf700
            (13, 0,  ProductKind.C2 | ProductKind.VB,                        VS_net_2002_7_0),            //prodidUtc13_Basic
            (13, 0,  ProductKind.C2 | ProductKind.C,                         VS_net_2002_7_0),            //prodidUtc13_C
            (13, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_net_2002_7_0),            //prodidUtc13_CPP
            (6,  10, ProductKind.LINK,                                       VC_Tools_6_1),               //prodidLinker610
            (6,  10, ProductKind.CVTOMF,                                     VC_Tools_6_1),               //prodidCvtomf610
            (6,  1,  ProductKind.LINK,                                       VC_Tools_6_1),               //prodidLinker601
            (6,  1,  ProductKind.CVTOMF,                                     VC_Tools_6_1),               //prodidCvtomf601
            (12, 10, ProductKind.C2 | ProductKind.VB,                        VC_Tools_6_1),               //prodidUtc12_1_Basic
            (12, 10, ProductKind.C2 | ProductKind.C,                         VC_Tools_6_1),               //prodidUtc12_1_C
            (12, 10, ProductKind.C2 | ProductKind.CPP,                       VC_Tools_6_1),               //prodidUtc12_1_CPP
            (6,  20, ProductKind.LINK,                                       VC_Tools_6_20),              //prodidLinker620
            (6,  20, ProductKind.CVTOMF,                                     VC_Tools_6_20),              //prodidCvtomf620
            (7,  0,  ProductKind.ALIASOBJ,                                   VS_net_2002_7_0),            //prodidAliasObj70
            (6,  21, ProductKind.LINK,                                       VC_Tools_6_20),              //prodidLinker621
            (6,  21, ProductKind.CVTOMF,                                     VC_Tools_6_20),              //prodidCvtomf621
            (6,  15, ProductKind.ASM | ProductKind.MASM,                     MASM_6_15),                  //prodidMasm615
            (13, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.LTCG,    VS_net_2002_7_0),            //prodidUtc13_LTCG_C
            (13, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.LTCG,    VS_net_2002_7_0),            //prodidUtc13_LTCG_CPP
            (6,  20, ProductKind.ASM | ProductKind.MASM,                     MASM_6_20),                  //prodidMasm620
            (1,  0,  ProductKind.ILASM,                                      VS_net_2002_7_0),            //prodidILAsm100
            (12, 20, ProductKind.C2 | ProductKind.VB,                        VS_1998_6_0_Processor_Pack), //prodidUtc12_2_Basic
            (12, 20, ProductKind.C2 | ProductKind.C,                         VS_1998_6_0_Processor_Pack), //prodidUtc12_2_C
            (12, 20, ProductKind.C2 | ProductKind.CPP,                       VS_1998_6_0_Processor_Pack), //prodidUtc12_2_CPP
            (12, 20, ProductKind.C2 | ProductKind.C   | ProductKind.Std,     VS_1998_6_0_Processor_Pack), //prodidUtc12_2_C_Std
            (12, 20, ProductKind.C2 | ProductKind.CPP | ProductKind.Book,    VS_1998_6_0_Processor_Pack), //prodidUtc12_2_CPP_Std
            (12, 20, ProductKind.C2 | ProductKind.C   | ProductKind.Std,     VS_1998_6_0_Processor_Pack), //prodidUtc12_2_C_Book
            (12, 20, ProductKind.C2 | ProductKind.CPP | ProductKind.Book,    VS_1998_6_0_Processor_Pack), //prodidUtc12_2_CPP_Book
            (6,  22, ProductKind.Import,                                     VC_Tools_6_20),              //prodidImplib622
            (6,  22, ProductKind.CVTOMF,                                     VC_Tools_6_20),              //prodidCvtomf622
            (5,  1,  ProductKind.CVTRES,                                     VS_net_2002_7_0),            //prodidCvtres501
            (13, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.Std,     VS_net_2002_7_0),            //prodidUtc13_C_Std
            (13, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.Std,     VS_net_2002_7_0),            //prodidUtc13_CPP_Std
            (13, 0,  ProductKind.CVTPGD,                                     VS_net_2002_7_0),            //prodidCvtpgd1300
            (6,  22, ProductKind.LINK,                                       VC_Tools_6_20),              //prodidLinker622
            (7,  0,  ProductKind.LINK,                                       VS_net_2002_7_0),            //prodidLinker700
            (6,  22, ProductKind.Export,                                     VC_Tools_6_20),              //prodidExport622
            (7,  0,  ProductKind.Export,                                     VS_net_2002_7_0),            //prodidExport700
            (7,  0,  ProductKind.ASM | ProductKind.MASM,                     VS_net_2002_7_0),            //prodidMasm700
            (13, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.POGO_I,  VS_net_2002_7_0),            //prodidUtc13_POGO_I_C
            (13, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_I,  VS_net_2002_7_0),            //prodidUtc13_POGO_I_CPP
            (13, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.POGO_O,  VS_net_2002_7_0),            //prodidUtc13_POGO_O_C
            (13, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_O,  VS_net_2002_7_0),            //prodidUtc13_POGO_O_CPP
            (7,  0,  ProductKind.CVTRES,                                     VS_net_2002_7_0),            //prodidCvtres700
            (7,  10, ProductKind.CVTRES,                                     VS_net_2003_7_1_PreRelease), //prodidCvtres710p
            (7,  10, ProductKind.LINK,                                       VS_net_2003_7_1_PreRelease), //prodidLinker710p
            (7,  10, ProductKind.CVTOMF,                                     VS_net_2003_7_1_PreRelease), //prodidCvtomf710p
            (7,  10, ProductKind.Export,                                     VS_net_2003_7_1_PreRelease), //prodidExport710p
            (7,  10, ProductKind.Import,                                     VS_net_2003_7_1_PreRelease), //prodidImplib710p
            (7,  10, ProductKind.ASM | ProductKind.MASM,                     VS_net_2003_7_1_PreRelease), //prodidMasm710p
            (13, 10, ProductKind.C2 | ProductKind.C,                         VS_net_2003_7_1_PreRelease), //prodidUtc1310p_C
            (13, 10, ProductKind.C2 | ProductKind.CPP,                       VS_net_2003_7_1_PreRelease), //prodidUtc1310p_CPP
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.Std,     VS_net_2003_7_1_PreRelease), //prodidUtc1310p_C_Std
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.Std,     VS_net_2003_7_1_PreRelease), //prodidUtc1310p_CPP_Std
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.LTCG,    VS_net_2003_7_1_PreRelease), //prodidUtc1310p_LTCG_C
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.LTCG,    VS_net_2003_7_1_PreRelease), //prodidUtc1310p_LTCG_CPP
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.POGO_I,  VS_net_2003_7_1_PreRelease), //prodidUtc1310p_POGO_I_C
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_I,  VS_net_2003_7_1_PreRelease), //prodidUtc1310p_POGO_I_CPP
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.POGO_O,  VS_net_2003_7_1_PreRelease), //prodidUtc1310p_POGO_O_C
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_O,  VS_net_2003_7_1_PreRelease), //prodidUtc1310p_POGO_O_CPP
            (6,  24, ProductKind.LINK,                                       VC_Tools_6_20),              //prodidLinker624
            (6,  24, ProductKind.CVTOMF,                                     VC_Tools_6_20),              //prodidCvtomf624
            (6,  24, ProductKind.Export,                                     VC_Tools_6_20),              //prodidExport624
            (6,  24, ProductKind.Import,                                     VC_Tools_6_20),              //prodidImplib624
            (7,  10, ProductKind.LINK,                                       VS_net_2003_7_1),            //prodidLinker710
            (7,  10, ProductKind.CVTOMF,                                     VS_net_2003_7_1),            //prodidCvtomf710
            (7,  10, ProductKind.Export,                                     VS_net_2003_7_1),            //prodidExport710
            (7,  10, ProductKind.Import,                                     VS_net_2003_7_1),            //prodidImplib710
            (7,  10, ProductKind.CVTRES,                                     VS_net_2003_7_1),            //prodidCvtres710
            (13, 10, ProductKind.C2 | ProductKind.C,                         VS_net_2003_7_1),            //prodidUtc1310_C
            (13, 10, ProductKind.C2 | ProductKind.CPP,                       VS_net_2003_7_1),            //prodidUtc1310_CPP
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.Std,     VS_net_2003_7_1),            //prodidUtc1310_C_Std
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.Std,     VS_net_2003_7_1),            //prodidUtc1310_CPP_Std
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.LTCG,    VS_net_2003_7_1),            //prodidUtc1310_LTCG_C
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.LTCG,    VS_net_2003_7_1),            //prodidUtc1310_LTCG_CPP
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.POGO_I,  VS_net_2003_7_1),            //prodidUtc1310_POGO_I_C
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_I,  VS_net_2003_7_1),            //prodidUtc1310_POGO_I_CPP
            (13, 10, ProductKind.C2 | ProductKind.C   | ProductKind.POGO_O,  VS_net_2003_7_1),            //prodidUtc1310_POGO_O_C
            (13, 10, ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_O,  VS_net_2003_7_1),            //prodidUtc1310_POGO_O_CPP
            (7,  10, ProductKind.ALIASOBJ,                                   VS_net_2003_7_1),            //prodidAliasObj710
            (7,  10, ProductKind.ALIASOBJ,                                   VS_net_2003_7_1_PreRelease), //prodidAliasObj710p
            (13, 10, ProductKind.CVTPGD,                                     VS_net_2003_7_1),            //prodidCvtpgd1310
            (13, 10, ProductKind.CVTPGD,                                     VS_net_2003_7_1_PreRelease), //prodidCvtpgd1310p
            (14, 0,  ProductKind.C2 | ProductKind.C,                         VS_2005_8_0),                //prodidUtc1400_C
            (14, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_2005_8_0),                //prodidUtc1400_CPP
            (14, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.Std,     VS_2005_8_0),                //prodidUtc1400_C_Std
            (14, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.Std,     VS_2005_8_0),                //prodidUtc1400_CPP_Std
            (14, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.LTCG,    VS_2005_8_0),                //prodidUtc1400_LTCG_C
            (14, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.LTCG,    VS_2005_8_0),                //prodidUtc1400_LTCG_CPP
            (14, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.POGO_I,  VS_2005_8_0),                //prodidUtc1400_POGO_I_C
            (14, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_I,  VS_2005_8_0),                //prodidUtc1400_POGO_I_CPP
            (14, 0,  ProductKind.C2 | ProductKind.C   | ProductKind.POGO_O,  VS_2005_8_0),                //prodidUtc1400_POGO_O_C
            (14, 0,  ProductKind.C2 | ProductKind.CPP | ProductKind.POGO_O,  VS_2005_8_0),                //prodidUtc1400_POGO_O_CPP
            (14, 0,  ProductKind.CVTPGD,                                     VS_2005_8_0),                //prodidCvtpgd1400
            (8,  0,  ProductKind.LINK,                                       VS_2005_8_0),                //prodidLinker800
            (8,  0,  ProductKind.CVTOMF,                                     VS_2005_8_0),                //prodidCvtomf800
            (8,  0,  ProductKind.Export,                                     VS_2005_8_0),                //prodidExport800
            (8,  0,  ProductKind.Import,                                     VS_2005_8_0),                //prodidImplib800
            (8,  0,  ProductKind.CVTRES,                                     VS_2005_8_0),                //prodidCvtres800
            (8,  0,  ProductKind.ASM | ProductKind.MASM,                     VS_2005_8_0),                //prodidMasm800
            (8,  0,  ProductKind.ALIASOBJ,                                   VS_2005_8_0),                //prodidAliasObj800
            (15, 0,  ProductKind.PreRelease,                                 Phoenix_PreRelease),         //prodidPhoenixPrerelease
            (14, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2005_8_0),                //prodidUtc1400_CVTCIL_C
            (14, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2005_8_0),                //prodidUtc1400_CVTCIL_CPP
            (14, 0,  ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2005_8_0),                //prodidUtc1400_LTCG_MSIL
            (15, 0,  ProductKind.C2 | ProductKind.C,                         VS_2008_9_0),                //prodidUtc1500_C
            (15, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_2008_9_0),                //prodidUtc1500_CPP
            (15, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.Std,    VS_2008_9_0),                //prodidUtc1500_C_Std
            (15, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.Std,    VS_2008_9_0),                //prodidUtc1500_CPP_Std
            (15, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2008_9_0),                //prodidUtc1500_CVTCIL_C
            (15, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2008_9_0),                //prodidUtc1500_CVTCIL_CPP
            (15, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.LTCG,   VS_2008_9_0),                //prodidUtc1500_LTCG_C
            (15, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.LTCG,   VS_2008_9_0),                //prodidUtc1500_LTCG_CPP
            (15, 0,  ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2008_9_0),                //prodidUtc1500_LTCG_MSIL
            (15, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_I, VS_2008_9_0),                //prodidUtc1500_POGO_I_C
            (15, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_I, VS_2008_9_0),                //prodidUtc1500_POGO_I_CPP
            (15, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_O, VS_2008_9_0),                //prodidUtc1500_POGO_O_C
            (15, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_O, VS_2008_9_0),                //prodidUtc1500_POGO_O_CPP
            (15, 0,  ProductKind.CVTPGD,                                     VS_2008_9_0),                //prodidCvtpgd1500
            (9,  0,  ProductKind.LINK,                                       VS_2008_9_0),                //prodidLinker900
            (9,  0,  ProductKind.Export,                                     VS_2008_9_0),                //prodidExport900
            (9,  0,  ProductKind.Import,                                     VS_2008_9_0),                //prodidImplib900
            (9,  0,  ProductKind.CVTRES,                                     VS_2008_9_0),                //prodidCvtres900
            (9,  0,  ProductKind.ASM | ProductKind.MASM,                     VS_2008_9_0),                //prodidMasm900
            (9,  0,  ProductKind.ALIASOBJ,                                   VS_2008_9_0),                //prodidAliasObj900
            (0,  0,  ProductKind.Resource,                                   Toolset_None),               //prodidResource
            (10, 0,  ProductKind.ALIASOBJ,                                   VS_2010_10_0),               //prodidAliasObj1000
            (16, 0,  ProductKind.CVTPGD,                                     VS_2010_10_0),               //prodidCvtpgd1600
            (10, 0,  ProductKind.CVTRES,                                     VS_2010_10_0),               //prodidCvtres1000
            (10, 0,  ProductKind.Export,                                     VS_2010_10_0),               //prodidExport1000
            (10, 0,  ProductKind.Import,                                     VS_2010_10_0),               //prodidImplib1000
            (10, 0,  ProductKind.LINK,                                       VS_2010_10_0),               //prodidLinker1000
            (10, 0,  ProductKind.ASM | ProductKind.MASM,                     VS_2010_10_0),               //prodidMasm1000
            (16, 0,  ProductKind.C,                                          Phoenix_10_0),               //prodidPhx1600_C
            (16, 0,  ProductKind.CPP,                                        Phoenix_10_0),               //prodidPhx1600_CPP
            (16, 0,  ProductKind.C    | ProductKind.CVTCIL,                  Phoenix_10_0),               //prodidPhx1600_CVTCIL_C
            (16, 0,  ProductKind.CPP  | ProductKind.CVTCIL,                  Phoenix_10_0),               //prodidPhx1600_CVTCIL_CPP
            (16, 0,  ProductKind.C    | ProductKind.LTCG,                    Phoenix_10_0),               //prodidPhx1600_LTCG_C
            (16, 0,  ProductKind.CPP  | ProductKind.LTCG,                    Phoenix_10_0),               //prodidPhx1600_LTCG_CPP
            (16, 0,  ProductKind.MSIL | ProductKind.LTCG,                    Phoenix_10_0),               //prodidPhx1600_LTCG_MSIL
            (16, 0,  ProductKind.C    | ProductKind.POGO_I,                  Phoenix_10_0),               //prodidPhx1600_POGO_I_C
            (16, 0,  ProductKind.CPP  | ProductKind.POGO_I,                  Phoenix_10_0),               //prodidPhx1600_POGO_I_CPP
            (16, 0,  ProductKind.C    | ProductKind.POGO_O,                  Phoenix_10_0),               //prodidPhx1600_POGO_O_C
            (16, 0,  ProductKind.CPP  | ProductKind.POGO_O,                  Phoenix_10_0),               //prodidPhx1600_POGO_O_CPP
            (16, 0,  ProductKind.C2 | ProductKind.C,                         VS_2010_10_0),               //prodidUtc1600_C
            (16, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_2010_10_0),               //prodidUtc1600_CPP
            (16, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2010_10_0),               //prodidUtc1600_CVTCIL_C
            (16, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2010_10_0),               //prodidUtc1600_CVTCIL_CPP
            (16, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.LTCG,   VS_2010_10_0),               //prodidUtc1600_LTCG_C
            (16, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.LTCG,   VS_2010_10_0),               //prodidUtc1600_LTCG_CPP
            (16, 0,  ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2010_10_0),               //prodidUtc1600_LTCG_MSIL
            (16, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_I, VS_2010_10_0),               //prodidUtc1600_POGO_I_C
            (16, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_I, VS_2010_10_0),               //prodidUtc1600_POGO_I_CPP
            (16, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_O, VS_2010_10_0),               //prodidUtc1600_POGO_O_C
            (16, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_O, VS_2010_10_0),               //prodidUtc1600_POGO_O_CPP
            (10, 10, ProductKind.ALIASOBJ,                                   VS_2010_10_1),               //prodidAliasObj1010
            (16, 10, ProductKind.CVTPGD,                                     VS_2010_10_1),               //prodidCvtpgd1610
            (10, 10, ProductKind.CVTRES,                                     VS_2010_10_1),               //prodidCvtres1010
            (10, 10, ProductKind.Export,                                     VS_2010_10_1),               //prodidExport1010
            (10, 10, ProductKind.Import,                                     VS_2010_10_1),               //prodidImplib1010
            (10, 10, ProductKind.LINK,                                       VS_2010_10_1),               //prodidLinker1010
            (10, 10, ProductKind.ASM | ProductKind.MASM,                     VS_2010_10_1),               //prodidMasm1010
            (16, 10, ProductKind.C2 | ProductKind.C,                         VS_2010_10_1),               //prodidUtc1610_C
            (16, 10, ProductKind.C2 | ProductKind.CPP,                       VS_2010_10_1),               //prodidUtc1610_CPP
            (16, 10, ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2010_10_1),               //prodidUtc1610_CVTCIL_C
            (16, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2010_10_1),               //prodidUtc1610_CVTCIL_CPP
            (16, 10, ProductKind.C2 | ProductKind.C    | ProductKind.LTCG,   VS_2010_10_1),               //prodidUtc1610_LTCG_C
            (16, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.LTCG,   VS_2010_10_1),               //prodidUtc1610_LTCG_CPP
            (16, 10, ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2010_10_1),               //prodidUtc1610_LTCG_MSIL
            (16, 10, ProductKind.C2 | ProductKind.C    | ProductKind.POGO_I, VS_2010_10_1),               //prodidUtc1610_POGO_I_C
            (16, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_I, VS_2010_10_1),               //prodidUtc1610_POGO_I_CPP
            (16, 10, ProductKind.C2 | ProductKind.C    | ProductKind.POGO_O, VS_2010_10_1),               //prodidUtc1610_POGO_O_C
            (16, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_O, VS_2010_10_1),               //prodidUtc1610_POGO_O_CPP
            (11, 0,  ProductKind.ALIASOBJ,                                   VS_2012_11_0),               //prodidAliasObj1100
            (17, 0,  ProductKind.CVTPGD,                                     VS_2012_11_0),               //prodidCvtpgd1700
            (11, 0,  ProductKind.CVTRES,                                     VS_2012_11_0),               //prodidCvtres1100
            (11, 0,  ProductKind.Export,                                     VS_2012_11_0),               //prodidExport1100
            (11, 0,  ProductKind.Import,                                     VS_2012_11_0),               //prodidImplib1100
            (11, 0,  ProductKind.LINK,                                       VS_2012_11_0),               //prodidLinker1100
            (11, 0,  ProductKind.ASM | ProductKind.MASM,                     VS_2012_11_0),               //prodidMasm1100
            (17, 0,  ProductKind.C2 | ProductKind.C,                         VS_2012_11_0),               //prodidUtc1700_C
            (17, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_2012_11_0),               //prodidUtc1700_CPP
            (17, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2012_11_0),               //prodidUtc1700_CVTCIL_C
            (17, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2012_11_0),               //prodidUtc1700_CVTCIL_CPP
            (17, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.LTCG,   VS_2012_11_0),               //prodidUtc1700_LTCG_C
            (17, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.LTCG,   VS_2012_11_0),               //prodidUtc1700_LTCG_CPP
            (17, 0,  ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2012_11_0),               //prodidUtc1700_LTCG_MSIL
            (17, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_I, VS_2012_11_0),               //prodidUtc1700_POGO_I_C
            (17, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_I, VS_2012_11_0),               //prodidUtc1700_POGO_I_CPP
            (17, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_O, VS_2012_11_0),               //prodidUtc1700_POGO_O_C
            (17, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_O, VS_2012_11_0),               //prodidUtc1700_POGO_O_CPP
            (12, 0,  ProductKind.ALIASOBJ,                                   VS_2013_12_0),               //prodidAliasObj1200
            (18, 0,  ProductKind.CVTPGD,                                     VS_2013_12_0),               //prodidCvtpgd1800
            (12, 0,  ProductKind.CVTRES,                                     VS_2013_12_0),               //prodidCvtres1200
            (12, 0,  ProductKind.Export,                                     VS_2013_12_0),               //prodidExport1200
            (12, 0,  ProductKind.Import,                                     VS_2013_12_0),               //prodidImplib1200
            (12, 0,  ProductKind.LINK,                                       VS_2013_12_0),               //prodidLinker1200
            (12, 0,  ProductKind.ASM | ProductKind.MASM,                     VS_2013_12_0),               //prodidMasm1200
            (18, 0,  ProductKind.C2 | ProductKind.C,                         VS_2013_12_0),               //prodidUtc1800_C
            (18, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_2013_12_0),               //prodidUtc1800_CPP
            (18, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2013_12_0),               //prodidUtc1800_CVTCIL_C
            (18, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2013_12_0),               //prodidUtc1800_CVTCIL_CPP
            (18, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.LTCG,   VS_2013_12_0),               //prodidUtc1800_LTCG_C
            (18, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.LTCG,   VS_2013_12_0),               //prodidUtc1800_LTCG_CPP
            (18, 0,  ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2013_12_0),               //prodidUtc1800_LTCG_MSIL
            (18, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_I, VS_2013_12_0),               //prodidUtc1800_POGO_I_C
            (18, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_I, VS_2013_12_0),               //prodidUtc1800_POGO_I_CPP
            (18, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_O, VS_2013_12_0),               //prodidUtc1800_POGO_O_C
            (18, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_O, VS_2013_12_0),               //prodidUtc1800_POGO_O_CPP
            (12, 10, ProductKind.ALIASOBJ,                                   VS_2013_12_1),               //prodidAliasObj1210
            (18, 10, ProductKind.CVTPGD,                                     VS_2013_12_1),               //prodidCvtpgd1810
            (12, 10, ProductKind.CVTRES,                                     VS_2013_12_1),               //prodidCvtres1210
            (12, 10, ProductKind.Export,                                     VS_2013_12_1),               //prodidExport1210
            (12, 10, ProductKind.Import,                                     VS_2013_12_1),               //prodidImplib1210
            (12, 10, ProductKind.LINK,                                       VS_2013_12_1),               //prodidLinker1210
            (12, 10, ProductKind.ASM | ProductKind.MASM,                     VS_2013_12_1),               //prodidMasm1210
            (18, 10, ProductKind.C2 | ProductKind.C,                         VS_2013_12_1),               //prodidUtc1810_C
            (18, 10, ProductKind.C2 | ProductKind.CPP,                       VS_2013_12_1),               //prodidUtc1810_CPP
            (18, 10, ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2013_12_1),               //prodidUtc1810_CVTCIL_C
            (18, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2013_12_1),               //prodidUtc1810_CVTCIL_CPP
            (18, 10, ProductKind.C2 | ProductKind.C    | ProductKind.LTCG,   VS_2013_12_1),               //prodidUtc1810_LTCG_C
            (18, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.LTCG,   VS_2013_12_1),               //prodidUtc1810_LTCG_CPP
            (18, 10, ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2013_12_1),               //prodidUtc1810_LTCG_MSIL
            (18, 10, ProductKind.C2 | ProductKind.C    | ProductKind.POGO_I, VS_2013_12_1),               //prodidUtc1810_POGO_I_C
            (18, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_I, VS_2013_12_1),               //prodidUtc1810_POGO_I_CPP
            (18, 10, ProductKind.C2 | ProductKind.C    | ProductKind.POGO_O, VS_2013_12_1),               //prodidUtc1810_POGO_O_C
            (18, 10, ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_O, VS_2013_12_1),               //prodidUtc1810_POGO_O_CPP
            (14, 0,  ProductKind.ALIASOBJ,                                   VS_2015_14_0),               //prodidAliasObj1400
            (19, 0,  ProductKind.CVTPGD,                                     VS_2015_14_0),               //prodidCvtpgd1900
            (14, 0,  ProductKind.CVTRES,                                     VS_2015_14_0),               //prodidCvtres1400
            (14, 0,  ProductKind.Export,                                     VS_2015_14_0),               //prodidExport1400
            (14, 0,  ProductKind.Import,                                     VS_2015_14_0),               //prodidImplib1400
            (14, 0,  ProductKind.LINK,                                       VS_2015_14_0),               //prodidLinker1400
            (14, 0,  ProductKind.ASM | ProductKind.MASM,                     VS_2015_14_0),               //prodidMasm1400
            (19, 0,  ProductKind.C2 | ProductKind.C,                         VS_2015_14_0),               //prodidUtc1900_C
            (19, 0,  ProductKind.C2 | ProductKind.CPP,                       VS_2015_14_0),               //prodidUtc1900_CPP
            (19, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.CVTCIL, VS_2015_14_0),               //prodidUtc1900_CVTCIL_C
            (19, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.CVTCIL, VS_2015_14_0),               //prodidUtc1900_CVTCIL_CPP
            (19, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.LTCG,   VS_2015_14_0),               //prodidUtc1900_LTCG_C
            (19, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.LTCG,   VS_2015_14_0),               //prodidUtc1900_LTCG_CPP
            (19, 0,  ProductKind.C2 | ProductKind.MSIL | ProductKind.LTCG,   VS_2015_14_0),               //prodidUtc1900_LTCG_MSIL
            (19, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_I, VS_2015_14_0),               //prodidUtc1900_POGO_I_C
            (19, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_I, VS_2015_14_0),               //prodidUtc1900_POGO_I_CPP
            (19, 0,  ProductKind.C2 | ProductKind.C    | ProductKind.POGO_O, VS_2015_14_0),               //prodidUtc1900_POGO_O_C
            (19, 0,  ProductKind.C2 | ProductKind.CPP  | ProductKind.POGO_O, VS_2015_14_0),               //prodidUtc1900_POGO_O_CPP
        };

        //Tools using Utc1900

        //Starting with VS2017, cl uses the same major version number (14) for each item.
        //This table maps each build version to its corresponding minor version
        //https://learn.microsoft.com/en-us/cpp/overview/compiler-versions?view=msvc-170
        private static ReadOnlySpan<(int buildId, int minorVersion)> utc1900MinorVersionMap => new[]
        {
            (24911, 10), //VS 2017 15.0 RC3+
            (25305, 11), //VS 2017 15.3+
            (25701, 12), //VS 2017 15.5+
            (25930, 13), //VS 2017 15.6 Preview 5+
            (26310, 14), //VS 2017 15.7+
            (26504, 15), //VS 2017 15.8+
            (26814, 16), //VS 2017 15.9+
            (27316, 20), //VS 2019 16.0 Preview 3+
            (27702, 21), //VS 2019 16.1 RTM+
            (27821, 22), //VS 2019 16.2+
            (28105, 23), //VS 2019 16.3 RTM+
            (28314, 24), //VS 2019 16.4 RTM+
            (28427, 25), //VS 2019 16.5+
            (28720, 26), //VS 2019 16.6+
            (28904, 27), //VS 2019 16.7+
            (29118, 28), //VS 2019 16.8+
            (29119, 27), //VS 2019 16.7.27+
            (29213, 28), //VS 2019 16.8 Preview 2+
            (29922, 29), //VS 2019 16.10+
            (29923, 28), //VS 2019 16.9.19+
            (30031, 29), //VS 2019 16.10 Preview 2+
            (30328, 30), //VS 2022 17.0+
            (30818, 31), //VS 2022 17.1 Preview 1+
            (31114, 32), //VS 2022 17.2 Preview 1+
            (31424, 33), //VS 2022 17.3 Preview 1+
            (31721, 34), //VS 2022 17.4 Preview 1+
            (32019, 35), //VS 2022 17.5 Preview 1+
            (32323, 36), //VS 2022 17.6 Preview 1+
            (32705, 37), //VS 2022 17.7 Preview 1+
            (32919, 38), //VS 2022 17.8 Preview 1+
            (33218, 39), //VS 2022 17.9 Preview 1+
            (33617, 40), //VS 2022 17.10 Preview 2+
            (33901, 41), //VS 2022 17.11 Preview 1+
            (34226, 42), //VS 2022 17.12 Preview 1+
        };
    }
}
