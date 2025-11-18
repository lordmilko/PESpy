namespace PESpy
{
    public enum Toolchain
    {
        Unknown,

        #region 16-bit

        //LINK 3.51
        MicrosoftC4_0,

        //LINK 3.61
        MicrosoftC_5_0,

        //LINK 5.05
        BasicPDS7_0,

        //LINK 5.10
        MicrosoftC6_0,

        //LINK 5.15
        QuickC1_0,

        //LINK 5.30, ILLINK 1.30
        MicrosoftC7_0,

        //LINK 5.50
        VisualCPP1_0,

        //LINK 5.60,
        VisualCPP1_52,

        #endregion
        #region 32-bit

        //LINK 2.50
        VisualCPP2_0,

        //LINK 3.00
        VisualCPP4_0,

        //Part of Visual Studio 97. In Visual Studio 97 SP3
        //rich header information started being emitted
        //LINK 5.00
        VisualCPP5_0,

        //Visual Studio 98
        //LINK 6.00
        VisualCPP6_0,

        //LINK 7.00
        VS2002,

        //LINK 7.10
        VS2003,

        //LINK 8.00
        VS2005,

        //LINK 9.00
        VS2008,

        //LINK 10.00
        VS2010,

        Phoenix,

        //LINK 11.00
        VS2012,

        //LINK 12.00
        VS2013,

        //LINK 14.00
        VS2015,

        #region VS2017

        //LINK 14.10
        VS2017_15_0, //15.0-15.2

        //LINK 14.11
        VS2017_15_3,

        //LINK 14.12
        VS2017_15_5,

        //LINK 14.13
        VS2017_15_6,

        //LINK 14.14
        VS2017_15_7,

        //LINK 14.15
        VS2017_15_8,

        //LINK 14.16
        VS2017_15_9,

        #endregion
        #region VS2019

        //LINK 14.20
        VS2019_16_0,

        //LINK 14.21
        VS2019_16_1,

        //LINK 14.22
        VS2019_16_2,

        //LINK 14.23
        VS2019_16_3,

        //LINK 14.24
        VS2019_16_4,

        //LINK 14.25
        VS2019_16_5,

        //LINK 14.26
        VS2019_16_6,

        //LINK 14.27
        VS2019_16_7,

        //LINK 14.27
        VS2019_16_8,

        //LINK 14.28
        VS2019_16_9,

        //LINK 14.28
        VS2019_16_10,

        //LINK 14.29
        VS2019_16_11,

        #endregion
        #region VS2022

        //LINK 14.30,
        VS2022_17_0,

        //LINK 14.31
        VS2022_17_1,

        //LINK 14.32
        VS2022_17_2,

        //LINK 14.33
        VS2022_17_3,

        //LINK 14.34
        VS2022_17_4,

        //LINK 14.35
        VS2022_17_5,

        //LINK 14.36
        VS2022_17_6,

        //LINK 14.37
        VS2022_17_7,

        //LINK 14.38
        VS2022_17_8,

        //LINK 14.39
        VS2022_17_9,

        //I have VS2022 17.9 and 17.14, so I've just tried to fill in the gaps

        //LINK 14.40
        VS2022_17_10,

        //LINK 14.41
        VS2022_17_11,

        //LINK 14.42
        VS2022_17_12,

        //LINK 14.43
        VS2022_17_13,

        //LINK 14.44
        VS2022_17_14,

        #endregion
        #endregion
    }
}
