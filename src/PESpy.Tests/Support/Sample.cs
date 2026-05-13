using System;
using System.Collections.Generic;
using System.IO;

namespace PESpy.Tests
{
    public static class Sample
    {
        #region File Types

        public static string DNRB => C400_EXE;
        public static string NB00 => C500_EXE;
        public static string NB01 => BC7_EXE;
        public static string NB02 => C600_Symbols_EXE;
        public static string NB05 => C700_Unpacked_EXE;
        public static string NB07 => QCWIN_EXE;
        public static string NB08 => C700_Packed_EXE;
        public static string NB09_NE => VC152_EXE;
        public static string NB09_PE => VC40_VXD_EXE;
        public static string NB10 => VC40_EXE;
        public static string NB11 => VC50_EXE;

        public static string NE => VC152_EXE;

        public static string EXE_C6 => C600_Symbols_EXE; //NB02. C6 has a completely different symbol format than C7

        public static string OBJ_C6 => throw new NotImplementedException();
        public static string OBJ_C7 => VC40_OBJ;
        public static string OBJ_C11 => VC50_OBJ;
        public static string OBJ_C13 => VS22_OBJ; //Classic OBJ

        #endregion
        #region C
        #region Microsoft C 4.0 (1986)

        /// <summary>
        /// Unknown DNRB signature
        /// </summary>
        public static readonly string C400_EXE;

        public static readonly string C400_MAP;
        public static readonly string C400_SYM;
        public static readonly string C400_OBJ;

        #endregion
        #region Microsoft C 5.0 (1987)

        /// <summary>
        /// NB00
        /// </summary>
        public static readonly string C500_EXE;

        public static readonly string C500_MAP;
        public static readonly string C500_OBJ;

        #endregion
        #region Microsoft C 6.0 (1990)

        public static readonly string C600_NoSymbols_EXE;
        public static readonly string C600_NoSymbols_OBJ;

        /// <summary>
        /// NB02
        /// </summary>
        public static readonly string C600_Symbols_EXE; //C6

        public static readonly string C600_Symbols_OBJ; //Not a COFF file

        public static readonly string C600_Tiny_COM;
        public static readonly string C600_Tiny_DBG;

        #endregion
        #region Microsoft C/C++ 7.0 (1992)

        /// <summary>
        /// NB08
        /// </summary>
        public static readonly string C700_Packed_EXE;

        public static readonly string C700_Packed_OBJ; //Not a COFF file

        /// <summary>
        /// NB05
        /// </summary>
        public static readonly string C700_Unpacked_EXE;

        public static readonly string C700_Unpacked_OBJ;

        #endregion
        #endregion
        #region MASM 5 (1988)

        public static readonly string MASM5_NB00_VXD;

        #endregion
        #region Basic PDS 7.0 (1989)

        /// <summary>
        /// NB01
        /// </summary>
        public static readonly string BC7_EXE;

        public static readonly string BC7_OBJ;

        #endregion
        #region QuickC for Windows 1.0 (1991)

        /// <summary>
        /// NB07
        /// </summary>
        public static readonly string QCWIN_EXE;

        public static readonly string QCWIN_OBJ; //Not a COFF file

        #endregion
        #region Visual Basic
        #region Visual Basic 1

        #endregion
        #region Visual Basic 2

        #endregion
        #region Visual Basic 3 (1993)

        #endregion
        #region Visual Basic 4 (1995)

        #endregion
        #region Visual Basic 5 (1997)

        #endregion
        #region Visual Basic 6 (1998)

        #endregion
        #endregion
        #region Visual C++
        #region Visual C++ 1.52 (1993)

        /// <summary>
        /// Visual C++ 1.52 EXE New Executable<para/>
        /// NB09
        /// </summary>
        public static readonly string VC152_EXE;

        public static readonly string VC152_PDB;

        #endregion
        #region Visual C++ 2.0

        public static readonly string VC20_EXE;

        public static readonly string VC20_OBJ; //C7
        public static readonly string VC20_PDB;

        #endregion
        #region Visual C++ 4.0 (1995)

        /// <summary>
        /// NB10
        /// </summary>
        public static readonly string VC40_EXE;

