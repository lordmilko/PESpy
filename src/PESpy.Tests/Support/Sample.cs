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

        #region 1. Microsoft C 4.0

        /// <summary>
        /// Unknown DNRB signature
        /// </summary>
        public static readonly string C400_EXE;

        public static readonly string C400_MAP;
        public static readonly string C400_OBJ;

        #endregion
        #region 2. Microsoft C 5.0

        /// <summary>
        /// NB00
        /// </summary>
        public static readonly string C500_EXE;

        public static readonly string C500_MAP;
        public static readonly string C500_OBJ;

        #endregion
        #region 3. MASM 5

        public static readonly string MASM5_NB00_VXD;

        #endregion
        #region 4. Basic PDS 7.0

        /// <summary>
        /// NB01
        /// </summary>
        public static readonly string BC7_EXE;

        public static readonly string BC7_OBJ;

        #endregion
        #region 5. Microsoft C 6.0

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
        #region 6. Microsoft C/C++ 7.0

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
        #region 7. QuickC for Windows 1.0

        /// <summary>
        /// NB07
        /// </summary>
        public static readonly string QCWIN_EXE;

        public static readonly string QCWIN_OBJ; //Not a COFF file

        #endregion
        #region 8. Visual C++ 1.52

        /// <summary>
        /// Visual C++ 1.52 EXE New Executable<para/>
        /// NB09
        /// </summary>
        public static readonly string VC152_EXE;

        public static readonly string VC152_PDB;

        #endregion
        #region 9. Visual C++ 2.0

        public static readonly string VC20_EXE;

        public static readonly string VC20_OBJ; //C7
        public static readonly string VC20_PDB;

        #endregion
        #region 10. Visual C++ 4.0

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
        #region 11. Visual C++ 5.0

        /// <summary>
        /// NB11
        /// </summary>
        public static readonly string VC50_EXE;

        public static readonly string VC50_OBJ; //C11

        //From a separate build to the VC50_EXE; the VC50_EXE contains NB11 symbols without an associated PDB
        public static readonly string VC50_PDB;

        #endregion
        #region 12. Visual C++ 6.0

        //Misc pointing to Dbg which has NB10
        public static readonly string VC60_EXE;
        public static readonly string VC60_PDB;
        public static readonly string VC60_DBG;

        public static readonly string VC60_Coff_EXE;

        #endregion
        #region 13. Visual Studio 2022

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

        #endregion
        #region 14. CLR

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

            #region 1. Microsoft C 4.0

            C400_EXE = MakePath("1. C400\\TESTAPP.EXE");
            C400_MAP = MakePath("1. C400\\TESTAPP.MAP");
            C400_OBJ = MakePath("1. C400\\TESTAPP.OBJ");

            #endregion
            #region 2. Microsoft C 5.0

            C500_EXE = MakePath("2. C500\\TESTAPP.EXE");
            C500_MAP = MakePath("2. C500\\TESTAPP.MAP");
            C500_OBJ = MakePath("2. C500\\TESTAPP.OBJ");

            #endregion
            #region 3. MASM 5

            MASM5_NB00_VXD = MakePath("3. masm5\\NB00_VXD\\EBIOS.386");

            #endregion
            #region 4. Basic PDS 7.0

            BC7_EXE = MakePath("4. bc7\\TESTAPP.EXE");
            BC7_OBJ = MakePath("4. bc7\\TESTAPP.OBj");

            #endregion
            #region 5. Microsoft C 6.0

            C600_NoSymbols_EXE = MakePath("5. C600\\NoSymbols\\TESTAPP.EXE");
            C600_NoSymbols_OBJ = MakePath("5. C600\\NoSymbols\\TESTAPP.OBJ");

            C600_Symbols_EXE = MakePath("5. C600\\Symbols\\TESTAPP.EXE");
            C600_Symbols_OBJ = MakePath("5. C600\\Symbols\\TESTAPP.OBJ");

            C600_Tiny_COM = MakePath("5. C600\\Tiny\\TESTAPP.COM");
            C600_Tiny_DBG = MakePath("5. C600\\Tiny\\TESTAPP.DBG");

            #endregion
            #region 6. Microsoft C/C++ 7.0

            C700_Packed_EXE = MakePath("6. C700\\Packed\\TESTAPP.EXE");
            C700_Packed_OBJ = MakePath("6. C700\\Packed\\TESTAPP.OBJ");

            C700_Unpacked_EXE = MakePath("6. C700\\Unpacked\\TESTAPP.EXE");
            C700_Unpacked_OBJ = MakePath("6. C700\\Unpacked\\TESTAPP.OBJ");

            #endregion
            #region 7. QuickC for Windows 1.0

            QCWIN_EXE = MakePath("7. qcwin\\TESTAPP.EXE");
            QCWIN_OBJ = MakePath("7. qcwin\\TESTAPP.OBJ");

            #endregion
            #region 8. Visual C++ 1.52

            VC152_EXE = MakePath("8. vc152\\TESTAPP.EXE");
            VC152_PDB = MakePath("8. vc152\\TESTAPP.PDB");

            #endregion
            #region 9. Visual C++ 2.0

            VC20_EXE = MakePath("9. vc20\\TestApp.exe");
            VC20_OBJ = MakePath("9. vc20\\main.obj");
            VC20_PDB = MakePath("9. vc20\\TestApp.pdb");

            #endregion
            #region 10. Visual C++ 4.0

            VC40_EXE = MakePath("10. vc40\\Normal\\TestApp.exe");
            VC40_OBJ = MakePath("10. vc40\\Normal\\main.obj");
            VC40_PDB = MakePath("10. vc40\\Normal\\TestApp.pdb");

            VC40_VXD_EXE = MakePath("10. vc40\\CVXD32\\con_samp.exe");
            VC40_LE = MakePath("10. vc40\\CVXD32\\cvxdsamp.vxd");

            #endregion
            #region 11. Visual C++ 5.0

            VC50_EXE = MakePath("11. vc50\\TestApp.exe");
            VC50_OBJ = MakePath("11. vc50\\main.obj");
            VC50_PDB = MakePath("11. vc50\\TestApp.pdb");

            #endregion
            #region 12. Visual C++ 6.0

            VC60_EXE = MakePath("12. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.exe");
            VC60_PDB = MakePath("12. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.pdb");
            VC60_DBG = MakePath("12. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.dbg");

            VC60_Coff_EXE = MakePath("12. vc60\\CoffSymbolsOnly\\TestApp.exe");

            #endregion
            #region 13. Visual Studio 2022

            VS22_EXE = MakePath("13. vs22\\Normal\\TestApp.exe");
            VS22_EXP = MakePath("13. vs22\\Normal\\TestApp.exp"); //Exports (relating to the lib file)
            VS22_LIB = MakePath("13. vs22\\Normal\\TestApp.lib");
            VS22_OBJ = MakePath("13. vs22\\Normal\\TestApp.obj");
            VS22_PDB = MakePath("13. vs22\\Normal\\TestApp.pdb");

            VS22_LTCG_EXE = MakePath("13. vs22\\LTCG\\TestApp.exe");
            VS22_LTCG_EXP = MakePath("13. vs22\\LTCG\\TestApp.exp");
            VS22_LTCG_LIB = MakePath("13. vs22\\LTCG\\TestApp.lib");
            VS22_LTCG_OBJ = MakePath("13. vs22\\LTCG\\TestApp.obj");
            VS22_LTCG_PDB = MakePath("13. vs22\\LTCG\\TestApp.pdb");

            #endregion
            #region 14. CLR

            Framework_EXE     = MakePath("14. CLR\\framework\\TestApp.exe");
            Framework_PDB     = MakePath("14. CLR\\framework\\TestApp.pdb");

            Interop_EXE       = MakePath("14. CLR\\interop\\TestApp.exe");
            Interop_PDB       = MakePath("14. CLR\\interop\\TestApp.pdb");

            Interop_Core_DLL  = MakePath("14. CLR\\interop-core\\TestLib.dll");
            Interop_Core_PDB  = MakePath("14. CLR\\interop-core\\TestLib.pdb");

            MPDB_DLL          = MakePath("14. CLR\\MPDB\\TestLib.dll");

            NativeAOT_EXE     = MakePath("14. CLR\\NativeAOT\\TestApp.exe");
            NativeAOT_PDB     = MakePath("14. CLR\\NativeAOT\\TestApp.pdb");

            NGEN_DLL          = MakePath("14. CLR\\NGEN\\TestLib.dll");
            NGEN_NI_DLL       = MakePath("14. CLR\\NGEN\\TestLib.ni.dll");
            NGEN_PDB          = MakePath("14. CLR\\NGEN\\TestLib.pdb");
            NGEN_NI_PDB       = MakePath("14. CLR\\NGEN\\TestLib.ni.pdb");

            R2R_EXE           = MakePath("14. CLR\\R2R\\TestApp.exe");
            R2R_DLL           = MakePath("14. CLR\\R2R\\TestApp.dll");
            R2R_PDB           = MakePath("14. CLR\\R2R\\TestApp.pdb");

            SingleFileApp_EXE = MakePath("14. CLR\\SingleFileApp\\TestApp.exe");
            SingleFileApp_PDB = MakePath("14. CLR\\SingleFileApp\\TestApp.pdb");

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
                    VC50_PDB,
                    VS22_PDB,
                    VS22_LTCG_PDB,
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
