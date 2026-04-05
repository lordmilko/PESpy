using System;

namespace PESpy
{
    partial struct ProdItem
    {
        //Get the span that corresponds to toolset group that the tool is associated with
        internal static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> GetToolGroup(int index)
        {
            return index switch
            {
                Toolset_None => default,
                VS_1997_5_0 => Group_VS_1997_5_0,
                VS_1998_6_0 => Group_VS_1998_6_0,
                Windows_5_0_SDK => Group_Windows_5_0_SDK,
                MASM_6_13 => Group_MASM_6_13,
                MASM_6_14 => Group_MASM_6_14,
                VC_Tools_6_10 => Group_VC_Tools_6_10,
                VC_Tools_6_1 => Group_VC_Tools_6_1,
                VC_Tools_6_20 => Group_VC_Tools_6_20,
                VC_Tools_6_21 => Group_VC_Tools_6_21,
                VS_1998_6_0_Processor_Pack => Group_VS_1998_6_0_Processor_Pack,
                VC_Tools_6_22 => Group_VC_Tools_6_22,
                MASM_6_15 => Group_MASM_6_15,
                MASM_6_20 => Group_MASM_6_20,
                Windows_5_1_SDK => Group_Windows_5_1_SDK,
                VS_net_2002_7_0 => Group_VS_net_2002_7_0,
                VS_net_2003_7_1_PreRelease => Group_VS_net_2003_7_1_PreRelease,
                //VC_Tools_6_24 => Group_//VC_Tools_6_24,
                VS_net_2003_7_1 => Group_VS_net_2003_7_1,
                VS_2005_8_0 => Group_VS_2005_8_0,
                //Phoenix_PreRelease => Group_//Phoenix_PreRelease,
                VS_2008_9_0 => Group_VS_2008_9_0,
                Phoenix_10_0 => Group_Phoenix_10_0,
                VS_2010_10_0 => Group_VS_2010_10_0,
                VS_2010_10_10 => Group_VS_2010_10_10,
                VS_2012_11_0 => Group_VS_2012_11_0,
                VS_2013_12_0 => Group_VS_2013_12_0,
                VS_2013_12_10 => Group_VS_2013_12_10,
                VS_2015_14_0 => Group_VS_2015_14_0,
            };
        }

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_1997_5_0 => new[]
        {
            //Group 1
            (7273, VS_1997_5_0, SP3),              //VS 1997 5.0 SP3
            (7274, VS_1997_5_0, SP3),              //VS 1997 5.0 SP3
            (7303, VS_1997_5_0, SP3),              //VS 1997 5.0 SP3
            (7304, VS_1997_5_0, SP3),              //VS 1997 5.0 SP3
            (7351, VS_1997_5_0, SP3),              //VS 1997 5.0 SP3
            (8034, VS_1997_5_0, SP3),              //VS 1997 5.0 SP3
            (8098, VS_1997_5_0, PDC_1998_Preview), //VS 1997 5.0 PDC 1998 Preview
            (8181, VS_1997_5_0, DDK_1998_Tools)    //VS 1997 5.0 DDK 1998 Tools
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_1998_6_0 => new[]
        {
            //Group 2
            (8041, VS_1998_6_0, March_Pre_Release), //VS 1998 6.0 March Pre-Release
            (8047, VS_1998_6_0, March_Pre_Release), //VS 1998 6.0 March Pre-Release
            (8141, VS_1998_6_0, RC),                //VS 1998 6.0 RC
            (8163, VS_1998_6_0, RTM),               //VS 1998 6.0 RTM
            (8167, VS_1998_6_0, RTM),               //VS 1998 6.0 RTM
            (8168, VS_1998_6_0, RTM),               //VS 1998 6.0 RTM
            (8169, VS_1998_6_0, RTM),               //VS 1998 6.0 RTM
            (8176, VS_1998_6_0, RTM),               //VS 1998 6.0 RTM
            (8178, VS_1998_6_0, RTM),               //VS 1998 6.0 RTM
            (8188, eMbedded_VC2_10, None),          //eMbedded VC2.10
            (8217, VS_1998_6_0, RTM),               //VS 1998 6.0 RTM
            (8247, eMbedded_VC2_10, None),          //eMbedded VC2.10
            (8268, VS_1998_6_0, SP1),               //VS 1998 6.0 SP1
            (8274, eMbedded_VC2_10, None),          //eMbedded VC2.10
            (8281, eMbedded_VC2_10, None),          //eMbedded VC2.10
            (8282, eMbedded_VC2_10, None),          //eMbedded VC2.10
            (8288, eMbedded_VC2_10, None),          //eMbedded VC2.10
            (8337, VS_1998_6_0, SP2),               //VS 1998 6.0 SP2
            (8347, VS_1998_6_0, SP2),               //VS 1998 6.0 SP2
            (8349, eMbedded_VC2_12, None),          //eMbedded VC2.12
            (8370, eMbedded_VC2_12, None),          //eMbedded VC2.12
            (8371, eMbedded_VC2_12, None),          //eMbedded VC2.12
            (8435, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8447, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8450, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8453, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8462, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8464, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8472, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8495, VS_1998_6_0, SP3),               //VS 1998 6.0 SP3
            (8783, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8797, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8798, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8799, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8804, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8841, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8862, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8867, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8876, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8877, VS_1998_6_0, SP4),               //VS 1998 6.0 SP4
            (8957, VS_1998_6_0, SP5),               //VS 1998 6.0 SP5
            (8964, VS_1998_6_0, SP5),               //VS 1998 6.0 SP5
            (8966, VS_1998_6_0, SP5),               //VS 1998 6.0 SP5
            (8988, VS_1998_6_0, SP5),               //VS 1998 6.0 SP5
            (9738, VS_1998_6_0, SP6),               //VS 1998 6.0 SP6
            (9782, VS_1998_6_0, SP6)                //VS 1998 6.0 SP6
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_Windows_5_0_SDK => new[]
        {
            //Group 3
            (1999, VS_net_2002_7_0, PDC_2000_Preview), //VS.net 2002 7.0 PDC 2000 Preview
            (2008, VS_net_2002_7_0, W2k_Beta_3_DDK),   //VS.net 2002 7.0 W2k Beta 3 DDK
            (2080, VS_net_2002_7_0, WXP_Beta_1_DDK),   //VS.net 2002 7.0 WXP Beta 1 DDK
            (2090, VS_net_2002_7_0, W2k_64Bit_RC2_DDK) //VS.net 2002 7.0 W2k 64Bit RC2 DDK
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_MASM_6_13 => new[]
        {
            //Group 4
            (8803, MASM_6_13, Developer_Preview), //MASM 6.13 Developer Preview
            (8905, MASM_6_13, WXP_Beta_1_DDK),    //MASM 6.13 WXP Beta 1 DDK
            (9030, VS_net_2002_7_0, Beta)         //VS.net 2002 7.0 Beta
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_MASM_6_14 => new[]
        {
            //Group 5
            (7299, MASM_6_14, None),              //MASM 6.14
            (8803, MASM_6_14, Developer_Preview), //MASM 6.14 Developer Preview
            (8905, MASM_6_14, WXP_Beta_1_DDK),    //MASM 6.14 WXP Beta 1 DDK
            (9030, VS_net_2002_7_0, Beta)         //VS.net 2002 7.0 Beta
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VC_Tools_6_10 => new[]
        {
            //Group 6
            (8244, eMbedded_VC2_10, None),         //eMbedded VC2.10
            (8285, eMbedded_VC2_10, None),         //eMbedded VC2.10
            (8345, eMbedded_VC3, None),            //eMbedded VC3
            (8349, eMbedded_VC3, None),            //eMbedded VC3
            (8421, VC_Tools_6_10, PSDK),           //VC Tools 6.10 PSDK
            (8426, eMbedded_VC3, None),            //eMbedded VC3
            (8430, VC_Tools_6_10, W2k_Beta_3_DDK), //VC Tools 6.10 W2k Beta 3 DDK
            (8463, eMbedded_VC2_12, None),         //eMbedded VC2.12
            (8470, VC_Tools_6_10, PSDK),           //VC Tools 6.10 PSDK
            (8495, eMbedded_VC3, None),            //eMbedded VC3
            (8511, eMbedded_VC3, None),            //eMbedded VC3
            (8569, eMbedded_VC3, None)             //eMbedded VC3
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VC_Tools_6_1 => new[]
        {
            //Group 7
            (8244, eMbedded_VC2_10, None),        //eMbedded VC2.10
            (8285, eMbedded_VC2_10, None),        //eMbedded VC2.10
            (8345, eMbedded_VC3, None),           //eMbedded VC3
            (8349, eMbedded_VC3, None),           //eMbedded VC3
            (8421, VC_Tools_6_1, PSDK),           //VC Tools 6.1 PSDK
            (8426, eMbedded_VC3, None),           //eMbedded VC3
            (8430, VC_Tools_6_1, W2k_Beta_3_DDK), //VC Tools 6.1 W2k Beta 3 DDK
            (8463, eMbedded_VC2_12, None),        //eMbedded VC2.12
            (8470, VC_Tools_6_1, PSDK),           //VC Tools 6.1 PSDK
            (8495, eMbedded_VC3, None),           //eMbedded VC3
            (8511, eMbedded_VC3, None),           //eMbedded VC3
            (8569, eMbedded_VC3, None)            //eMbedded VC3
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VC_Tools_6_20 => new[]
        {
            //Group 8
            (8528, VC_Tools_6_20, W2k_64Bit_RC1_DDK), //VC Tools 6.20 W2k 64Bit RC1 DDK
            (8667, eMbedded_VC3, None),               //eMbedded VC3
            (8700, eMbedded_VC3, None)                //eMbedded VC3
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VC_Tools_6_21 => new[]
        {
            //Group 9
            (8528, VC_Tools_6_21, W2k_64Bit_RC1_DDK), //VC Tools 6.21 W2k 64Bit RC1 DDK
            (8667, eMbedded_VC3, None),               //eMbedded VC3
            (8700, eMbedded_VC3, None)                //eMbedded VC3
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_1998_6_0_Processor_Pack => new[]
        {
            //Group 10
            (8812, VS_1998_6_0_Processor_Pack, Tech__Preview), //VS 1998 6.0 Processor Pack Tech. Preview
            (8876, VS_1998_6_0_Processor_Pack, Beta),          //VS 1998 6.0 Processor Pack Beta
            (8943, VS_1998_6_0_Processor_Pack, SP4),           //VS 1998 6.0 Processor Pack SP4
            (9036, VS_1998_6_0_Processor_Pack, SP5),           //VS 1998 6.0 Processor Pack SP5
            (9044, VS_1998_6_0_Processor_Pack, SP5),           //VS 1998 6.0 Processor Pack SP5
            (9176, eMbedded_VC4, None),                        //eMbedded VC4
            (9332, eMbedded_VC4, None),                        //eMbedded VC4
            (9356, eMbedded_VC4, None),                        //eMbedded VC4
            (9372, eMbedded_VC4, RTM),                         //eMbedded VC4 RTM
            (9394, eMbedded_VC4, None),                        //eMbedded VC4
            (9409, eMbedded_VC4, RTM),                         //eMbedded VC4 RTM
            (9419, eMbedded_VC4, RTM),                         //eMbedded VC4 RTM
            (9482, eMbedded_VC4, SP2),                         //eMbedded VC4 SP2
            (9518, eMbedded_VC4, SP2),                         //eMbedded VC4 SP2
            (9552, eMbedded_VC4, None),                        //eMbedded VC4
            (9615, eMbedded_VC4, SP2),                         //eMbedded VC4 SP2
            (9772, eMbedded_VC4, SP3),                         //eMbedded VC4 SP3
            (9773, eMbedded_VC4, SP3),                         //eMbedded VC4 SP3
            (9775, eMbedded_VC4, SP3),                         //eMbedded VC4 SP3
            (9776, eMbedded_VC4, SP4),                         //eMbedded VC4 SP4
            (9836, eMbedded_VC4, SP4)                          //eMbedded VC4 SP4
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VC_Tools_6_22 => new[]
        {
            //Group 11
            (8528, VC_Tools_6_22, W2k_64Bit_RC1_DDK), //VC Tools 6.22 W2k 64Bit RC1 DDK
            (8667, eMbedded_VC3, None),               //eMbedded VC3
            (8700, eMbedded_VC3, None)                //eMbedded VC3
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_MASM_6_15 => new[]
        {
            //Group 12
            (8803, MASM_6_15, Developer_Preview), //MASM 6.15 Developer Preview
            (8905, MASM_6_15, WXP_Beta_1_DDK),    //MASM 6.15 WXP Beta 1 DDK
            (9030, VS_net_2002_7_0, Beta)         //VS.net 2002 7.0 Beta
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_MASM_6_20 => new[]
        {
            //Group 13
            (8803, MASM_6_20, Developer_Preview), //MASM 6.20 Developer Preview
            (8905, MASM_6_20, WXP_Beta_1_DDK),    //MASM 6.20 WXP Beta 1 DDK
            (9030, VS_net_2002_7_0, Beta)         //VS.net 2002 7.0 Beta
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_Windows_5_1_SDK => new[]
        {
            //Group 14
            (2402, VS_net_2002_7_0, WXP_Beta_2_DDK) //VS.net 2002 7.0 WXP Beta 2 DDK
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_net_2002_7_0 => new[]
        {
            //Group 15
            (8177, VS_net_2002_7_0, PDC_1998_Preview),  //VS.net 2002 7.0 PDC 1998 Preview
            (8209, VS_net_2002_7_0, PDC_1998_Preview),  //VS.net 2002 7.0 PDC 1998 Preview
            (8240, VS_net_2002_7_0, PDC_1998_Preview),  //VS.net 2002 7.0 PDC 1998 Preview
            (8272, VS_net_2002_7_0, PDC_1998_Preview),  //VS.net 2002 7.0 PDC 1998 Preview
            (8378, VS_net_2002_7_0, W2k3_Beta_3_DDK),   //VS.net 2002 7.0 W2k3 Beta 3 DDK
            (8392, VS_net_2002_7_0, W2k3_Beta_3_DDK),   //VS.net 2002 7.0 W2k3 Beta 3 DDK
            (8483, VS_net_2002_7_0, W2k_64Bit_RC1_DDK), //VS.net 2002 7.0 W2k 64Bit RC1 DDK
            (8485, VS_net_2002_7_0, W2k_64Bit_RC1_DDK), //VS.net 2002 7.0 W2k 64Bit RC1 DDK
            (8499, VS_net_2002_7_0, W2k_64Bit_RC1_DDK), //VS.net 2002 7.0 W2k 64Bit RC1 DDK
            (8528, VS_net_2002_7_0, W2k_64Bit_RC1_DDK), //VS.net 2002 7.0 W2k 64Bit RC1 DDK
            (8576, VS_net_2002_7_0, W2k_64Bit_RC2_DDK), //VS.net 2002 7.0 W2k 64Bit RC2 DDK
            (8644, eMbedded_VC3, Beta),                 //eMbedded VC3 Beta
            (8681, eMbedded_VC3, Beta_2),               //eMbedded VC3 Beta 2
            (8830, VS_net_2002_7_0, WXP_Beta_1_DDK),    //VS.net 2002 7.0 WXP Beta 1 DDK
            (8905, VS_net_2002_7_0, PDC_2000_Preview),  //VS.net 2002 7.0 PDC 2000 Preview
            (8929, VS_net_2002_7_0, WXP_Beta_1_DDK),    //VS.net 2002 7.0 WXP Beta 1 DDK
            (8936, VS_net_2002_7_0, WXP_Beta_1_DDK),    //VS.net 2002 7.0 WXP Beta 1 DDK
            (8959, VS_net_2002_7_0, WXP_Beta_1_DDK),    //VS.net 2002 7.0 WXP Beta 1 DDK
            (8987, VS_net_2002_7_0, WXP_Beta_1_DDK),    //VS.net 2002 7.0 WXP Beta 1 DDK
            (9022, VS_net_2002_7_0, WXP_Beta_1_DDK),    //VS.net 2002 7.0 WXP Beta 1 DDK
            (9030, VS_net_2002_7_0, Beta),              //VS.net 2002 7.0 Beta
            (9037, VS_net_2002_7_0, WXP_Beta_2_DDK),    //VS.net 2002 7.0 WXP Beta 2 DDK
            (9043, VS_net_2002_7_0, WXP_Beta_2_DDK),    //VS.net 2002 7.0 WXP Beta 2 DDK
            (9076, VS_net_2002_7_0, WXP_Beta_2_DDK),    //VS.net 2002 7.0 WXP Beta 2 DDK
            (9175, VS_net_2002_7_0, XDK),               //VS.net 2002 7.0 XDK
            (9176, VS_net_2002_7_0, WXP_DDK),           //VS.net 2002 7.0 WXP DDK
            (9178, VS_net_2002_7_0, WXP_DDK),           //VS.net 2002 7.0 WXP DDK
            (9210, VS_net_2002_7_0, WXP_DDK),           //VS.net 2002 7.0 WXP DDK
            (9234, VS_net_2002_7_0, WXP_IA64_DDK),      //VS.net 2002 7.0 WXP IA64 DDK
            (9254, VS_net_2002_7_0, Beta_2),            //VS.net 2002 7.0 Beta 2
            (9290, VS_net_2002_7_0, XDK),               //VS.net 2002 7.0 XDK
            (9337, VS_net_2002_7_0, W2k3_Beta_3_DDK),   //VS.net 2002 7.0 W2k3 Beta 3 DDK
            (9351, VS_net_2002_7_0, W2k3_SP1_DDK),      //VS.net 2002 7.0 W2k3 SP1 DDK
            (9372, VS_net_2002_7_0, RC),                //VS.net 2002 7.0 RC
            (9466, VS_net_2002_7_0, RTM),               //VS.net 2002 7.0 RTM
            (9483, VS_net_2002_7_0, W2k3_DDK),          //VS.net 2002 7.0 W2k3 DDK
            (9500, VS_net_2002_7_0, W2k3_RC2_PSDK),     //VS.net 2002 7.0 W2k3 RC2 PSDK
            (9955, VS_net_2002_7_0, SP1)                //VS.net 2002 7.0 SP1
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_net_2003_7_1_PreRelease => new[]
        {
            //Group 16
            (2100, VS_net_2003_7_1_PreRelease, W2k3_RC1_DDK), //VS.net 2003 7.1 PreRelease W2k3 RC1 DDK
            (2154, VS_net_2003_7_1_PreRelease, W2k3_RC1_DDK), //VS.net 2003 7.1 PreRelease W2k3 RC1 DDK
            (2240, VS_net_2003_7_1_PreRelease, W2k3_DDK),     //VS.net 2003 7.1 PreRelease W2k3 DDK
            (3085, VS_net_2003_7_1_PreRelease, WLH_4074_DDK)  //VS.net 2003 7.1 PreRelease WLH 4074 DDK
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_net_2003_7_1 => new[]
        {
            //Group 18
            (2067, VS_net_2003_7_1, W2k3_RC1_DDK),  //VS.net 2003 7.1 W2k3 RC1 DDK
            (2100, VS_net_2003_7_1, W2k3_RC1_DDK),  //VS.net 2003 7.1 W2k3 RC1 DDK
            (2154, VS_net_2003_7_1, W2k3_RC1_DDK),  //VS.net 2003 7.1 W2k3 RC1 DDK
            (2179, VS_net_2003_7_1, W2k3_DDK),      //VS.net 2003 7.1 W2k3 DDK
            (2190, VS_net_2003_7_1, W2k3_DDK),      //VS.net 2003 7.1 W2k3 DDK
            (2240, VS_net_2003_7_1, W2k3_DDK),      //VS.net 2003 7.1 W2k3 DDK
            (2292, VS_net_2003_7_1, Beta),          //VS.net 2003 7.1 Beta
            (3052, VS_net_2003_7_1, Free_Toolkit),  //VS.net 2003 7.1 Free Toolkit
            (3077, VS_net_2003_7_1, RTM),           //VS.net 2003 7.1 RTM
            (3085, VS_net_2003_7_1, WLH_4074_DDK),  //VS.net 2003 7.1 WLH 4074 DDK
            (3104, WCE5_PlatformBuilder, CTP),      //WCE5 PlatformBuilder CTP
            (3343, WCE5_PlatformBuilder, CTP),      //WCE5 PlatformBuilder CTP
            (3348, WCE5_PlatformBuilder, CTP),      //WCE5 PlatformBuilder CTP
            (3349, WCE5_PlatformBuilder, CTP),      //WCE5 PlatformBuilder CTP
            (4031, VS_net_2003_7_1, W2k3_SP1_DDK),  //VS.net 2003 7.1 W2k3 SP1 DDK
            (4035, VS_net_2003_7_1, W2k3_SP1_DDK),  //VS.net 2003 7.1 W2k3 SP1 DDK
            (4074, WCE5_PlatformBuilder, RTM),      //WCE5 PlatformBuilder RTM
            (4077, WCE5_PlatformBuilder, RTM),      //WCE5 PlatformBuilder RTM
            (4078, WCE5_PlatformBuilder, RTM),      //WCE5 PlatformBuilder RTM
            (4091, WCE5_PlatformBuilder, RTM),      //WCE5 PlatformBuilder RTM
            (4237, WCE5_PlatformBuilder, Rollup),   //WCE5 PlatformBuilder Rollup
            (4241, WCE5_PlatformBuilder, Rollup),   //WCE5 PlatformBuilder Rollup
            (4345, WCE5_PlatformBuilder, Update_2), //WCE5 PlatformBuilder Update 2
            (6030, VS_net_2003_7_1, SP1)            //VS.net 2003 7.1 SP1
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2005_8_0 => new[]
        {
            //Group 19
            (417, VS_2005_8_0_Xbox360, XDK),          //VS 2005 8.0 Xbox360 XDK
            (422, VS_2005_8_0_Xbox360, XDK),          //VS 2005 8.0 Xbox360 XDK
            (522, VS_2005_8_0_Xbox360, XDK),          //VS 2005 8.0 Xbox360 XDK
            (622, VS_2005_8_0_Xbox360, XDK),          //VS 2005 8.0 Xbox360 XDK
            (1120, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1232, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1242, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1315, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1429, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1530, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1628, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1640, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1835, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (1863, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (2110, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (2207, VS_2005_8_0, W2k3_64Bit_DDK),      //VS 2005 8.0 W2k3 64Bit DDK
            (2228, VS_2005_8_0, W2k3_64Bit_DDK),      //VS 2005 8.0 W2k3 64Bit DDK
            (2558, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (2712, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (2909, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (4404, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (4609, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (5101, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (5603, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (6021, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (6251, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (6502, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (7032, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (7151, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (7710, VS_2005_8_0_Xbox360, XDK),         //VS 2005 8.0 Xbox360 XDK
            (21118, VS_2005_8_0, Alpha),              //VS 2005 8.0 Alpha
            (21213, VS_2005_8_0, Alpha),              //VS 2005 8.0 Alpha
            (30402, VS_2005_8_0, WLH_4051_DDK),       //VS 2005 8.0 WLH 4051 DDK
            (30703, VS_2005_8_0, PDC_2003_Preview),   //VS 2005 8.0 PDC 2003 Preview
            (31008, VS_2005_8_0, WLH_4074_DDK),       //VS 2005 8.0 WLH 4074 DDK
            (31113, VS_2005_8_0, WLH_4074_DDK),       //VS 2005 8.0 WLH 4074 DDK
            (40121, VS_2005_8_0, Beta),               //VS 2005 8.0 Beta
            (40301, VS_2005_8_0, _2004_Mar_CTP),      //VS 2005 8.0 2004-Mar CTP
            (40310, VS_2005_8_0, W2k3_64Bit_SP1_DDK), //VS 2005 8.0 W2k3 64Bit SP1 DDK
            (40426, VS_2005_8_0, _2004_May_CTP),      //VS 2005 8.0 2004-May CTP
            (40507, VS_2005_8_0, Beta),               //VS 2005 8.0 Beta
            (40607, VS_2005_8_0, Beta),               //VS 2005 8.0 Beta
            (40809, VS_2005_8_0, Beta_Tools_Refresh), //VS 2005 8.0 Beta Tools Refresh
            (40816, VS_2005_8_0, WLH_5048_DDK),       //VS 2005 8.0 WLH 5048 DDK
            (40903, VS_2005_8_0, _2004_Oct_CTP),      //VS 2005 8.0 2004-Oct CTP
            (40904, VS_2005_8_0, _2004_Oct_CTP),      //VS 2005 8.0 2004-Oct CTP
            (41101, VS_2005_8_0_WCE, Beta_2),         //VS 2005 8.0 WCE Beta 2
            (41111, VS_2005_8_0, _2004_Dec_CTP),      //VS 2005 8.0 2004-Dec CTP
            (41115, VS_2005_8_0, _2004_Dec_CTP),      //VS 2005 8.0 2004-Dec CTP
            (41204, VS_2005_8_0, WLH_x64_Beta_1_DDK), //VS 2005 8.0 WLH x64 Beta 1 DDK
            (50110, VS_2005_8_0, _2005_Feb_CTP),      //VS 2005 8.0 2005-Feb CTP
            (50113, VS_2005_8_0_WCE, Beta_2),         //VS 2005 8.0 WCE Beta 2
            (50215, VS_2005_8_0, Beta_2),             //VS 2005 8.0 Beta 2
            (50601, VS_2005_8_0, _2005_Jun_CTP),      //VS 2005 8.0 2005-Jun CTP
            (50712, VS_2005_8_0, _2005_Jul_CTP),      //VS 2005 8.0 2005-Jul CTP
            (50725, VS_2005_8_0_WCE, RTM),            //VS 2005 8.0 WCE RTM
            (50727, VS_2005_8_0, RTM),                //VS 2005 8.0 RTM
            (60228, WCE6_PlatformBuilder, Beta),      //WCE6 PlatformBuilder Beta
            (60302, WCE6_PlatformBuilder, Beta),      //WCE6 PlatformBuilder Beta
            (60511, WCE6_PlatformBuilder, RTM)        //WCE6 PlatformBuilder RTM
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2008_9_0 => new[]
        {
            //Group 21
            (8153, VS_2008_9_0_Xbox360, XDK),               //VS 2008 9.0 Xbox360 XDK
            (8327, VS_2008_9_0_Xbox360, Developer_Preview), //VS 2008 9.0 Xbox360 Developer Preview
            (11209, VS_2008_9_0, _2006_Dec_CTP),            //VS 2008 9.0 2006-Dec CTP
            (11213, VS_2008_9_0_WCE, Beta),                 //VS 2008 9.0 WCE Beta
            (20209, VS_2008_9_0, _2007_Feb_CTP),            //VS 2008 9.0 2007-Feb CTP
            (20404, VS_2008_9_0, Beta),                     //VS 2008 9.0 Beta
            (20526, VS_2008_9_0, _2007_Jun_CTP),            //VS 2008 9.0 2007-Jun CTP
            (20706, VS_2008_9_0, Beta_2),                   //VS 2008 9.0 Beta 2
            (20720, VS_2008_9_0_WCE, RTM),                  //VS 2008 9.0 WCE RTM
            (21022, VS_2008_9_0, RTM),                      //VS 2008 9.0 RTM
            (21107, VS_2008_9_0_WCE, RTM),                  //VS 2008 9.0 WCE RTM
            (30219, VS_2008_9_0_WCE, None),                 //VS 2008 9.0 WCE
            (30304, VS_2008_9_0, Feature_Pack),             //VS 2008 9.0 Feature Pack
            (30411, VS_2008_9_0, Feature_Pack_Refresh),     //VS 2008 9.0 Feature Pack Refresh
            (30413, VS_2008_9_0, Feature_Pack_Refresh),     //VS 2008 9.0 Feature Pack Refresh
            (30428, VS_2008_9_0, SP1_Beta),                 //VS 2008 9.0 SP1 Beta
            (30514, VS_2008_9_1, None),                     //VS 2008 9.1
            (30729, VS_2008_9_0, SP1),                      //VS 2008 9.0 SP1
            (50110, VS_2008_9_1, None),                     //VS 2008 9.1
            (50304, VS_2008_9_1, None)                      //VS 2008 9.1
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_Phoenix_10_0 => new[]
        {
            //Group 22
            (10224, VS_2010_10_0_Xbox360, XDK),               //VS 2010 10.0 Xbox360 XDK
            (10358, VS_2010_10_0_Xbox360, Developer_Preview), //VS 2010 10.0 Xbox360 Developer Preview
            (11001, Phoenix_10_0, PDC_2008_Preview),          //Phoenix 10.0 PDC 2008 Preview
            (11886, VS_2010_10_0_Xbox360, XDK),               //VS 2010 10.0 Xbox360 XDK
            (20506, Phoenix_10_0, Beta),                      //Phoenix 10.0 Beta
            (21003, Phoenix_10_0, Beta_2),                    //Phoenix 10.0 Beta 2
            (21006, Phoenix_10_0, Beta_2),                    //Phoenix 10.0 Beta 2
            (30128, Phoenix_10_0, RC),                        //Phoenix 10.0 RC
            (30319, Phoenix_10_0, RTM),                       //Phoenix 10.0 RTM
            (30329, Phoenix_10_0, W8_7822_WDK),               //Phoenix 10.0 W8 7822 WDK
            (30414, Phoenix_10_0, W8_7822_WDK),               //Phoenix 10.0 W8 7822 WDK
            (31118, Phoenix_10_0, SP1_Beta),                  //Phoenix 10.0 SP1 Beta
            (40219, Phoenix_10_0, SP1)                        //Phoenix 10.0 SP1
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2010_10_0 => new[]
        {
            //Group 23
            (10224, VS_2010_10_0_Xbox360, XDK),               //VS 2010 10.0 Xbox360 XDK
            (10358, VS_2010_10_0_Xbox360, Developer_Preview), //VS 2010 10.0 Xbox360 Developer Preview
            (11001, VS_2010_10_0, PDC_2008_Preview),          //VS 2010 10.0 PDC 2008 Preview
            (11886, VS_2010_10_0_Xbox360, XDK),               //VS 2010 10.0 Xbox360 XDK
            (20506, VS_2010_10_0, Beta),                      //VS 2010 10.0 Beta
            (21003, VS_2010_10_0, Beta_2),                    //VS 2010 10.0 Beta 2
            (21006, VS_2010_10_0, Beta_2),                    //VS 2010 10.0 Beta 2
            (30128, VS_2010_10_0, RC),                        //VS 2010 10.0 RC
            (30319, VS_2010_10_0, RTM),                       //VS 2010 10.0 RTM
            (30329, VS_2010_10_0, W8_7822_WDK),               //VS 2010 10.0 W8 7822 WDK
            (30414, VS_2010_10_0, W8_7822_WDK),               //VS 2010 10.0 W8 7822 WDK
            (31118, VS_2010_10_0, SP1_Beta),                  //VS 2010 10.0 SP1 Beta
            (40219, VS_2010_10_0, SP1)                        //VS 2010 10.0 SP1
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2010_10_10 => new[]
        {
            //Group 24
            (16045, VS_2010_10_10_Xbox360, Developer_Preview), //VS 2010 10.10 Xbox360 Developer Preview
            (20476, VS_2010_10_10_Xbox360, Developer_Preview), //VS 2010 10.10 Xbox360 Developer Preview
            (30329, VS_2010_10_10, W8_7822_WDK),               //VS 2010 10.10 W8 7822 WDK
            (30716, VS_2010_10_10, Developer_Preview)          //VS 2010 10.10 Developer Preview
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2012_11_0 => new[]
        {
            //Group 25
            (31209, VS_2012_11_0, W8_7915_WDK),                //VS 2012 11.0 W8 7915 WDK
            (31213, VS_2012_11_0, W8_7915_WDK),                //VS 2012 11.0 W8 7915 WDK
            (40216, VS_2012_11_0, W8_7957_WDK),                //VS 2012 11.0 W8 7957 WDK
            (40825, VS_2012_11_0, Developer_Preview),          //VS 2012 11.0 Developer Preview
            (50110, VS_2012_11_0, Beta),                       //VS 2012 11.0 Beta
            (50119, VS_2012_11_0, W8Mobile_SDK),               //VS 2012 11.0 W8Mobile SDK
            (50214, VS_2012_11_0, Beta),                       //VS 2012 11.0 Beta
            (50307, VS_2012_11_0, W8Mobile_SDK),               //VS 2012 11.0 W8Mobile SDK
            (50323, VS_2012_11_0, Beta_Update),                //VS 2012 11.0 Beta Update
            (50503, VS_2012_11_0, WinPhone_Developer_Preview), //VS 2012 11.0 WinPhone Developer Preview
            (50522, VS_2012_11_0, RC),                         //VS 2012 11.0 RC
            (50524, VS_2012_11_0, RC),                         //VS 2012 11.0 RC
            (50531, VS_2012_11_0, W8Mobile_SDK),               //VS 2012 11.0 W8Mobile SDK
            (50626, VS_2012_11_0, RC_2012_Jul_Update),         //VS 2012 11.0 RC 2012-Jul Update
            (50706, VS_2012_11_0, RC_2012_Jul_Update_2),       //VS 2012 11.0 RC 2012-Jul Update 2
            (50727, VS_2012_11_0, RTM),                        //VS 2012 11.0 RTM
            (50912, VS_2012_11_0, Update_1_CTP2),              //VS 2012 11.0 Update 1 CTP2
            (51025, VS_2012_11_0, Update_1_Final_CTP),         //VS 2012 11.0 Update 1 Final CTP
            (51106, VS_2012_11_0, Update_1),                   //VS 2012 11.0 Update 1
            (51114, VS_2012_11_0, Update_1),                   //VS 2012 11.0 Update 1
            (60223, VS_2012_11_0, Update_2_CTP4),              //VS 2012 11.0 Update 2 CTP4
            (60314, VS_2012_11_0, Update_2),                   //VS 2012 11.0 Update 2
            (60315, VS_2012_11_0, Update_2),                   //VS 2012 11.0 Update 2
            (60610, VS_2012_11_0, Update_3),                   //VS 2012 11.0 Update 3
            (60722, VS_2012_11_0, Update_4_RC),                //VS 2012 11.0 Update 4 RC
            (60810, VS_2012_11_0, Update_4_RC2),               //VS 2012 11.0 Update 4 RC2
            (60830, VS_2012_11_0, Update_4_RC3),               //VS 2012 11.0 Update 4 RC3
            (60930, VS_2012_11_0, Update_4_RC4),               //VS 2012 11.0 Update 4 RC4
            (61030, VS_2012_11_0, Update_4),                   //VS 2012 11.0 Update 4
            (61219, VS_2012_11_0, Update_5),                   //VS 2012 11.0 Update 5
            (65500, VS_2012_11_0, PSDK),                       //VS 2012 11.0 PSDK
            (65501, VS_2012_11_0, PSDK)                        //VS 2012 11.0 PSDK
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2013_12_0 => new[]
        {
            //Group 26
            (20222, VS_2013_12_0, CTP),                   //VS 2013 12.0 CTP
            (20617, VS_2013_12_0, Beta),                  //VS 2013 12.0 Beta
            (20824, VS_2013_12_0, RC),                    //VS 2013 12.0 RC
            (20827, VS_2013_12_0, RC),                    //VS 2013 12.0 RC
            (21005, VS_2013_12_0, RTM),                   //VS 2013 12.0 RTM
            (21114, VS_2013_12_0, Compiler_2013_Nov_CTP), //VS 2013 12.0 Compiler 2013-Nov CTP
            (21126, VS_2013_12_0, Update_1_RC),           //VS 2013 12.0 Update 1 RC
            (30110, VS_2013_12_0, Update_1),              //VS 2013 12.0 Update 1
            (30112, VS_2013_12_0, Update_1),              //VS 2013 12.0 Update 1
            (30129, VS_2013_12_0, Update_2_CTP),          //VS 2013 12.0 Update 2 CTP
            (30203, VS_2013_12_0, W8_1Mobile_Beta_SDK),   //VS 2013 12.0 W8.1Mobile Beta SDK
            (30219, VS_2013_12_0, Update_2_CTP2),         //VS 2013 12.0 Update 2 CTP2
            (30220, VS_2013_12_0, Update_2_CTP2),         //VS 2013 12.0 Update 2 CTP2
            (30324, VS_2013_12_0, Update_2_RC),           //VS 2013 12.0 Update 2 RC
            (30501, VS_2013_12_0, Update_2),              //VS 2013 12.0 Update 2
            (30626, VS_2013_12_0, Update_3_RC),           //VS 2013 12.0 Update 3 RC
            (30723, VS_2013_12_0, Update_3),              //VS 2013 12.0 Update 3
            (31010, VS_2013_12_0, Update_4_RC),           //VS 2013 12.0 Update 4 RC
            (31101, VS_2013_12_0, Update_4),              //VS 2013 12.0 Update 4
            (40629, VS_2013_12_0, Update_5)               //VS 2013 12.0 Update 5
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2013_12_10 => new[]
        {
            //Group 27
            (40116, VS_2013_12_10, PSDK) //VS 2013 12.10 PSDK
        };

        private static ReadOnlySpan<(int toolBuildId, int nameIndex, int releaseType)> Group_VS_2015_14_0 => new[]
        {
            //Group 28
            (21730, VS_2015_14_0, CTP1),            //VS 2015 14.0 CTP1
            (21901, VS_2015_14_0, CTP2),            //VS 2015 14.0 CTP2
            (22013, VS_2015_14_0, CTP3),            //VS 2015 14.0 CTP3
            (22129, VS_2015_14_0, CTP4),            //VS 2015 14.0 CTP4
            (22310, VS_2015_14_0, Beta),            //VS 2015 14.0 Beta
            (22512, VS_2015_14_0, CTP5),            //VS 2015 14.0 CTP5
            (22609, VS_2015_14_0, CTP6),            //VS 2015 14.0 CTP6
            (22816, VS_2015_14_0, RC),              //VS 2015 14.0 RC
            (22823, VS_2015_14_0, RC),              //VS 2015 14.0 RC
            (22929, ILC, None),                     //ILC
            (23026, VS_2015_14_0, RTM),             //VS 2015 14.0 RTM
            (23115, ILC, None),                     //ILC
            (23308, VS_2015_14_0, Update_1_CTP),    //VS 2015 14.0 Update 1 CTP
            (23406, VS_2015_14_0, Update_1_RC),     //VS 2015 14.0 Update 1 RC
            (23505, VS_2015_14_0, Update_1),        //VS 2015 14.0 Update 1
            (23506, VS_2015_14_0, Update_1),        //VS 2015 14.0 Update 1
            (23524, VS_2015_14_0, Update_1),        //VS 2015 14.0 Update 1
            (23601, VS_2015_14_0, Update_1_SR1),    //VS 2015 14.0 Update 1 SR1
            (23615, VS_2015_14_0, Update_1_SR1),    //VS 2015 14.0 Update 1 SR1
            (23621, VS_2015_14_0, Update_1_SR1),    //VS 2015 14.0 Update 1 SR1
            (23631, VS_2015_14_0, Update_1_SR1),    //VS 2015 14.0 Update 1 SR1
            (23705, VS_2015_14_0, Update_1_SR2),    //VS 2015 14.0 Update 1 SR2
            (23706, VS_2015_14_0, Update_1_SR2),    //VS 2015 14.0 Update 1 SR2
            (23711, VS_2015_14_0, Update_1_SR2),    //VS 2015 14.0 Update 1 SR2
            (23722, VS_2015_14_0, Update_1_SR3),    //VS 2015 14.0 Update 1 SR3
            (23725, VS_2015_14_0, Update_1_SR3),    //VS 2015 14.0 Update 1 SR3
            (23817, VS_2017_15_0, CTP1),            //VS 2017 15.0 CTP1
            (23901, ILC, None),                     //ILC
            (23914, ILC, None),                     //ILC
            (23917, VS_2015_14_0, PSDK),            //VS 2015 14.0 PSDK
            (23918, VS_2015_14_0, Update_2),        //VS 2015 14.0 Update 2
            (24018, VS_2017_15_0, Preview_2),       //VS 2017 15.0 Preview 2
            (24116, VS_2015_14_0, Update_3_CTP),    //VS 2015 14.0 Update 3 CTP
            (24123, VS_2015_14_0, Update_3_RC),     //VS 2015 14.0 Update 3 RC
            (24201, ILC, None),                     //ILC
            (24210, VS_2015_14_0, Update_3),        //VS 2015 14.0 Update 3
            (24211, ILC, None),                     //ILC
            (24212, VS_2015_14_0, Update_3_SR2),    //VS 2015 14.0 Update 3 SR2
            (24213, VS_2015_14_0, Update_3_SR4),    //VS 2015 14.0 Update 3 SR4
            (24215, VS_2015_14_0, Update_3_SR4),    //VS 2015 14.0 Update 3 SR4
            (24218, VS_2015_14_0, Update_3_VS15_0), //VS 2015 14.0 Update 3 VS15.0
            (24225, VS_2015_14_0, Update_3_VS15_3), //VS 2015 14.0 Update 3 VS15.3
            (24231, VS_2015_14_0, Update_3_VS15_6), //VS 2015 14.0 Update 3 VS15.6
            (24234, VS_2015_14_0, Update_3_VS15_7), //VS 2015 14.0 Update 3 VS15.7
            (24241, VS_2015_14_0, None),            //VS 2015 14.0
            (24245, VS_2015_14_0, Update_3_VS16_0), //VS 2015 14.0 Update 3 VS16.0
            (24325, VS_2015_14_0, PSDK),            //VS 2015 14.0 PSDK
            (24405, VS_2017_15_0, None),            //VS 2017 15.0
            (24406, VS_2017_15_0, Preview_4),       //VS 2017 15.0 Preview 4
            (24516, VS_2017_15_0, Preview_5),       //VS 2017 15.0 Preview 5
            (24605, VS_2017_15_0, None),            //VS 2017 15.0
            (24610, VS_2017_15_0, PSDK),            //VS 2017 15.0 PSDK
            (24621, VS_2017_15_0, None),            //VS 2017 15.0
            (24629, VS_2017_15_0, RC),              //VS 2017 15.0 RC
            (24631, VS_2017_15_0, None),            //VS 2017 15.0
            (24704, VS_2017_15_0, None),            //VS 2017 15.0
            (24723, VS_2017_15_0, None),            //VS 2017 15.0
            (24728, VS_2017_15_0, RC2),             //VS 2017 15.0 RC2
            (24903, ILC, None),                     //ILC
            (24911, VS_2017_15_0, RC3),             //VS 2017 15.0 RC3
            (24930, VS_2017_15_0, RC4),             //VS 2017 15.0 RC4
            (25017, VS_2017_15_0, RTM),             //VS 2017 15.0 RTM
            (25019, VS_2017_15_0, RTM),             //VS 2017 15.0 RTM
            (25025, VS_2017_15_0, None),            //VS 2017 15.0
            (25028, VS_2017_15_0, None),            //VS 2017 15.0
            (25131, VS_2017_15_3, None),            //VS 2017 15.3
            (25203, VS_2017_15_3, PSDK),            //VS 2017 15.3 PSDK
            (25221, ILC, None),                     //ILC
            (25224, VS_2017_15_3, None),            //VS 2017 15.3
            (25305, VS_2017_15_3, None),            //VS 2017 15.3
            (25503, VS_2017_15_3, Preview_5),       //VS 2017 15.3 Preview 5
            (25506, VS_2017_15_3, RTM),             //VS 2017 15.3 RTM
            (25507, VS_2017_15_3, HAS_BUILD | 4),   //VS 2017 15.3.4
            (25508, VS_2017_15_3, HAS_BUILD | 5),   //VS 2017 15.3.5
            (25531, ILC, None),                     //ILC
            (25542, VS_2017_15_4, None),            //VS 2017 15.4
            (25545, VS_2017_15_4, None),            //VS 2017 15.4
            (25547, VS_2017_15_4, RTM),             //VS 2017 15.4 RTM
            (25548, VS_2017_15_4, None),            //VS 2017 15.4
            (25601, ILC, None),                     //ILC
            (25625, VS_2017_15_5, None),            //VS 2017 15.5
            (25701, VS_2017_15_5, None),            //VS 2017 15.5
            (25711, VS_2017_15_5, PSDK),            //VS 2017 15.5 PSDK
            (25713, VS_2017_15_5, None),            //VS 2017 15.5
            (25805, VS_2017_15_5, Preview_4),       //VS 2017 15.5 Preview 4
            (25810, VS_2017_15_5, None),            //VS 2017 15.5
            (25819, VS_2017_15_5, None),            //VS 2017 15.5
            (25827, VS_2017_15_5, Preview_5),       //VS 2017 15.5 Preview 5
            (25830, VS_2017_15_5, RTM),             //VS 2017 15.5 RTM
            (25831, VS_2017_15_5, HAS_BUILD | 2),   //VS 2017 15.5.2
            (25833, VS_2017_15_5, None),            //VS 2017 15.5
            (25834, VS_2017_15_5, HAS_BUILD | 3),   //VS 2017 15.5.3
            (25835, VS_2017_15_5, HAS_BUILD | 5),   //VS 2017 15.5.5
            (25907, VS_2017_15_6, None),            //VS 2017 15.6
            (25930, VS_2017_15_6, Preview_5),       //VS 2017 15.6 Preview 5
            (26028, VS_2017_15_6, None),            //VS 2017 15.6
            (26128, VS_2017_15_6, RTM),             //VS 2017 15.6 RTM
            (26129, VS_2017_15_6, HAS_BUILD | 3),   //VS 2017 15.6.3
            (26130, VS_2017_15_6, HAS_BUILD | 5),   //VS 2017 15.6.5
            (26131, VS_2017_15_6, HAS_BUILD | 6),   //VS 2017 15.6.6
            (26132, VS_2017_15_6, HAS_BUILD | 7),   //VS 2017 15.6.7
            (26213, VS_2017_15_6, PSDK),            //VS 2017 15.6 PSDK
            (26228, VS_2017_15_7, None),            //VS 2017 15.7
            (26310, VS_2017_15_7, None),            //VS 2017 15.7
            (26418, ILC, None),                     //ILC
            (26424, ILC, None),                     //ILC
            (26428, VS_2017_15_7, RTM),             //VS 2017 15.7 RTM
            (26429, VS_2017_15_7, HAS_BUILD | 2),   //VS 2017 15.7.2
            (26430, VS_2017_15_7, HAS_BUILD | 3),   //VS 2017 15.7.3
            (26431, VS_2017_15_7, HAS_BUILD | 4),   //VS 2017 15.7.4
            (26433, VS_2017_15_7, HAS_BUILD | 5),   //VS 2017 15.7.5
            (26504, VS_2017_15_8, None),            //VS 2017 15.8
            (26706, VS_2017_15_8, Preview_5),       //VS 2017 15.8 Preview 5
            (26715, VS_2017_15_8, PSDK),            //VS 2017 15.8 PSDK
            (26726, VS_2017_15_8, RTM),             //VS 2017 15.8 RTM
            (26727, VS_2017_15_8, HAS_BUILD | 2),   //VS 2017 15.8.2
            (26729, VS_2017_15_8, None),            //VS 2017 15.8
            (26730, VS_2017_15_8, HAS_BUILD | 3),   //VS 2017 15.8.3
            (26731, VS_2017_15_8, HAS_BUILD | 8),   //VS 2017 15.8.8
            (26732, VS_2017_15_8, HAS_BUILD | 9),   //VS 2017 15.8.9
            (26814, VS_2017_15_9, None),            //VS 2017 15.9
            (26924, ILC, None),                     //ILC
            (27015, ILC, None),                     //ILC
            (27023, VS_2017_15_9, RTM),             //VS 2017 15.9 RTM
            (27024, VS_2017_15_9, HAS_BUILD | 2),   //VS 2017 15.9.2
            (27025, VS_2017_15_9, HAS_BUILD | 4),   //VS 2017 15.9.4
            (27026, VS_2017_15_9, HAS_BUILD | 5),   //VS 2017 15.9.5
            (27027, VS_2017_15_9, HAS_BUILD | 7),   //VS 2017 15.9.7
            (27030, VS_2017_15_9, HAS_BUILD | 11),  //VS 2017 15.9.11
            (27031, VS_2017_15_9, HAS_BUILD | 12),  //VS 2017 15.9.12
            (27032, VS_2017_15_9, HAS_BUILD | 14),  //VS 2017 15.9.14
            (27034, VS_2017_15_9, HAS_BUILD | 16),  //VS 2017 15.9.16
            (27035, VS_2017_15_9, HAS_BUILD | 19),  //VS 2017 15.9.19
            (27038, VS_2017_15_9, HAS_BUILD | 21),  //VS 2017 15.9.21
            (27039, VS_2017_15_9, HAS_BUILD | 22),  //VS 2017 15.9.22
            (27040, VS_2017_15_9, HAS_BUILD | 23),  //VS 2017 15.9.23
            (27041, VS_2017_15_9, HAS_BUILD | 24),  //VS 2017 15.9.24
            (27042, VS_2017_15_9, HAS_BUILD | 25),  //VS 2017 15.9.25
            (27043, VS_2017_15_9, HAS_BUILD | 26),  //VS 2017 15.9.26
            (27044, VS_2017_15_9, HAS_BUILD | 29),  //VS 2017 15.9.29
            (27045, VS_2017_15_9, HAS_BUILD | 30),  //VS 2017 15.9.30
            (27047, VS_2017_15_9, HAS_BUILD | 46),  //VS 2017 15.9.46
            (27048, VS_2017_15_9, HAS_BUILD | 47),  //VS 2017 15.9.47
            (27049, VS_2017_15_9, HAS_BUILD | 52),  //VS 2017 15.9.52
            (27050, VS_2017_15_9, HAS_BUILD | 55),  //VS 2017 15.9.55
            (27051, VS_2017_15_9, HAS_BUILD | 57),  //VS 2017 15.9.57
            (27053, VS_2017_15_9, _69),             //VS 2017 15.9 .69
            (27054, VS_2017_15_9, _71),             //VS 2017 15.9 .71
            (27316, VS_2019_16_0, Preview_3),       //VS 2019 16.0 Preview 3
            (27323, VS_2019_16_0, RC),              //VS 2019 16.0 RC
            (27404, VS_2019_16_0, None),            //VS 2019 16.0
            (27412, VS_2019_16_0, PSDK),            //VS 2019 16.0 PSDK
            (27420, ILC, None),                     //ILC
            (27508, VS_2019_16_0, RTM),             //VS 2019 16.0 RTM
            (27519, VS_2019_16_0, HAS_BUILD | 10),  //VS 2019 16.0.10
            (27521, VS_2019_16_0, None),            //VS 2019 16.0
            (27523, VS_2019_16_0, HAS_BUILD | 12),  //VS 2019 16.0.12
            (27525, VS_2019_16_0, HAS_BUILD | 16),  //VS 2019 16.0.16
            (27702, VS_2019_16_1, RTM),             //VS 2019 16.1 RTM
            (27821, VS_2019_16_2, None),            //VS 2019 16.2
            (27905, VS_2019_16_2, RTM),             //VS 2019 16.2 RTM
            (27906, VS_2019_16_2, None),            //VS 2019 16.2
            (27911, VS_2019_16_3, Preview_1),       //VS 2019 16.3 Preview 1
            (27913, ILC, None),                     //ILC
            (28105, VS_2019_16_3, RTM),             //VS 2019 16.3 RTM
            (28106, VS_2019_16_3, HAS_BUILD | 3),   //VS 2019 16.3.3
            (28107, VS_2019_16_3, HAS_BUILD | 9),   //VS 2019 16.3.9
            (28117, VS_2019_16_3, None),            //VS 2019 16.3
            (28127, VS_2019_16_3, None),            //VS 2019 16.3
            (28200, VS_2019_16_3, PSDK),            //VS 2019 16.3 PSDK
            (28307, VS_2019_16_4, Preview_5),       //VS 2019 16.4 Preview 5
            (28314, VS_2019_16_4, RTM),             //VS 2019 16.4 RTM
            (28315, VS_2019_16_4, HAS_BUILD | 3),   //VS 2019 16.4.3
            (28316, VS_2019_16_4, HAS_BUILD | 4),   //VS 2019 16.4.4
            (28319, VS_2019_16_4, HAS_BUILD | 6),   //VS 2019 16.4.6
            (28320, VS_2019_16_4, HAS_BUILD | 9),   //VS 2019 16.4.9
            (28321, VS_2019_16_4, HAS_BUILD | 10),  //VS 2019 16.4.10
            (28322, VS_2019_16_4, HAS_BUILD | 11),  //VS 2019 16.4.11
            (28323, VS_2019_16_4, HAS_BUILD | 13),  //VS 2019 16.4.13
            (28324, VS_2019_16_4, HAS_BUILD | 14),  //VS 2019 16.4.14
            (28325, VS_2019_16_4, HAS_BUILD | 16),  //VS 2019 16.4.16
            (28326, VS_2019_16_4, HAS_BUILD | 27),  //VS 2019 16.4.27
            (28427, VS_2019_16_5, None),            //VS 2019 16.5
            (28518, VS_2019_16_5, None),            //VS 2019 16.5
            (28605, ILC, None),                     //ILC
            (28610, VS_2019_16_5, RTM),             //VS 2019 16.5 RTM
            (28611, VS_2019_16_5, HAS_BUILD | 1),   //VS 2019 16.5.1
            (28612, VS_2019_16_5, HAS_BUILD | 2),   //VS 2019 16.5.2
            (28614, VS_2019_16_5, HAS_BUILD | 4),   //VS 2019 16.5.4
            (28619, VS_2019_16_5, None),            //VS 2019 16.5
            (28624, VS_2019_16_5, None),            //VS 2019 16.5
            (28720, VS_2019_16_6, None),            //VS 2019 16.6
            (28726, VS_2019_16_6, None),            //VS 2019 16.6
            (28801, VS_2019_16_6, Preview_1),       //VS 2019 16.6 Preview 1
            (28803, VS_2019_16_6, Preview_4),       //VS 2019 16.6 Preview 4
            (28804, VS_2019_16_6, Preview_5),       //VS 2019 16.6 Preview 5
            (28805, VS_2019_16_6, RTM),             //VS 2019 16.6 RTM
            (28806, VS_2019_16_6, HAS_BUILD | 1),   //VS 2019 16.6.1
            (28808, VS_2019_16_6, None),            //VS 2019 16.6
            (28826, VS_2019_16_7, Preview_1),       //VS 2019 16.7 Preview 1
            (28900, VS_2019_16_6, PSDK),            //VS 2019 16.6 PSDK
            (28904, VS_2019_16_7, None),            //VS 2019 16.7
            (28919, VS_2019_16_7, Preview_2),       //VS 2019 16.7 Preview 2
            (28920, VS_2019_16_7, None),            //VS 2019 16.7
            (29009, VS_2019_16_7, Preview_3),       //VS 2019 16.7 Preview 3
            (29016, VS_2019_16_7, Preview_4),       //VS 2019 16.7 Preview 4
            (29109, VS_2019_16_7, Preview_5),       //VS 2019 16.7 Preview 5
            (29110, VS_2019_16_7, RTM),             //VS 2019 16.7 RTM
            (29111, VS_2019_16_7, HAS_BUILD | 1),   //VS 2019 16.7.1
            (29112, VS_2019_16_7, HAS_BUILD | 5),   //VS 2019 16.7.5
            (29114, VS_2019_16_7, HAS_BUILD | 8),   //VS 2019 16.7.8
            (29115, VS_2019_16_7, HAS_BUILD | 9),   //VS 2019 16.7.9
            (29116, VS_2019_16_7, HAS_BUILD | 11),  //VS 2019 16.7.11
            (29117, VS_2019_16_7, HAS_BUILD | 14),  //VS 2019 16.7.14
            (29118, VS_2019_16_8, None),            //VS 2019 16.8
            (29119, VS_2019_16_7, HAS_BUILD | 27),  //VS 2019 16.7.27
            (29120, VS_2019_16_7, HAS_BUILD | 28),  //VS 2019 16.7.28
            (29213, VS_2019_16_8, Preview_2),       //VS 2019 16.8 Preview 2
            (29304, VS_2019_16_8, Preview_3),       //VS 2019 16.8 Preview 3
            (29331, VS_2019_16_8, Preview_4),       //VS 2019 16.8 Preview 4
            (29333, VS_2019_16_8, RTM),             //VS 2019 16.8 RTM
            (29334, VS_2019_16_8, HAS_BUILD | 2),   //VS 2019 16.8.2
            (29335, VS_2019_16_8, HAS_BUILD | 3),   //VS 2019 16.8.3
            (29336, VS_2019_16_8, HAS_BUILD | 4),   //VS 2019 16.8.4
            (29337, VS_2019_16_8, HAS_BUILD | 5),   //VS 2019 16.8.5
            (29395, VS_2019_16_8, PSDK),            //VS 2019 16.8 PSDK
            (29429, VS_2019_16_9, None),            //VS 2019 16.9
            (29507, VS_2019_16_9, None),            //VS 2019 16.9
            (29512, ILC, None),                     //ILC
            (29515, VS_2019_16_9, Preview_1),       //VS 2019 16.9 Preview 1
            (29617, VS_2019_16_9, Preview_2),       //VS 2019 16.9 Preview 2
            (29620, VS_2019_16_9, None),            //VS 2019 16.9
            (29722, ILC, None),                     //ILC
            (29804, VS_2019_16_9, None),            //VS 2019 16.9
            (29812, VS_2019_16_9, Preview_3),       //VS 2019 16.9 Preview 3
            (29828, VS_2019_16_9, Preview_4),       //VS 2019 16.9 Preview 4
            (29910, VS_2019_16_9, RTM),             //VS 2019 16.9 RTM
            (29912, VS_2019_16_9, HAS_BUILD | 1),   //VS 2019 16.9.1
            (29913, VS_2019_16_9, HAS_BUILD | 2),   //VS 2019 16.9.2
            (29914, VS_2019_16_9, HAS_BUILD | 4),   //VS 2019 16.9.4
            (29915, VS_2019_16_9, HAS_BUILD | 5),   //VS 2019 16.9.5
            (29916, VS_2019_16_9, HAS_BUILD | 7),   //VS 2019 16.9.7
            (29917, VS_2019_16_9, HAS_BUILD | 8),   //VS 2019 16.9.8
            (29918, VS_2019_16_9, HAS_BUILD | 9),   //VS 2019 16.9.9
            (29919, VS_2019_16_9, HAS_BUILD | 10),  //VS 2019 16.9.10
            (29920, VS_2019_16_9, HAS_BUILD | 11),  //VS 2019 16.9.11
            (29921, VS_2019_16_9, HAS_BUILD | 16),  //VS 2019 16.9.16
            (29922, VS_2019_16_10, None),           //VS 2019 16.10
            (29923, VS_2019_16_9, HAS_BUILD | 19),  //VS 2019 16.9.19
            (29924, VS_2019_16_9, HAS_BUILD | 20),  //VS 2019 16.9.20
            (30031, VS_2019_16_10, Preview_2),      //VS 2019 16.10 Preview 2
            (30034, VS_2019_16_10, None),           //VS 2019 16.10
            (30035, VS_2019_16_10, Preview_3),      //VS 2019 16.10 Preview 3
            (30036, VS_2019_16_10, Preview_4),      //VS 2019 16.10 Preview 4
            (30037, VS_2019_16_10, RTM),            //VS 2019 16.10 RTM
            (30038, VS_2019_16_10, HAS_BUILD | 2),  //VS 2019 16.10.2
            (30039, VS_2019_16_10, None),           //VS 2019 16.10
            (30040, VS_2019_16_10, HAS_BUILD | 4),  //VS 2019 16.10.4
            (30095, VS_2019_16_11, PSDK),           //VS 2019 16.11 PSDK
            (30129, VS_2019_16_11, Preview_1),      //VS 2019 16.11 Preview 1
            (30130, VS_2019_16_11, Preview_2),      //VS 2019 16.11 Preview 2
            (30132, VS_2019_16_11, Preview_3),      //VS 2019 16.11 Preview 3
            (30133, VS_2019_16_11, RTM),            //VS 2019 16.11 RTM
            (30136, VS_2019_16_11, HAS_BUILD | 4),  //VS 2019 16.11.4
            (30137, VS_2019_16_11, HAS_BUILD | 6),  //VS 2019 16.11.6
            (30138, VS_2019_16_11, HAS_BUILD | 8),  //VS 2019 16.11.8
            (30139, VS_2019_16_11, HAS_BUILD | 9),  //VS 2019 16.11.9
            (30140, VS_2019_16_11, HAS_BUILD | 10), //VS 2019 16.11.10
            (30141, VS_2019_16_11, HAS_BUILD | 11), //VS 2019 16.11.11
            (30142, VS_2019_16_11, HAS_BUILD | 12), //VS 2019 16.11.12
            (30143, VS_2019_16_11, HAS_BUILD | 13), //VS 2019 16.11.13
            (30145, VS_2019_16_11, HAS_BUILD | 14), //VS 2019 16.11.14
            (30146, VS_2019_16_11, HAS_BUILD | 17), //VS 2019 16.11.17
            (30147, VS_2019_16_11, HAS_BUILD | 21), //VS 2019 16.11.21
            (30148, VS_2019_16_11, HAS_BUILD | 24), //VS 2019 16.11.24
            (30149, VS_2019_16_11, HAS_BUILD | 26), //VS 2019 16.11.26
            (30151, VS_2019_16_11, HAS_BUILD | 27), //VS 2019 16.11.27
            (30152, VS_2019_16_11, HAS_BUILD | 30), //VS 2019 16.11.30
            (30153, VS_2019_16_11, HAS_BUILD | 32), //VS 2019 16.11.32
            (30154, VS_2019_16_11, HAS_BUILD | 34), //VS 2019 16.11.34
            (30156, VS_2019_16_11, HAS_BUILD | 41), //VS 2019 16.11.41
            (30157, VS_2019_16_11, HAS_BUILD | 42), //VS 2019 16.11.42
            (30158, VS_2019_16_11, HAS_BUILD | 43), //VS 2019 16.11.43
            (30159, VS_2019_16_11, HAS_BUILD | 45), //VS 2019 16.11.45
            (30328, VS_2022_17_0, None),            //VS 2022 17.0
            (30401, VS_2022_17_0, Preview_2),       //VS 2022 17.0 Preview 2
            (30416, VS_2022_17_0, None),            //VS 2022 17.0
            (30423, VS_2022_17_0, Preview_3),       //VS 2022 17.0 Preview 3
            (30526, VS_2022_17_0, None),            //VS 2022 17.0
            (30528, VS_2022_17_0, Preview_4),       //VS 2022 17.0 Preview 4
            (30601, ILC, None),                     //ILC
            (30625, VS_2022_17_0, None),            //VS 2022 17.0
            (30704, VS_2022_17_0, RC),              //VS 2022 17.0 RC
            (30705, VS_2022_17_0, RTM),             //VS 2022 17.0 RTM
            (30706, VS_2022_17_0, HAS_BUILD | 2),   //VS 2022 17.0.2
            (30709, VS_2022_17_0, HAS_BUILD | 5),   //VS 2022 17.0.5
            (30710, VS_2022_17_0, HAS_BUILD | 8),   //VS 2022 17.0.8
            (30711, VS_2022_17_0, HAS_BUILD | 9),   //VS 2022 17.0.9
            (30712, VS_2022_17_0, HAS_BUILD | 19),  //VS 2022 17.0.19
            (30713, VS_2022_17_0, HAS_BUILD | 21),  //VS 2022 17.0.21
            (30715, VS_2022_17_0, HAS_BUILD | 22),  //VS 2022 17.0.22
            (30795, VS_2022_17_0, PSDK),            //VS 2022 17.0 PSDK
            (30818, VS_2022_17_1, Preview_1),       //VS 2022 17.1 Preview 1
            (30919, VS_2022_17_1, Preview_2),       //VS 2022 17.1 Preview 2
            (31103, VS_2022_17_1, Preview_3),       //VS 2022 17.1 Preview 3
            (31104, VS_2022_17_1, RTM),             //VS 2022 17.1 RTM
            (31105, VS_2022_17_1, HAS_BUILD | 2),   //VS 2022 17.1.2
            (31106, VS_2022_17_1, HAS_BUILD | 4),   //VS 2022 17.1.4
            (31107, VS_2022_17_1, HAS_BUILD | 5),   //VS 2022 17.1.5
            (31114, VS_2022_17_2, Preview_1),       //VS 2022 17.2 Preview 1
            (31116, ILC, None),                     //ILC
            (31302, VS_2022_17_2, Preview_2),       //VS 2022 17.2 Preview 2
            (31326, VS_2022_17_2, Preview_3),       //VS 2022 17.2 Preview 3
            (31328, VS_2022_17_2, RTM),             //VS 2022 17.2 RTM
            (31329, VS_2022_17_2, HAS_BUILD | 1),   //VS 2022 17.2.1
            (31332, VS_2022_17_2, HAS_BUILD | 5),   //VS 2022 17.2.5
            (31333, VS_2022_17_2, HAS_BUILD | 6),   //VS 2022 17.2.6
            (31335, VS_2022_17_2, HAS_BUILD | 8),   //VS 2022 17.2.8
            (31336, VS_2022_17_2, HAS_BUILD | 13),  //VS 2022 17.2.13
            (31337, VS_2022_17_2, HAS_BUILD | 15),  //VS 2022 17.2.15
            (31340, VS_2022_17_2, HAS_BUILD | 16),  //VS 2022 17.2.16
            (31341, VS_2022_17_2, HAS_BUILD | 19),  //VS 2022 17.2.19
            (31342, VS_2022_17_2, HAS_BUILD | 22),  //VS 2022 17.2.22
            (31424, VS_2022_17_3, Preview_1),       //VS 2022 17.3 Preview 1
            (31517, VS_2022_17_3, Preview_2),       //VS 2022 17.3 Preview 2
            (31616, VS_2022_17_3, None),            //VS 2022 17.3
            (31627, VS_2022_17_3, Preview_3),       //VS 2022 17.3 Preview 3
            (31629, VS_2022_17_3, RTM),             //VS 2022 17.3 RTM
            (31630, VS_2022_17_3, HAS_BUILD | 4),   //VS 2022 17.3.4
            (31631, VS_2022_17_3, HAS_BUILD | 6),   //VS 2022 17.3.6
            (31721, VS_2022_17_4, Preview_1),       //VS 2022 17.4 Preview 1
            (31823, VS_2022_17_4, Preview_2),       //VS 2022 17.4 Preview 2
            (31921, VS_2022_17_4, Preview_3),       //VS 2022 17.4 Preview 3
            (31931, VS_2022_17_4, Preview_4),       //VS 2022 17.4 Preview 4
            (31932, VS_2022_17_4, Preview_5),       //VS 2022 17.4 Preview 5
            (31933, VS_2022_17_4, RTM),             //VS 2022 17.4 RTM
            (31935, VS_2022_17_4, HAS_BUILD | 2),   //VS 2022 17.4.2
            (31937, VS_2022_17_4, HAS_BUILD | 3),   //VS 2022 17.4.3
            (31942, VS_2022_17_4, HAS_BUILD | 5),   //VS 2022 17.4.5
            (31943, VS_2022_17_4, HAS_BUILD | 6),   //VS 2022 17.4.6
            (31944, VS_2022_17_4, HAS_BUILD | 7),   //VS 2022 17.4.7
            (31946, VS_2022_17_4, HAS_BUILD | 8),   //VS 2022 17.4.8
            (31947, VS_2022_17_4, HAS_BUILD | 11),  //VS 2022 17.4.11
            (31948, VS_2022_17_4, HAS_BUILD | 14),  //VS 2022 17.4.14
            (31995, VS_2022_17_4, PSDK),            //VS 2022 17.4 PSDK
            (32019, VS_2022_17_5, Preview_1),       //VS 2022 17.5 Preview 1
            (32124, VS_2022_17_5, Preview_2),       //VS 2022 17.5 Preview 2
            (32213, VS_2022_17_5, Preview_3),       //VS 2022 17.5 Preview 3
            (32215, VS_2022_17_5, RTM),             //VS 2022 17.5 RTM
            (32216, VS_2022_17_5, HAS_BUILD | 3),   //VS 2022 17.5.3
            (32217, VS_2022_17_5, HAS_BUILD | 4),   //VS 2022 17.5.4
            (32323, VS_2022_17_6, Preview_1),       //VS 2022 17.6 Preview 1
            (32420, VS_2022_17_6, None),            //VS 2022 17.6
            (32502, VS_2022_17_6, Preview_2),       //VS 2022 17.6 Preview 2
            (32522, VS_2022_17_6, Preview_3),       //VS 2022 17.6 Preview 3
            (32530, VS_2022_17_6, Preview_5),       //VS 2022 17.6 Preview 5
            (32532, VS_2022_17_6, RTM),             //VS 2022 17.6 RTM
            (32533, VS_2022_17_6, None),            //VS 2022 17.6
            (32534, VS_2022_17_6, HAS_BUILD | 3),   //VS 2022 17.6.3
            (32535, VS_2022_17_6, HAS_BUILD | 4),   //VS 2022 17.6.4
            (32537, VS_2022_17_6, HAS_BUILD | 5),   //VS 2022 17.6.5
            (32538, VS_2022_17_6, HAS_BUILD | 6),   //VS 2022 17.6.6
            (32541, VS_2022_17_6, HAS_BUILD | 7),   //VS 2022 17.6.7
            (32542, VS_2022_17_6, HAS_BUILD | 8),   //VS 2022 17.6.8
            (32543, VS_2022_17_6, HAS_BUILD | 10),  //VS 2022 17.6.10
            (32544, VS_2022_17_6, HAS_BUILD | 11),  //VS 2022 17.6.11
            (32545, VS_2022_17_6, HAS_BUILD | 12),  //VS 2022 17.6.12
            (32546, VS_2022_17_6, HAS_BUILD | 16),  //VS 2022 17.6.16
            (32548, VS_2022_17_6, HAS_BUILD | 22),  //VS 2022 17.6.22
            (32595, VS_2022_17_6, PSDK),            //VS 2022 17.6 PSDK
            (32705, VS_2022_17_7, Preview_1),       //VS 2022 17.7 Preview 1
            (32820, VS_2022_17_7, Preview_3),       //VS 2022 17.7 Preview 3
            (32822, VS_2022_17_7, RTM),             //VS 2022 17.7 RTM
            (32824, VS_2022_17_7, HAS_BUILD | 4),   //VS 2022 17.7.4
            (32825, VS_2022_17_7, HAS_BUILD | 5),   //VS 2022 17.7.5
            (32826, VS_2022_17_7, HAS_BUILD | 7),   //VS 2022 17.7.7
            (32919, VS_2022_17_8, Preview_1),       //VS 2022 17.8 Preview 1
            (33030, VS_2022_17_8, Preview_2),       //VS 2022 17.8 Preview 2
            (33126, VS_2022_17_8, Preview_3),       //VS 2022 17.8 Preview 3
            (33128, VS_2022_17_8, Preview_5),       //VS 2022 17.8 Preview 5
            (33129, VS_2022_17_8, Preview_6),       //VS 2022 17.8 Preview 6
            (33130, VS_2022_17_8, RTM),             //VS 2022 17.8 RTM
            (33133, VS_2022_17_8, HAS_BUILD | 3),   //VS 2022 17.8.3
            (33134, VS_2022_17_8, HAS_BUILD | 4),   //VS 2022 17.8.4
            (33135, VS_2022_17_8, HAS_BUILD | 6),   //VS 2022 17.8.6
            (33136, VS_2022_17_8, HAS_BUILD | 8),   //VS 2022 17.8.8
            (33138, VS_2022_17_8, HAS_BUILD | 10),  //VS 2022 17.8.10
            (33139, VS_2022_17_8, HAS_BUILD | 11),  //VS 2022 17.8.11
            (33140, VS_2022_17_8, HAS_BUILD | 13),  //VS 2022 17.8.13
            (33141, VS_2022_17_8, HAS_BUILD | 14),  //VS 2022 17.8.14
            (33143, VS_2022_17_8, HAS_BUILD | 16),  //VS 2022 17.8.16
            (33144, VS_2022_17_8, HAS_BUILD | 17),  //VS 2022 17.8.17
            (33145, VS_2022_17_8, HAS_BUILD | 19),  //VS 2022 17.8.19
            (33218, VS_2022_17_9, Preview_1),       //VS 2022 17.9 Preview 1
            (33220, ILC, None),                     //ILC
            (33321, VS_2022_17_9, Preview_2),       //VS 2022 17.9 Preview 2
            (33428, VS_2022_17_9, Preview_3),       //VS 2022 17.9 Preview 3
            (33519, VS_2022_17_9, RTM),             //VS 2022 17.9 RTM
            (33520, VS_2022_17_9, HAS_BUILD | 1),   //VS 2022 17.9.1
            (33521, VS_2022_17_9, HAS_BUILD | 2),   //VS 2022 17.9.2
            (33522, VS_2022_17_9, HAS_BUILD | 3),   //VS 2022 17.9.3
            (33523, VS_2022_17_9, HAS_BUILD | 4),   //VS 2022 17.9.4
            (33617, VS_2022_17_10, Preview_2),      //VS 2022 17.10 Preview 2
            (33721, VS_2022_17_10, Preview_3),      //VS 2022 17.10 Preview 3
            (33731, VS_2022_17_10, None),           //VS 2022 17.10
            (33807, VS_2022_17_10, Preview_4),      //VS 2022 17.10 Preview 4
            (33808, VS_2022_17_10, RTM),            //VS 2022 17.10 RTM
            (33810, VS_2022_17_10, None),           //VS 2022 17.10
            (33811, VS_2022_17_10, HAS_BUILD | 1),  //VS 2022 17.10.1
            (33812, VS_2022_17_10, HAS_BUILD | 4),  //VS 2022 17.10.4
            (33813, VS_2022_17_10, HAS_BUILD | 5),  //VS 2022 17.10.5
            (33814, VS_2022_17_10, HAS_BUILD | 6),  //VS 2022 17.10.6
            (33815, VS_2022_17_10, HAS_BUILD | 7),  //VS 2022 17.10.7
            (33816, VS_2022_17_10, HAS_BUILD | 8),  //VS 2022 17.10.8
            (33817, VS_2022_17_10, HAS_BUILD | 9),  //VS 2022 17.10.9
            (33818, VS_2022_17_10, HAS_BUILD | 10), //VS 2022 17.10.10
            (33819, VS_2022_17_10, HAS_BUILD | 11), //VS 2022 17.10.11
            (33820, VS_2022_17_10, HAS_BUILD | 12), //VS 2022 17.10.12
            (33821, VS_2022_17_10, HAS_BUILD | 16), //VS 2022 17.10.16
            (33901, VS_2022_17_11, Preview_1),      //VS 2022 17.11 Preview 1
            (33923, VS_2022_17_11, Preview_2),      //VS 2022 17.11 Preview 2
            (34021, VS_2022_17_11, Preview_3),      //VS 2022 17.11 Preview 3
            (34117, VS_2022_17_11, Preview_5),      //VS 2022 17.11 Preview 5
            (34119, VS_2022_17_11, Preview_7),      //VS 2022 17.11 Preview 7
            (34120, VS_2022_17_11, RTM),            //VS 2022 17.11 RTM
            (34123, VS_2022_17_11, HAS_BUILD | 5),  //VS 2022 17.11.5
            (34226, VS_2022_17_12, Preview_1),      //VS 2022 17.12 Preview 1
            (34321, VS_2022_17_12, Preview_2),      //VS 2022 17.12 Preview 2
            (34430, VS_2022_17_12, Preview_3),      //VS 2022 17.12 Preview 3
            (34431, VS_2022_17_12, Preview_4),      //VS 2022 17.12 Preview 4
            (34432, VS_2022_17_12, Preview_5),      //VS 2022 17.12 Preview 5
            (34433, VS_2022_17_12, RTM),            //VS 2022 17.12 RTM
            (34435, VS_2022_17_12, HAS_BUILD | 2),  //VS 2022 17.12.2
            (34436, VS_2022_17_12, HAS_BUILD | 4),  //VS 2022 17.12.4
            (34437, VS_2022_17_12, PSDK),           //VS 2022 17.12 PSDK
            (34438, VS_2022_17_12, HAS_BUILD | 5),  //VS 2022 17.12.5
            (34440, VS_2022_17_12, HAS_BUILD | 6),  //VS 2022 17.12.6
            (34441, VS_2022_17_12, HAS_BUILD | 7),  //VS 2022 17.12.7
            (34442, VS_2022_17_12, PSDK),           //VS 2022 17.12 PSDK
            (34443, VS_2022_17_12, HAS_BUILD | 8),  //VS 2022 17.12.8
            (34444, VS_2022_17_12, HAS_BUILD | 9),  //VS 2022 17.12.9
            (34604, VS_2022_17_13, Preview_1),      //VS 2022 17.13 Preview 1
            (34618, VS_2022_17_13, Preview_2),      //VS 2022 17.13 Preview 2
            (34808, VS_2022_17_13, RTM),            //VS 2022 17.13 RTM
            (34809, VS_2022_17_13, HAS_BUILD | 3),  //VS 2022 17.13.3
            (34810, VS_2022_17_13, HAS_BUILD | 6),  //VS 2022 17.13.6
            (34823, VS_2022_17_14, Preview_1),      //VS 2022 17.14 Preview 1
            (34918, VS_2022_17_14, Preview_2),      //VS 2022 17.14 Preview 2
            (35109, VS_2022_17_14, Preview_3),      //VS 2022 17.14 Preview 3
            (35112, VS_2022_17_14, Preview_4),      //VS 2022 17.14 Preview 4
            (35128, VS_2022_17_14, Preview_6),      //VS 2022 17.14 Preview 6
            (35207, VS_2022_17_14, RTM),            //VS 2022 17.14 RTM
            (35208, VS_2022_17_14, HAS_BUILD | 3),  //VS 2022 17.14.3
            (35209, VS_2022_17_14, HAS_BUILD | 4),  //VS 2022 17.14.4
            (35211, VS_2022_17_14, HAS_BUILD | 6),  //VS 2022 17.14.6
            (35213, VS_2022_17_14, HAS_BUILD | 9),  //VS 2022 17.14.9
            (35214, VS_2022_17_14, HAS_BUILD | 11), //VS 2022 17.14.11
            (35215, VS_2022_17_14, HAS_BUILD | 13), //VS 2022 17.14.13
            (35216, VS_2022_17_14, HAS_BUILD | 14), //VS 2022 17.14.14
            (35217, VS_2022_17_14, HAS_BUILD | 16), //VS 2022 17.14.16
            (35219, VS_2022_17_14, HAS_BUILD | 18), //VS 2022 17.14.18
            (35220, VS_2022_17_14, HAS_BUILD | 20), //VS 2022 17.14.20
            (35221, VS_2022_17_14, HAS_BUILD | 21), //VS 2022 17.14.21
            (35222, VS_2022_17_14, HAS_BUILD | 22), //VS 2022 17.14.22
            (35223, VS_2022_17_14, HAS_BUILD | 27), //VS 2022 17.14.27
            (35224, VS_2022_17_14, HAS_BUILD | 28), //VS 2022 17.14.28
            (35225, VS_2022_17_14, HAS_BUILD | 29), //VS 2022 17.14.29
            (35403, VS_2026_18_0, None),            //VS 2026 18.0
            (35503, VS_2026_18_0, Insiders),        //VS 2026 18.0 Insiders
            (35615, VS_2026_18_0, Insiders),        //VS 2026 18.0 Insiders
            (35702, VS_2026_18_0, Insiders),        //VS 2026 18.0 Insiders
            (35710, VS_2026_18_0, Insiders),        //VS 2026 18.0 Insiders
            (35717, VS_2026_18_0, RTM),             //VS 2026 18.0 RTM
            (35718, VS_2026_18_0, HAS_BUILD | 1),   //VS 2026 18.0.1
            (35719, VS_2026_18_0, HAS_BUILD | 2),   //VS 2026 18.0.2
            (35720, VS_2026_18_1, RTM),             //VS 2026 18.1 RTM
            (35721, VS_2026_18_1, HAS_BUILD | 1),   //VS 2026 18.1.1
            (35722, VS_2026_18_2, RTM),             //VS 2026 18.2 RTM
            (35723, VS_2026_18_2, HAS_BUILD | 1),   //VS 2026 18.2.1
            (35724, VS_2026_18_3, RTM),             //VS 2026 18.3 RTM
            (35725, VS_2026_18_3, HAS_BUILD | 2),   //VS 2026 18.3.2
            (35726, VS_2026_18_3, HAS_BUILD | 3),   //VS 2026 18.3.3
            (35727, VS_2026_18_3, HAS_BUILD | 4),   //VS 2026 18.3.4
            (35728, VS_2026_18_3, HAS_BUILD | 5),   //VS 2026 18.3.5
            (36014, VS_2026_18_4, RTM),             //VS 2026 18.4 RTM
            (36122, VS_2026_18_5, Insiders),        //VS 2026 18.5 Insiders
            (36210, VS_2026_18_5, Insiders)         //VS 2026 18.5 Insiders
        };
    }
}