        public static readonly string VC40_OBJ; //C7
        public static readonly string VC40_PDB;

        /// <summary>
        /// PE EXE used to control VXD driver.<para/>
        /// NB09
        /// </summary>
        public static readonly string VC40_VXD_EXE;

        /// <summary>
        /// VXD
        /// </summary>
        public static readonly string VC40_LE;

        #endregion
        #region Visual C++ 5.0 (1997)

        /// <summary>
        /// NB11
        /// </summary>
        public static readonly string VC50_EXE;

        public static readonly string VC50_OBJ; //C11

        //From a separate build to the VC50_EXE; the VC50_EXE contains NB11 symbols without an associated PDB
        public static readonly string VC50_PDB;

        #endregion
        #region Visual C++ 6.0 (1998)

        //Misc pointing to Dbg which has NB10
        public static readonly string VC60_EXE;
        public static readonly string VC60_PDB;
        public static readonly string VC60_DBG;

        public static readonly string VC60_Coff_EXE;

        #endregion
        #endregion
        #region Visual Studio
        #region Visual Studio .NET (2002)

        #endregion
        #region Visual Studio 2022 (2022)

        /// <summary>
        /// Visual Studio 2022 Portable Executable<para/>
        /// RSDS
        /// </summary>
        public static readonly string VS22_EXE;
        public static readonly string VS22_EXP;
        public static readonly string VS22_LIB;
        public static readonly string VS22_OBJ; //C13
        public static readonly string VS22_PDB;

        public static readonly string VS22_LTCG_EXE;
        public static readonly string VS22_LTCG_EXP;
        public static readonly string VS22_LTCG_LIB;
        public static readonly string VS22_LTCG_OBJ;
        public static readonly string VS22_LTCG_PDB;

        public static readonly string VS22_Debug_EXE;
        public static readonly string VS22_Debug_PDB;

        #endregion
        #endregion
        #region CLR

        public static readonly string Framework_EXE;
        public static readonly string Framework_PDB;

        public static readonly string Interop_EXE;
        public static readonly string Interop_PDB;

        public static readonly string Interop_Core_DLL;
        public static readonly string Interop_Core_PDB;

        public static readonly string MPDB_DLL;

        public static readonly string NativeAOT_EXE;
        public static readonly string NativeAOT_PDB;

        public static readonly string NGEN_DLL;
        public static readonly string NGEN_NI_DLL;
        public static readonly string NGEN_PDB;
        public static readonly string NGEN_NI_PDB;

        public static readonly string R2R_EXE;
        public static readonly string R2R_DLL;
        public static readonly string R2R_PDB;

        public static readonly string SingleFileApp_EXE;
        public static readonly string SingleFileApp_PDB;

        #endregion

