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
        private const int VS_1998_6_0 = 2;                 //VS 1998 6.0
        private const int MASM_6_13 = 3;                   //MASM 6.13
        private const int MASM_6_14 = 4;                   //MASM 6.14
        private const int VC_Tools_6_1 = 5;                //VC Tools 6.1
        private const int VC_Tools_6_20 = 6;               //VC Tools 6.20
        private const int VS_1998_6_0_Processor_Pack = 7;  //VS 1998 6.0 Processor Pack
        private const int MASM_6_15 = 8;                   //MASM 6.15
        private const int MASM_6_20 = 9;                   //MASM 6.20
        private const int VS_net_2002_7_0 = 10;            //VS.net 2002 7.0
        private const int VS_net_2003_7_1_PreRelease = 11; //VS.net 2003 7.1 PreRelease
        private const int VS_net_2003_7_1 = 12;            //VS.net 2003 7.1
        private const int VS_2005_8_0 = 13;                //VS 2005 8.0
        private const int Phoenix_PreRelease = 14;         //Phoenix PreRelease
        private const int VS_2008_9_0 = 15;                //VS 2008 9.0
        private const int Phoenix_10_0 = 16;               //Phoenix 10.0
        private const int VS_2010_10_0 = 17;               //VS 2010 10.0
        private const int VS_2010_10_1 = 18;               //VS 2010 10.1
        private const int VS_2012_11_0 = 19;               //VS 2012 11.0
        private const int VS_2013_12_0 = 20;               //VS 2013 12.0
        private const int VS_2013_12_1 = 21;               //VS 2013 12.1
        private const int VS_2015_14_0 = 22;               //VS 2015 14.0
        private const int eMbedded_VC2_10 = 23;            //eMbedded VC2.10
        private const int eMbedded_VC2_12 = 24;            //eMbedded VC2.12
        private const int eMbedded_VC3 = 25;               //eMbedded VC3
        private const int eMbedded_VC4 = 26;               //eMbedded VC4
        private const int WCE5_PlatformBuilder = 27;       //WCE5 PlatformBuilder
        private const int VS_2005_8_0_WCE = 28;            //VS 2005 8.0 WCE
        private const int VS_2005_8_0_Xbox360 = 29;        //VS 2005 8.0 Xbox360
        private const int WCE6_PlatformBuilder = 30;       //WCE6 PlatformBuilder
        private const int VS_2008_9_0_WCE = 31;            //VS 2008 9.0 WCE
        private const int VS_2008_9_0_Xbox360 = 32;        //VS 2008 9.0 Xbox360
        private const int VS_2008_9_1 = 33;                //VS 2008 9.1
        private const int VS_2010_10_0_Xbox360 = 34;       //VS 2010 10.0 Xbox360
        private const int VS_2010_10_10_Xbox360 = 35;      //VS 2010 10.10 Xbox360
        private const int VS_2017_15_0 = 36;               //VS 2017 15.0
        private const int VS_2017_15_3 = 37;               //VS 2017 15.3
        private const int VS_2017_15_4 = 38;               //VS 2017 15.4
        private const int VS_2017_15_5 = 39;               //VS 2017 15.5
        private const int VS_2017_15_6 = 40;               //VS 2017 15.6
        private const int VS_2017_15_7 = 41;               //VS 2017 15.7
        private const int VS_2017_15_8 = 42;               //VS 2017 15.8
        private const int VS_2017_15_9 = 43;               //VS 2017 15.9
        private const int VS_2019_16_0 = 44;               //VS 2019 16.0
        private const int VS_2019_16_1 = 45;               //VS 2019 16.1
        private const int VS_2019_16_2 = 46;               //VS 2019 16.2
        private const int VS_2019_16_3 = 47;               //VS 2019 16.3
        private const int VS_2019_16_4 = 48;               //VS 2019 16.4
        private const int VS_2019_16_5 = 49;               //VS 2019 16.5
        private const int VS_2019_16_6 = 50;               //VS 2019 16.6
        private const int VS_2019_16_7 = 51;               //VS 2019 16.7
        private const int VS_2019_16_8 = 52;               //VS 2019 16.8
        private const int VS_2019_16_9 = 53;               //VS 2019 16.9
        private const int VS_2019_16_10 = 54;              //VS 2019 16.10
        private const int VS_2019_16_11 = 55;              //VS 2019 16.11
        private const int VS_2022_17_0 = 56;               //VS 2022 17.0
        private const int VS_2022_17_1 = 57;               //VS 2022 17.1
        private const int VS_2022_17_2 = 58;               //VS 2022 17.2
        private const int VS_2022_17_3 = 59;               //VS 2022 17.3
        private const int VS_2022_17_4 = 60;               //VS 2022 17.4
        private const int VS_2022_17_5 = 61;               //VS 2022 17.5
        private const int VS_2022_17_6 = 62;               //VS 2022 17.6
        private const int VS_2022_17_7 = 63;               //VS 2022 17.7
        private const int VS_2022_17_8 = 64;               //VS 2022 17.8
        private const int VS_2022_17_9 = 65;               //VS 2022 17.9
        private const int VS_2022_17_10 = 66;              //VS 2022 17.10
        private const int VS_2022_17_11 = 67;              //VS 2022 17.11
        private const int VS_2022_17_12 = 68;              //VS 2022 17.12

        private static ReadOnlySpan<string> toolsetNames => new string[]
        {
            "VS 1997 5.0",
            "VS 1998 6.0",
            "MASM 6.13",
            "MASM 6.14",
            "VC Tools 6.1",
            "VC Tools 6.20",
            "VS 1998 6.0 Processor Pack",
            "MASM 6.15",
            "MASM 6.20",
            "VS.net 2002 7.0",
            "VS.net 2003 7.1 PreRelease",
            "VS.net 2003 7.1",
            "VS 2005 8.0",
            "Phoenix PreRelease",
            "VS 2008 9.0",
            "Phoenix 10.0",
            "VS 2010 10.0",
            "VS 2010 10.1",
            "VS 2012 11.0",
            "VS 2013 12.0",
            "VS 2013 12.1",
            "VS 2015 14.0",
            "eMbedded VC2.10",
            "eMbedded VC2.12",
            "eMbedded VC3",
            "eMbedded VC4",
            "WCE5 PlatformBuilder",
            "VS 2005 8.0 WCE",
            "VS 2005 8.0 Xbox360",
            "WCE6 PlatformBuilder",
            "VS 2008 9.0 WCE",
            "VS 2008 9.0 Xbox360",
            "VS 2008 9.1",
            "VS 2010 10.0 Xbox360",
            "VS 2010 10.10 Xbox360",
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
        };

        private const int None = 0;
        private const int RTM = 1;                   //RTM
        private const int PSDK = 2;                  //PSDK
        private const int Preview_1 = 3;             //Preview 1
        private const int Preview_2 = 4;             //Preview 2
        private const int Preview_3 = 5;             //Preview 3
        private const int Preview_4 = 6;             //Preview 4
        private const int Preview_5 = 7;             //Preview 5
        private const int Preview_6 = 8;             //Preview 6
        private const int Preview_7 = 9;             //Preview 7
        private const int Tech__Preview = 10;        //Tech. Preview
        private const int RC = 11;                   //RC
        private const int RC2 = 12;                  //RC2
        private const int RC3 = 13;                  //RC3
        private const int RC4 = 14;                  //RC4
        private const int CTP = 15;                  //CTP
        private const int CTP1 = 16;                 //CTP1
        private const int CTP2 = 17;                 //CTP2
        private const int CTP3 = 18;                 //CTP3
        private const int CTP4 = 19;                 //CTP4
        private const int CTP5 = 20;                 //CTP5
        private const int CTP6 = 21;                 //CTP6
        private const int SP1_Beta = 22;             //SP1 Beta
        private const int SP1 = 23;                  //SP1
        private const int SP2 = 24;                  //SP2
        private const int SP3 = 25;                  //SP3
        private const int SP4 = 26;                  //SP4
        private const int SP5 = 27;                  //SP5
        private const int SP6 = 28;                  //SP6
        private const int Alpha = 29;                //Alpha
        private const int Beta = 30;                 //Beta
        private const int Beta_Update = 31;          //Beta Update
        private const int Beta_2 = 32;               //Beta 2
        private const int Beta_Tools_Refresh = 33;   //Beta Tools Refresh
        private const int Free_Toolkit = 34;         //Free Toolkit
        private const int Feature_Pack = 35;         //Feature Pack
        private const int Feature_Pack_Refresh = 36; //Feature Pack Refresh
        private const int Developer_Preview = 37;    //Developer Preview
        private const int Rollup = 38;               //Rollup
        private const int Upd_1_CTP = 39;            //Upd 1 CTP
        private const int Upd_1_Final_CTP = 40;      //Upd 1 Final CTP
        private const int Upd_1_RC = 41;             //Upd 1 RC
        private const int Upd_1 = 42;                //Upd 1
        private const int Upd_2_CTP = 43;            //Upd 2 CTP
        private const int Upd_2_CTP2 = 44;           //Upd 2 CTP2
        private const int Upd_2_CTP3 = 45;           //Upd 2 CTP3
        private const int Upd_2_CTP4 = 46;           //Upd 2 CTP4
        private const int Upd_2_RC = 47;             //Upd 2 RC
        private const int Upd_2 = 48;                //Upd 2
        private const int Upd_3_CTP = 49;            //Upd 3 CTP
        private const int Upd_3_RC = 50;             //Upd 3 RC
        private const int Upd_3 = 51;                //Upd 3
        private const int Upd_4_RC = 52;             //Upd 4 RC
        private const int Upd_4_RC3 = 53;            //Upd 4 RC3
        private const int Upd_4_RC4 = 54;            //Upd 4 RC4
        private const int Upd_4 = 55;                //Upd 4
        private const int Upd_5 = 56;                //Upd 5
        private const int PDC1998_Preview = 57;      //PDC1998 Preview
        private const int PDC2000_Preview = 58;      //PDC2000 Preview
        private const int PDC2003_Preview = 59;      //PDC2003 Preview
        private const int _2004Mar_CTP = 60;         //2004Mar CTP
        private const int _2004May_CTP = 61;         //2004May CTP
        private const int _2004Oct_CTP = 62;         //2004Oct CTP
        private const int _2004Dec_CTP = 63;         //2004Dec CTP
        private const int _2005Feb_CTP = 64;         //2005Feb CTP
        private const int _2005Jun_CTP = 65;         //2005Jun CTP
        private const int _2005Jul_CTP = 66;         //2005Jul CTP
        private const int RC_2012Jul_Upd = 67;       //RC 2012Jul Upd
        private const int RC_2012Jul_Upd2 = 68;      //RC 2012Jul Upd2
        private const int Compiler_2013Nov_CTP = 69; //Compiler 2013Nov CTP
        private const int Upd_1_SR1 = 70;            //Upd 1 SR1
        private const int Upd_1_SR2 = 71;            //Upd 1 SR2
        private const int Upd_1_SR3 = 72;            //Upd 1 SR3
        private const int Upd_3_SR2 = 73;            //Upd 3 SR2
        private const int Upd_3_SR4 = 74;            //Upd 3 SR4
        private const int Upd_3_VS15_0 = 75;         //Upd 3 VS15.0
        private const int Upd_3_VS15_3 = 76;         //Upd 3 VS15.3
        private const int Upd_3_VS15_6 = 77;         //Upd 3 VS15.6
        private const int Upd_3_VS15_7 = 78;         //Upd 3 VS15.7
        private const int Upd_3_VS16_0 = 79;         //Upd 3 VS16.0
        private const int ILC = 80;                  //ILC
        private const int DDK_1998_Tools = 81;       //DDK 1998 Tools
        private const int W2k_Beta_3_DDK = 82;       //W2k Beta 3 DDK
        private const int W2k_64Bit_RC1_DDK = 83;    //W2k 64Bit RC1 DDK
        private const int W2k_64Bit_RC2_DDK = 84;    //W2k 64Bit RC2 DDK
        private const int WXP_Beta_1_DDK = 85;       //WXP Beta 1 DDK
        private const int WXP_Beta_2_DDK = 86;       //WXP Beta 2 DDK
        private const int WXP_DDK = 87;              //WXP DDK
        private const int WXP_IA64_DDK = 88;         //WXP IA64 DDK
        private const int W2k3_Beta_3_DDK = 89;      //W2k3 Beta 3 DDK
        private const int W2k3_RC1_DDK = 90;         //W2k3 RC1 DDK
        private const int W2k3_DDK = 91;             //W2k3 DDK
        private const int W2k3_SP1_DDK = 92;         //W2k3 SP1 DDK
        private const int W2k3_64Bit_DDK = 93;       //W2k3 64Bit DDK
        private const int W2k3_64Bit_SP1_DDK = 94;   //W2k3 64Bit SP1 DDK
        private const int WLH_b4051_DDK = 95;        //WLH b4051 DDK
        private const int WLH_b4074_DDK = 96;        //WLH b4074 DDK
        private const int WLH_b5048_DDK = 97;        //WLH b5048 DDK
        private const int WLH_x64_Beta_1_DDK = 98;   //WLH x64 Beta 1 DDK
        private const int XDK = 99;                  //XDK

        //Along with an index into the toolsetNames array, each toolset descriptor
        //also stores the build ID of the toolset (not to be confused with the build
        //ID of the tool that is found in the PRODITEM) or an index into this array to get
        //the type of toolset release that the tool was found in
        private static ReadOnlySpan<string> toolsetReleaseTypes => new string[]
        {
            "RTM",
            "PSDK",
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
            "Rollup",
            "Upd 1 CTP",
            "Upd 1 Final CTP",
            "Upd 1 RC",
            "Upd 1",
            "Upd 2 CTP",
            "Upd 2 CTP2",
            "Upd 2 CTP3",
            "Upd 2 CTP4",
            "Upd 2 RC",
            "Upd 2",
            "Upd 3 CTP",
            "Upd 3 RC",
            "Upd 3",
            "Upd 4 RC",
            "Upd 4 RC3",
            "Upd 4 RC4",
            "Upd 4",
            "Upd 5",
            "PDC1998 Preview",
            "PDC2000 Preview",
            "PDC2003 Preview",
            "2004Mar CTP",
            "2004May CTP",
            "2004Oct CTP",
            "2004Dec CTP",
            "2005Feb CTP",
            "2005Jun CTP",
            "2005Jul CTP",
            "RC 2012Jul Upd",
            "RC 2012Jul Upd2",
            "Compiler 2013Nov CTP",
            "Upd 1 SR1",
            "Upd 1 SR2",
            "Upd 1 SR3",
            "Upd 3 SR2",
            "Upd 3 SR4",
            "Upd 3 VS15.0",
            "Upd 3 VS15.3",
            "Upd 3 VS15.6",
            "Upd 3 VS15.7",
            "Upd 3 VS16.0",
            "ILC",
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
            "W2k3 DDK",
            "W2k3 SP1 DDK",
            "W2k3 64Bit DDK",
            "W2k3 64Bit SP1 DDK",
            "WLH b4051 DDK",
            "WLH b4074 DDK",
            "WLH b5048 DDK",
            "WLH x64 Beta 1 DDK",
            "XDK"
        };
    }
}
