using System;

namespace PESpy
{
    partial struct ProdItem
    {
        //Strings for toolset descriptors (from PEAnatomist)
        //For each build version, we maintain a mapping of which item
        //in this list that that build corresponds to

        private const int HAS_BUILD = 0xC0;

        private const int Toolset_None = 0;

        private const int VS_1997_5_0 = 1;                 //VS 1997 5.0
        //private const int VS_1997_5_0 = 2;               //VS 1997 5.0
        private const int VS_1998_6_0 = 3;                 //VS 1998 6.0
        private const int Windows_5_0_SDK = 4;             //Windows 5.0 SDK
        private const int MASM_6_13 = 5;                   //MASM 6.13
        private const int MASM_6_14 = 6;                   //MASM 6.14
        private const int VC_Tools_6_10 = 7;               //VC Tools 6.10
        private const int VC_Tools_6_1 = 8;                //VC Tools 6.1
        private const int VC_Tools_6_20 = 9;               //VC Tools 6.20
        private const int VC_Tools_6_21 = 10;              //VC Tools 6.21
        private const int VS_1998_6_0_Processor_Pack = 11; //VS 1998 6.0 Processor Pack
        private const int VC_Tools_6_22 = 12;              //VC Tools 6.22
        private const int MASM_6_15 = 13;                  //MASM 6.15
        private const int MASM_6_20 = 14;                  //MASM 6.20
        private const int Windows_5_1_SDK = 15;            //Windows 5.1 SDK
        private const int VS_net_2002_7_0 = 16;            //VS.net 2002 7.0
        private const int VS_net_2003_7_1_PreRelease = 17; //VS.net 2003 7.1 PreRelease
        private const int VC_Tools_6_24 = 18;              //VC Tools 6.24
        private const int VS_net_2003_7_1 = 19;            //VS.net 2003 7.1
        private const int VS_2005_8_0 = 20;                //VS 2005 8.0
        private const int Phoenix_PreRelease = 21;         //Phoenix PreRelease
        private const int VS_2008_9_0 = 22;                //VS 2008 9.0
        private const int Phoenix_10_0 = 23;               //Phoenix 10.0
        private const int VS_2010_10_0 = 24;               //VS 2010 10.0
        private const int VS_2010_10_10 = 25;              //VS 2010 10.10
        private const int VS_2012_11_0 = 26;               //VS 2012 11.0
        private const int VS_2013_12_0 = 27;               //VS 2013 12.0
        private const int VS_2013_12_10 = 28;              //VS 2013 12.10
        private const int VS_2015_14_0 = 29;               //VS 2015 14.0
        private const int eMbedded_VC2_10 = 30;            //eMbedded VC2.10
        private const int eMbedded_VC2_12 = 31;            //eMbedded VC2.12
        private const int eMbedded_VC3 = 32;               //eMbedded VC3
        private const int eMbedded_VC4 = 33;               //eMbedded VC4
        private const int WCE5_PlatformBuilder = 34;       //WCE5 PlatformBuilder
        private const int VS_2005_8_0_WCE = 35;            //VS 2005 8.0 WCE
        //private const int VS_2005_8_0_WCE = 36;          //VS 2005 8.0 WCE
        private const int VS_2005_8_0_Xbox360 = 37;        //VS 2005 8.0 Xbox360
        private const int WCE6_PlatformBuilder = 38;       //WCE6 PlatformBuilder
        private const int VS_2008_9_0_WCE = 39;            //VS 2008 9.0 WCE
        private const int VS_2008_9_0_Xbox360 = 40;        //VS 2008 9.0 Xbox360
        private const int VS_2008_9_1 = 41;                //VS 2008 9.1
        private const int VS_2010_10_0_Xbox360 = 42;       //VS 2010 10.0 Xbox360
        private const int VS_2010_10_10_Xbox360 = 43;      //VS 2010 10.10 Xbox360
        private const int ILC = 44;                        //ILC
        private const int VS_2017_15_0 = 45;               //VS 2017 15.0
        private const int VS_2017_15_3 = 46;               //VS 2017 15.3
        private const int VS_2017_15_4 = 47;               //VS 2017 15.4
        private const int VS_2017_15_5 = 48;               //VS 2017 15.5
        private const int VS_2017_15_6 = 49;               //VS 2017 15.6
        private const int VS_2017_15_7 = 50;               //VS 2017 15.7
        private const int VS_2017_15_8 = 51;               //VS 2017 15.8
        private const int VS_2017_15_9 = 52;               //VS 2017 15.9
        private const int VS_2019_16_0 = 53;               //VS 2019 16.0
        private const int VS_2019_16_1 = 54;               //VS 2019 16.1
        private const int VS_2019_16_2 = 55;               //VS 2019 16.2
        private const int VS_2019_16_3 = 56;               //VS 2019 16.3
        private const int VS_2019_16_4 = 57;               //VS 2019 16.4
        private const int VS_2019_16_5 = 58;               //VS 2019 16.5
        private const int VS_2019_16_6 = 59;               //VS 2019 16.6
        private const int VS_2019_16_7 = 60;               //VS 2019 16.7
        private const int VS_2019_16_8 = 61;               //VS 2019 16.8
        private const int VS_2019_16_9 = 62;               //VS 2019 16.9
        private const int VS_2019_16_10 = 63;              //VS 2019 16.10
        private const int VS_2019_16_11 = 64;              //VS 2019 16.11
        private const int VS_2022_17_0 = 65;               //VS 2022 17.0
        private const int VS_2022_17_1 = 66;               //VS 2022 17.1
        private const int VS_2022_17_2 = 67;               //VS 2022 17.2
        private const int VS_2022_17_3 = 68;               //VS 2022 17.3
        private const int VS_2022_17_4 = 69;               //VS 2022 17.4
        private const int VS_2022_17_5 = 70;               //VS 2022 17.5
        private const int VS_2022_17_6 = 71;               //VS 2022 17.6
        private const int VS_2022_17_7 = 72;               //VS 2022 17.7
        private const int VS_2022_17_8 = 73;               //VS 2022 17.8
        private const int VS_2022_17_9 = 74;               //VS 2022 17.9
        private const int VS_2022_17_10 = 75;              //VS 2022 17.10
        private const int VS_2022_17_11 = 76;              //VS 2022 17.11
        private const int VS_2022_17_12 = 77;              //VS 2022 17.12
        private const int VS_2022_17_13 = 78;              //VS 2022 17.13
        private const int VS_2022_17_14 = 79;              //VS 2022 17.14
        private const int VS_2026_18_0 = 80;               //VS 2026 18.0
        private const int VS_2026_18_1 = 81;               //VS 2026 18.1
        private const int VS_2026_18_2 = 82;               //VS 2026 18.2
        private const int VS_2026_18_3 = 83;               //VS 2026 18.3
        private const int VS_2026_18_4 = 84;               //VS 2026 18.4
        private const int VS_2026_18_5 = 85;               //VS 2026 18.5

