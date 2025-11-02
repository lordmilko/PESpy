namespace PESpy
{
    public struct LinkerVersion
    {
        public int MajorVersion { get; }

        public int MinorVersion { get; }

        public bool Is16Bit { get; }

        public LinkerVersion(int majorVersion, int minorVersion, bool is16Bit = false)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            Is16Bit = is16Bit;
        }

        public Toolchain Kind
        {
            get
            {
                if (Is16Bit)
                {
                    return MajorVersion switch
                    {
                        3 => MinorVersion switch
                        {
                            51 => Toolchain.MicrosoftC4_0,
                            61 => Toolchain.MicrosoftC_5_0,
                            _ => Toolchain.Unknown
                        },
                        5 => MinorVersion switch
                        {
                            5 => Toolchain.BasicPDS7_0,
                            10 => Toolchain.MicrosoftC6_0,
                            15 => Toolchain.QuickC1_0,
                            30 => Toolchain.MicrosoftC7_0,
                            50 => Toolchain.VisualCPP1_0,
                            60 => Toolchain.VisualCPP1_52,
                            _ => Toolchain.Unknown
                        },
                        _ => Toolchain.Unknown
                    };
                }
                else
                {
                    return MajorVersion switch
                    {
                        2 => MinorVersion switch
                        {
                            50 => Toolchain.VisualCPP2_0,
                            _ => Toolchain.Unknown
                        },
                        3 => MinorVersion switch
                        {
                            0 => Toolchain.VisualCPP4_0,
                            _ => Toolchain.Unknown
                        },
                        5 => MinorVersion switch
                        {
                            //VS97 bundled the various separate tools together. Visual C++ 5 was included
                            //in VS97
                            0 => Toolchain.VisualCPP5_0,
                            _ => Toolchain.Unknown
                        },
                        6 => MinorVersion switch
                        {
                            0 => Toolchain.VisualCPP6_0,
                            _ => Toolchain.Unknown
                        },
                        7 => MinorVersion switch
                        {
                            0 => Toolchain.VS2002,
                            10 => Toolchain.VS2003,
                            _ => Toolchain.Unknown
                        },
                        8 => MinorVersion switch
                        {
                            0 => Toolchain.VS2005,
                            _ => Toolchain.Unknown
                        },
                        9 => MinorVersion switch
                        {
                            0 => Toolchain.VS2008,
                            _ => Toolchain.Unknown
                        },
                        10 => MinorVersion switch
                        {
                            0 => Toolchain.VS2010,
                            _ => Toolchain.Unknown
                        },
                        11 => MinorVersion switch
                        {
                            0 => Toolchain.VS2012,
                            _ => Toolchain.Unknown
                        },
                        12 => MinorVersion switch
                        {
                            0 => Toolchain.VS2013,
                            _ => Toolchain.Unknown
                        },
                        14 => MinorVersion switch
                        {
                            0 => Toolchain.VS2015,

                            #region VS2017

                            10 => Toolchain.VS2017_15_0,
                            11 => Toolchain.VS2017_15_3,
                            12 => Toolchain.VS2017_15_5,
                            13 => Toolchain.VS2017_15_6,
                            14 => Toolchain.VS2017_15_7,
                            15 => Toolchain.VS2017_15_8,
                            16 => Toolchain.VS2017_15_9,

                            #endregion
                            #region VS2019

                            20 => Toolchain.VS2019_16_0,
                            21 => Toolchain.VS2019_16_1,
                            22 => Toolchain.VS2019_16_2,
                            23 => Toolchain.VS2019_16_3,
                            24 => Toolchain.VS2019_16_4,
                            25 => Toolchain.VS2019_16_5,
                            26 => Toolchain.VS2019_16_6,
                            27 => Toolchain.VS2019_16_7,
                            28 => Toolchain.VS2019_16_9,
                            29 => Toolchain.VS2019_16_11,

                            #endregion
                            #region VS2022

                            30 => Toolchain.VS2022_17_0,
                            31 => Toolchain.VS2022_17_1,
                            32 => Toolchain.VS2022_17_2,
                            33 => Toolchain.VS2022_17_3,
                            34 => Toolchain.VS2022_17_4,
                            35 => Toolchain.VS2022_17_5,
                            36 => Toolchain.VS2022_17_6,
                            37 => Toolchain.VS2022_17_7,
                            38 => Toolchain.VS2022_17_8,
                            39 => Toolchain.VS2022_17_9,
                            40 => Toolchain.VS2022_17_10,
                            41 => Toolchain.VS2022_17_11,
                            42 => Toolchain.VS2022_17_12,
                            43 => Toolchain.VS2022_17_13,
                            44 => Toolchain.VS2022_17_14,

                            #endregion

                            _ => Toolchain.Unknown
                        },
                        _ => Toolchain.Unknown
                    };
                }
            }
        }

        public override string ToString()
        {
            using var builder = new ValueStringBuilder();

            var str = Kind switch
            {
                #region 16-bit

                Toolchain.MicrosoftC4_0  => "Microsoft C 4.0 (1986)",
                Toolchain.MicrosoftC_5_0 => "Microsoft C 5.0 (1987)",
                Toolchain.BasicPDS7_0    => "Basic PDS 7.0 (1989)",
                Toolchain.MicrosoftC6_0  => "Microsoft C 6.0 (1989)",
                Toolchain.QuickC1_0      => "QuickC for Windows 1.0 (1991)",
                Toolchain.MicrosoftC7_0  => "Microsoft C/C++ 7.0 (1992)",
                Toolchain.VisualCPP1_0   => "Visual C++ 1.0 (1993)",
                Toolchain.VisualCPP1_52  => "Visual C++ 1.52 (1993)",

                #endregion

                Toolchain.VisualCPP2_0 => "Visual C++ 2.0 (1994)",
                Toolchain.VisualCPP4_0 => "Visual C++ 4.0 (1995)",
                Toolchain.VisualCPP5_0 => "Visual C++ 5.0 (1997)",
                Toolchain.VisualCPP6_0 => "Visual C++ 6.0 (1998)",
                Toolchain.VS2002 => "Visual Studio 2002",
                Toolchain.VS2003 => "Visual Studio 2003",
                Toolchain.VS2005 => "Visual Studio 2005",
                Toolchain.VS2008 => "Visual Studio 2008",
                Toolchain.VS2010 => "Visual Studio 2010",
                Toolchain.VS2012 => "Visual Studio 2012",
                Toolchain.VS2013 => "Visual Studio 2013",
                Toolchain.VS2015 => "Visual Studio 2015",

                #region VS2017

                Toolchain.VS2017_15_0 => "Visual Studio 2017 (15.0-15.2)",
                Toolchain.VS2017_15_3 => "Visual Studio 2017 (15.3)",
                Toolchain.VS2017_15_5 => "Visual Studio 2017 (15.5)",
                Toolchain.VS2017_15_6 => "Visual Studio 2017 (15.6)",
                Toolchain.VS2017_15_7 => "Visual Studio 2017 (15.7)",
                Toolchain.VS2017_15_8 => "Visual Studio 2017 (15.8)",
                Toolchain.VS2017_15_9 => "Visual Studio 2017 (15.9)",

                #endregion
                #region VS2019

                Toolchain.VS2019_16_0 => "Visual Studio 2019 (16.0)",
                Toolchain.VS2019_16_1 => "Visual Studio 2019 (16.1)",
                Toolchain.VS2019_16_2 => "Visual Studio 2019 (16.2)",
                Toolchain.VS2019_16_3 => "Visual Studio 2019 (16.3)",
                Toolchain.VS2019_16_4 => "Visual Studio 2019 (16.4)",
                Toolchain.VS2019_16_5 => "Visual Studio 2019 (16.5)",
                Toolchain.VS2019_16_6 => "Visual Studio 2019 (16.6)",
                Toolchain.VS2019_16_7 => "Visual Studio 2019 (16.7-16.8)",
                Toolchain.VS2019_16_9 => "Visual Studio 2019 (16.9-16.10)",
                Toolchain.VS2019_16_11 => "Visual Studio 2019 (16.11)",

                #endregion
                #region VS2022

                Toolchain.VS2022_17_0 => "Visual Studio 2022 (17.0)",
                Toolchain.VS2022_17_1 => "Visual Studio 2022 (17.1)",
                Toolchain.VS2022_17_2 => "Visual Studio 2022 (17.2)",
                Toolchain.VS2022_17_3 => "Visual Studio 2022 (17.3)",
                Toolchain.VS2022_17_4 => "Visual Studio 2022 (17.4)",
                Toolchain.VS2022_17_5 => "Visual Studio 2022 (17.5)",
                Toolchain.VS2022_17_6 => "Visual Studio 2022 (17.6)",
                Toolchain.VS2022_17_7 => "Visual Studio 2022 (17.7)",
                Toolchain.VS2022_17_8 => "Visual Studio 2022 (17.8)",
                Toolchain.VS2022_17_9 => "Visual Studio 2022 (17.9)",
                Toolchain.VS2022_17_10 => "Visual Studio 2022 (17.10)",
                Toolchain.VS2022_17_11 => "Visual Studio 2022 (17.11)",
                Toolchain.VS2022_17_12 => "Visual Studio 2022 (17.12)",
                Toolchain.VS2022_17_13 => "Visual Studio 2022 (17.13)",
                Toolchain.VS2022_17_14 => "Visual Studio 2022 (17.14)",

                #endregion

                Toolchain.Unknown => "Unknown"
            };

            builder.Append(str);
            builder.Append(" (");
            builder.Append(MajorVersion);
            builder.Append('.');

            if (MinorVersion < 10)
                builder.Append('0');

            builder.Append(MinorVersion);

            builder.Append(')');

            return builder.ToString();
        }
    }
}