        static Sample()
        {
            var sampleDir = Path.GetFullPath(Path.Combine((typeof(Sample).Assembly.Location), "..\\..\\..\\..\\..\\..\\samples"));

            string MakePath(string relativePath)
            {
                var path = Path.Combine(sampleDir, relativePath);

                if (!File.Exists(path))
                    throw new FileNotFoundException($"File '{path}' does not exist");

                return path;
            }

            #region C
            #region Microsoft C 4.0 (1986)

            C400_EXE = MakePath("C\\C400 (1986)\\TESTAPP.EXE");
            C400_MAP = MakePath("C\\C400 (1986)\\TESTAPP.MAP");
            C400_SYM = MakePath("C\\C400 (1986)\\LineNumbers\\TESTAPP.SYM");
            C400_OBJ = MakePath("C\\C400 (1986)\\TESTAPP.OBJ");

            #endregion
            #region Microsoft C 5.0 (1987)

            C500_EXE = MakePath("C\\C500 (1987)\\TESTAPP.EXE");
            C500_MAP = MakePath("C\\C500 (1987)\\TESTAPP.MAP");
            C500_OBJ = MakePath("C\\C500 (1987)\\TESTAPP.OBJ");

            #endregion
            #region Microsoft C 6.0 (1990)

            C600_NoSymbols_EXE = MakePath("C\\C600 (1990)\\NoSymbols\\TESTAPP.EXE");
            C600_NoSymbols_OBJ = MakePath("C\\C600 (1990)\\NoSymbols\\TESTAPP.OBJ");

            C600_Symbols_EXE = MakePath("C\\C600 (1990)\\Symbols\\TESTAPP.EXE");
            C600_Symbols_OBJ = MakePath("C\\C600 (1990)\\Symbols\\TESTAPP.OBJ");

            C600_Tiny_COM = MakePath("C\\C600 (1990)\\Tiny\\TESTAPP.COM");
            C600_Tiny_DBG = MakePath("C\\C600 (1990)\\Tiny\\TESTAPP.DBG");

            #endregion
            #region Microsoft C/C++ 7.0 (1992)

            C700_Packed_EXE = MakePath("C\\C700 (1992)\\Packed\\TESTAPP.EXE");
            C700_Packed_OBJ = MakePath("C\\C700 (1992)\\Packed\\TESTAPP.OBJ");

            C700_Unpacked_EXE = MakePath("C\\C700 (1992)\\Unpacked\\TESTAPP.EXE");
            C700_Unpacked_OBJ = MakePath("C\\C700 (1992)\\Unpacked\\TESTAPP.OBJ");

            #endregion
            #endregion
            #region MASM 5 (1988)

            MASM5_NB00_VXD = MakePath("MASM\\masm5 (1988)\\NB00_VXD\\EBIOS.386");

            #endregion
            #region Basic PDS 7.0 (1989)

            BC7_EXE = MakePath("BASIC\\bc7 (1989)\\TESTAPP.EXE");
            BC7_OBJ = MakePath("BASIC\\bc7 (1989)\\TESTAPP.OBj");

            #endregion
            #region QuickC for Windows 1.0 (1991)

            QCWIN_EXE = MakePath("QuickC\\qcwin (1991)\\TESTAPP.EXE");
            QCWIN_OBJ = MakePath("QuickC\\qcwin (1991)\\TESTAPP.OBJ");

            #endregion
            #region Visual Basic
            #region Visual Basic 1 (1991)

            #endregion
            #region Visual Basic 2 (1992)

            #endregion
            #region Visual Basic 3 (1993)

            #endregion
            #region Visual Basic 4 (1995)

            #endregion
            #region Visual Studio 5 (1997)

            #endregion
            #region Visual Basic 6 (1998)

            #endregion
            #endregion
            #region Visual C++
            #region Visual C++ 1.52 (1993)

            VC152_EXE = MakePath("VC\\vc152 (1993)\\TESTAPP.EXE");
            VC152_PDB = MakePath("VC\\vc152 (1993)\\TESTAPP.PDB");

            #endregion
            #region Visual C++ 2.0 (1994)

            VC20_EXE = MakePath("VC\\vc20 (1994)\\TestApp.exe");
            VC20_OBJ = MakePath("VC\\vc20 (1994)\\main.obj");
            VC20_PDB = MakePath("VC\\vc20 (1994)\\TestApp.pdb");

            #endregion
            #region Visual C++ 4.0 (1995)

            VC40_EXE = MakePath("VC\\vc40 (1995)\\Normal\\TestApp.exe");
            VC40_OBJ = MakePath("VC\\vc40 (1995)\\Normal\\main.obj");
            VC40_PDB = MakePath("VC\\vc40 (1995)\\Normal\\TestApp.pdb");

            VC40_VXD_EXE = MakePath("VC\\vc40 (1995)\\CVXD32\\con_samp.exe");
            VC40_LE = MakePath("VC\\vc40 (1995)\\CVXD32\\cvxdsamp.vxd");

            #endregion
            #region Visual C++ 5.0 (1997)

            VC50_EXE = MakePath("VC\\vc50 (1997)\\TestApp.exe");
            VC50_OBJ = MakePath("VC\\vc50 (1997)\\main.obj");
            VC50_PDB = MakePath("VC\\vc50 (1997)\\TestApp.pdb");

            #endregion
            #region Visual C++ 6.0 (1998)

            VC60_EXE = MakePath("VC\\vc60 (1998)\\CoffAndPdbSymbols_PostSplit\\TestApp.exe");
            VC60_PDB = MakePath("VC\\vc60 (1998)\\CoffAndPdbSymbols_PostSplit\\TestApp.pdb");
            VC60_DBG = MakePath("VC\\vc60 (1998)\\CoffAndPdbSymbols_PostSplit\\TestApp.dbg");

            VC60_Coff_EXE = MakePath("VC\\vc60 (1998)\\CoffSymbolsOnly\\TestApp.exe");

            #endregion
            #endregion
            #region Visual Studio
            #region Visual Studio .NET 2002 (2002)

            #endregion
            #region Visual Studio 2022 (2022)

            VS22_EXE = MakePath("VS\\vs22 (2022)\\Normal\\TestApp.exe");
            VS22_EXP = MakePath("VS\\vs22 (2022)\\Normal\\TestApp.exp"); //Exports (relating to the lib file)
            VS22_LIB = MakePath("VS\\vs22 (2022)\\Normal\\TestApp.lib");
            VS22_OBJ = MakePath("VS\\vs22 (2022)\\Normal\\TestApp.obj");
            VS22_PDB = MakePath("VS\\vs22 (2022)\\Normal\\TestApp.pdb");

            VS22_LTCG_EXE = MakePath("VS\\vs22 (2022)\\LTCG\\TestApp.exe");
            VS22_LTCG_EXP = MakePath("VS\\vs22 (2022)\\LTCG\\TestApp.exp");
            VS22_LTCG_LIB = MakePath("VS\\vs22 (2022)\\LTCG\\TestApp.lib");
            VS22_LTCG_OBJ = MakePath("VS\\vs22 (2022)\\LTCG\\TestApp.obj");
            VS22_LTCG_PDB = MakePath("VS\\vs22 (2022)\\LTCG\\TestApp.pdb");

            VS22_Debug_EXE = MakePath("VS\\vs22 (2022)\\Debug\\TestApp.exe");
            VS22_Debug_PDB = MakePath("VS\\vs22 (2022)\\Debug\\TestApp.pdb");

            #endregion
            #endregion
            #region CLR

            Framework_EXE = MakePath("CLR\\framework\\TestApp.exe");
            Framework_PDB     = MakePath("CLR\\framework\\TestApp.pdb");

            Interop_EXE       = MakePath("CLR\\interop\\TestApp.exe");
            Interop_PDB       = MakePath("CLR\\interop\\TestApp.pdb");

            Interop_Core_DLL  = MakePath("CLR\\interop-core\\TestLib.dll");
            Interop_Core_PDB  = MakePath("CLR\\interop-core\\TestLib.pdb");

            MPDB_DLL          = MakePath("CLR\\MPDB\\TestLib.dll");

            NativeAOT_EXE     = MakePath("CLR\\NativeAOT\\TestApp.exe");
            NativeAOT_PDB     = MakePath("CLR\\NativeAOT\\TestApp.pdb");

            NGEN_DLL          = MakePath("CLR\\NGEN\\TestLib.dll");
            NGEN_NI_DLL       = MakePath("CLR\\NGEN\\TestLib.ni.dll");
            NGEN_PDB          = MakePath("CLR\\NGEN\\TestLib.pdb");
            NGEN_NI_PDB       = MakePath("CLR\\NGEN\\TestLib.ni.pdb");

            R2R_EXE           = MakePath("CLR\\R2R\\TestApp.exe");
            R2R_DLL           = MakePath("CLR\\R2R\\TestApp.dll");
            R2R_PDB           = MakePath("CLR\\R2R\\TestApp.pdb");

            SingleFileApp_EXE = MakePath("CLR\\SingleFileApp\\TestApp.exe");
            SingleFileApp_PDB = MakePath("CLR\\SingleFileApp\\TestApp.pdb");

            #endregion
        }

        public static string[] PDBs
        {
            get
            {
                return new[]
                {
                    VC152_PDB,
                    VC20_PDB,
                    VC40_PDB,
                    VC50_PDB,
                    VC60_PDB,
                    VS22_PDB,
                    VS22_LTCG_PDB,
                    VS22_Debug_PDB,
                    Framework_PDB,
                    Interop_PDB,
                    Interop_Core_PDB,
                    NativeAOT_PDB,
                    NGEN_PDB,
                    NGEN_NI_PDB,
                    //R2R_PDB,
                    //SingleFileApp_PDB
                };
            }
        }
    }
}