        private static ReadOnlySpan<string> toolsetNames => new string[]
        {
            "VS 1997 5.0",
            "VS 1997 5.0",
            "VS 1998 6.0",
            "Windows 5.0 SDK",
            "MASM 6.13",
            "MASM 6.14",
            "VC Tools 6.10",
            "VC Tools 6.1",
            "VC Tools 6.20",
            "VC Tools 6.21",
            "VS 1998 6.0 Processor Pack",
            "VC Tools 6.22",
            "MASM 6.15",
            "MASM 6.20",
            "Windows 5.1 SDK",
            "VS.net 2002 7.0",
            "VS.net 2003 7.1 PreRelease",
            "VC Tools 6.24",
            "VS.net 2003 7.1",
            "VS 2005 8.0",
            "Phoenix PreRelease",
            "VS 2008 9.0",
            "Phoenix 10.0",
            "VS 2010 10.0",
            "VS 2010 10.10",
            "VS 2012 11.0",
            "VS 2013 12.0",
            "VS 2013 12.10",
            "VS 2015 14.0",
            "eMbedded VC2.10",
            "eMbedded VC2.12",
            "eMbedded VC3",
            "eMbedded VC4",
            "WCE5 PlatformBuilder",
            "VS 2005 8.0 WCE",
            "VS 2005 8.0 WCE",
            "VS 2005 8.0 Xbox360",
            "WCE6 PlatformBuilder",
            "VS 2008 9.0 WCE",
            "VS 2008 9.0 Xbox360",
            "VS 2008 9.1",
            "VS 2010 10.0 Xbox360",
            "VS 2010 10.10 Xbox360",
            "ILC",
            "VS 2017 15.0",
            "VS 2017 15.3",
            "VS 2017 15.4",
            "VS 2017 15.5",
            "VS 2017 15.6",
            "VS 2017 15.7",
            "VS 2017 15.8",
            "VS 2017 15.9",
            "VS 2019 16.0",
            "VS 2019 16.1",
            "VS 2019 16.2",
            "VS 2019 16.3",
            "VS 2019 16.4",
            "VS 2019 16.5",
            "VS 2019 16.6",
            "VS 2019 16.7",
            "VS 2019 16.8",
            "VS 2019 16.9",
            "VS 2019 16.10",
            "VS 2019 16.11",
            "VS 2022 17.0",
            "VS 2022 17.1",
            "VS 2022 17.2",
            "VS 2022 17.3",
            "VS 2022 17.4",
            "VS 2022 17.5",
            "VS 2022 17.6",
            "VS 2022 17.7",
            "VS 2022 17.8",
            "VS 2022 17.9",
            "VS 2022 17.10",
            "VS 2022 17.11",
            "VS 2022 17.12",
            "VS 2022 17.13",
            "VS 2022 17.14",
            "VS 2026 18.0",
            "VS 2026 18.1",
            "VS 2026 18.2",
            "VS 2026 18.3",
            "VS 2026 18.4",
            "VS 2026 18.5"
        };

        private const int None = 0;
        private const int RTM = 1;                         //RTM
        //private const int RTM = 2;                       //RTM
        private const int PSDK = 3;                        //PSDK
        private const int Insiders = 4;                    //Insiders
        private const int Preview_1 = 5;                   //Preview 1
        private const int Preview_2 = 6;                   //Preview 2
        private const int Preview_3 = 7;                   //Preview 3
        private const int Preview_4 = 8;                   //Preview 4
        private const int Preview_5 = 9;                   //Preview 5
        private const int Preview_6 = 10;                  //Preview 6
        private const int Preview_7 = 11;                  //Preview 7
        private const int Tech__Preview = 12;              //Tech. Preview
        private const int RC = 13;                         //RC
        private const int RC2 = 14;                        //RC2
        private const int RC3 = 15;                        //RC3
        private const int RC4 = 16;                        //RC4
        private const int CTP = 17;                        //CTP
        //private const int CTP = 18;                      //CTP
        private const int CTP1 = 19;                       //CTP1
        private const int CTP2 = 20;                       //CTP2
        private const int CTP3 = 21;                       //CTP3
        private const int CTP4 = 22;                       //CTP4
        private const int CTP5 = 23;                       //CTP5
        private const int CTP6 = 24;                       //CTP6
        private const int SP1_Beta = 25;                   //SP1 Beta
        private const int SP1 = 26;                        //SP1
        private const int SP2 = 27;                        //SP2
        private const int SP3 = 28;                        //SP3
        //private const int SP3 = 29;                      //SP3
        private const int SP4 = 30;                        //SP4
        //private const int SP4 = 31;                      //SP4
        private const int SP5 = 32;                        //SP5
        private const int SP6 = 33;                        //SP6
        private const int Alpha = 34;                      //Alpha
        private const int Beta = 35;                       //Beta
        private const int Beta_Update = 36;                //Beta Update
        private const int Beta_2 = 37;                     //Beta 2
        private const int Beta_Tools_Refresh = 38;         //Beta Tools Refresh
        private const int Free_Toolkit = 39;               //Free Toolkit
        private const int Feature_Pack = 40;               //Feature Pack
        private const int Feature_Pack_Refresh = 41;       //Feature Pack Refresh
        private const int Developer_Preview = 42;          //Developer Preview
        //private const int Developer_Preview = 43;        //Developer Preview
        //private const int Developer_Preview = 44;        //Developer Preview
        private const int WinPhone_Developer_Preview = 45; //WinPhone Developer Preview
        private const int Rollup = 46;                     //Rollup
        private const int Update_1_CTP = 47;               //Update 1 CTP
        private const int Update_1_CTP2 = 48;              //Update 1 CTP2
        private const int Update_1_Final_CTP = 49;         //Update 1 Final CTP
        private const int Update_1_RC = 50;                //Update 1 RC
        private const int Update_1 = 51;                   //Update 1
        private const int Update_2_CTP = 52;               //Update 2 CTP
        private const int Update_2_CTP2 = 53;              //Update 2 CTP2
        private const int Update_2_CTP3 = 54;              //Update 2 CTP3
        private const int Update_2_CTP4 = 55;              //Update 2 CTP4
        private const int Update_2_RC = 56;                //Update 2 RC
        private const int Update_2 = 57;                   //Update 2
        private const int Update_3_CTP = 58;               //Update 3 CTP
        private const int Update_3_RC = 59;                //Update 3 RC
        private const int Update_3 = 60;                   //Update 3
        private const int Update_4_RC = 61;                //Update 4 RC
        private const int Update_4_RC2 = 62;               //Update 4 RC2
        private const int Update_4_RC3 = 63;               //Update 4 RC3
        private const int Update_4_RC4 = 64;               //Update 4 RC4
        private const int Update_4 = 65;                   //Update 4
        private const int Update_5 = 66;                   //Update 5
        private const int March_Pre_Release = 67;          //March Pre-Release
        private const int PDC_1998_Preview = 68;           //PDC 1998 Preview
        //private const int PDC_1998_Preview = 69;         //PDC 1998 Preview
        private const int PDC_2000_Preview = 70;           //PDC 2000 Preview
        private const int PDC_2003_Preview = 71;           //PDC 2003 Preview
        private const int PDC_2008_Preview = 72;           //PDC 2008 Preview
        private const int _2004_Mar_CTP = 73;              //2004-Mar CTP
        private const int _2004_May_CTP = 74;              //2004-May CTP
        private const int _2004_Oct_CTP = 75;              //2004-Oct CTP
        private const int _2004_Dec_CTP = 76;              //2004-Dec CTP
        private const int _2005_Feb_CTP = 77;              //2005-Feb CTP
        private const int _2005_Jun_CTP = 78;              //2005-Jun CTP
        private const int _2005_Jul_CTP = 79;              //2005-Jul CTP
        private const int _2006_Dec_CTP = 80;              //2006-Dec CTP
        private const int _2007_Feb_CTP = 81;              //2007-Feb CTP
        private const int _2007_Jun_CTP = 82;              //2007-Jun CTP
        private const int RC_2012_Jul_Update = 83;         //RC 2012-Jul Update
        private const int RC_2012_Jul_Update_2 = 84;       //RC 2012-Jul Update 2
        private const int Compiler_2013_Nov_CTP = 85;      //Compiler 2013-Nov CTP
        private const int Update_1_SR1 = 86;               //Update 1 SR1
        private const int Update_1_SR2 = 87;               //Update 1 SR2
        private const int Update_1_SR3 = 88;               //Update 1 SR3
        private const int Update_3_SR2 = 89;               //Update 3 SR2
        private const int Update_3_SR4 = 90;               //Update 3 SR4
        private const int Update_3_VS15_0 = 91;            //Update 3 VS15.0
        private const int Update_3_VS15_3 = 92;            //Update 3 VS15.3
        private const int Update_3_VS15_6 = 93;            //Update 3 VS15.6
        private const int Update_3_VS15_7 = 94;            //Update 3 VS15.7
        private const int Update_3_VS16_0 = 95;            //Update 3 VS16.0
        private const int DDK_1998_Tools = 96;             //DDK 1998 Tools
        private const int W2k_Beta_3_DDK = 97;             //W2k Beta 3 DDK
        private const int W2k_64Bit_RC1_DDK = 98;          //W2k 64Bit RC1 DDK
        private const int W2k_64Bit_RC2_DDK = 99;          //W2k 64Bit RC2 DDK
        private const int WXP_Beta_1_DDK = 100;            //WXP Beta 1 DDK
        private const int WXP_Beta_2_DDK = 101;            //WXP Beta 2 DDK
        private const int WXP_DDK = 102;                   //WXP DDK
        private const int WXP_IA64_DDK = 103;              //WXP IA64 DDK
        private const int W2k3_Beta_3_DDK = 104;           //W2k3 Beta 3 DDK
        private const int W2k3_RC1_DDK = 105;              //W2k3 RC1 DDK
        private const int W2k3_RC2_PSDK = 106;             //W2k3 RC2 PSDK
        private const int W2k3_DDK = 107;                  //W2k3 DDK
        private const int W2k3_SP1_DDK = 108;              //W2k3 SP1 DDK
        private const int W2k3_64Bit_DDK = 109;            //W2k3 64Bit DDK
        private const int W2k3_64Bit_SP1_DDK = 110;        //W2k3 64Bit SP1 DDK
        private const int WLH_4051_DDK = 111;              //WLH 4051 DDK
        private const int WLH_4074_DDK = 112;              //WLH 4074 DDK
        private const int WLH_5048_DDK = 113;              //WLH 5048 DDK
        private const int WLH_x64_Beta_1_DDK = 114;        //WLH x64 Beta 1 DDK
        private const int W8Mobile_SDK = 115;              //W8Mobile SDK
        //private const int W8Mobile_SDK = 116;            //W8Mobile SDK
        //private const int W8Mobile_SDK = 117;            //W8Mobile SDK
        private const int W8_1Mobile_Beta_SDK = 118;       //W8.1Mobile Beta SDK
        private const int W8_7729_WDK = 119;               //W8 7729 WDK
        private const int W8_7822_WDK = 120;               //W8 7822 WDK
        private const int W8_7915_WDK = 121;               //W8 7915 WDK
        private const int W8_7957_WDK = 122;               //W8 7957 WDK
        private const int XDK = 123;                       //XDK
        //private const int XDK = 124;                     //XDK
        //private const int XDK = 125;                     //XDK
        //private const int XDK = 126;                     //XDK
        private const int _69 = 127;                       //.69
        private const int _71 = 128;                       //.71

        //Along with an index into the toolsetNames array, each toolset descriptor
        //also stores the build ID of the toolset (not to be confused with the build
        //ID of the tool that is found in the PRODITEM) or an index into this array to get
        //the type of toolset release that the tool was found in
        private static ReadOnlySpan<string> toolsetReleaseTypes => new string[]
        {
            "RTM",
            "RTM",
            "PSDK",
            "Insiders",
            "Preview 1",
            "Preview 2",
            "Preview 3",
            "Preview 4",
            "Preview 5",
            "Preview 6",
            "Preview 7",
            "Tech. Preview",
            "RC",
            "RC2",
            "RC3",
            "RC4",
            "CTP",
            "CTP",
            "CTP1",
            "CTP2",
            "CTP3",
            "CTP4",
            "CTP5",
            "CTP6",
            "SP1 Beta",
            "SP1",
            "SP2",
            "SP3",
            "SP3",
            "SP4",
            "SP4",
            "SP5",
            "SP6",
            "Alpha",
            "Beta",
            "Beta Update",
            "Beta 2",
            "Beta Tools Refresh",
            "Free Toolkit",
            "Feature Pack",
            "Feature Pack Refresh",
            "Developer Preview",
            "Developer Preview",
            "Developer Preview",
            "WinPhone Developer Preview",
            "Rollup",
            "Update 1 CTP",
            "Update 1 CTP2",
            "Update 1 Final CTP",
            "Update 1 RC",
            "Update 1",
            "Update 2 CTP",
            "Update 2 CTP2",
            "Update 2 CTP3",
            "Update 2 CTP4",
            "Update 2 RC",
            "Update 2",
            "Update 3 CTP",
            "Update 3 RC",
            "Update 3",
            "Update 4 RC",
            "Update 4 RC2",
            "Update 4 RC3",
            "Update 4 RC4",
            "Update 4",
            "Update 5",
            "March Pre-Release",
            "PDC 1998 Preview",
            "PDC 1998 Preview",
            "PDC 2000 Preview",
            "PDC 2003 Preview",
            "PDC 2008 Preview",
            "2004-Mar CTP",
            "2004-May CTP",
            "2004-Oct CTP",
            "2004-Dec CTP",
            "2005-Feb CTP",
            "2005-Jun CTP",
            "2005-Jul CTP",
            "2006-Dec CTP",
            "2007-Feb CTP",
            "2007-Jun CTP",
            "RC 2012-Jul Update",
            "RC 2012-Jul Update 2",
            "Compiler 2013-Nov CTP",
            "Update 1 SR1",
            "Update 1 SR2",
            "Update 1 SR3",
            "Update 3 SR2",
            "Update 3 SR4",
            "Update 3 VS15.0",
            "Update 3 VS15.3",
            "Update 3 VS15.6",
            "Update 3 VS15.7",
            "Update 3 VS16.0",
            "DDK 1998 Tools",
            "W2k Beta 3 DDK",
            "W2k 64Bit RC1 DDK",
            "W2k 64Bit RC2 DDK",
            "WXP Beta 1 DDK",
            "WXP Beta 2 DDK",
            "WXP DDK",
            "WXP IA64 DDK",
            "W2k3 Beta 3 DDK",
            "W2k3 RC1 DDK",
            "W2k3 RC2 PSDK",
            "W2k3 DDK",
            "W2k3 SP1 DDK",
            "W2k3 64Bit DDK",
            "W2k3 64Bit SP1 DDK",
            "WLH 4051 DDK",
            "WLH 4074 DDK",
            "WLH 5048 DDK",
            "WLH x64 Beta 1 DDK",
            "W8Mobile SDK",
            "W8Mobile SDK",
            "W8Mobile SDK",
            "W8.1Mobile Beta SDK",
            "W8 7729 WDK",
            "W8 7822 WDK",
            "W8 7915 WDK",
            "W8 7957 WDK",
            "XDK",
            "XDK",
            "XDK",
            "XDK",
            ".69",
            ".71"
        };
    }
}
