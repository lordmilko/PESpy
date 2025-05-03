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
        #region 3. Basic PDS 7.0

        /// <summary>
        /// NB01
        /// </summary>
        public static readonly string BC7_EXE;

        public static readonly string BC7_OBJ;

        #endregion
        #region 4. Microsoft C 6.0

        public static readonly string C600_NoSymbols_EXE;
        public static readonly string C600_NoSymbols_OBJ;

        /// <summary>
        /// NB02
        /// </summary>
        public static readonly string C600_Symbols_EXE;

        public static readonly string C600_Symbols_OBJ;

        public static readonly string C600_Tiny_COM;
        public static readonly string C600_Tiny_DBG;

        #endregion
        #region 5. Microsoft C/C++ 7.0

        /// <summary>
        /// NB08
        /// </summary>
        public static readonly string C700_Packed_EXE;

        public static readonly string C700_Packed_OBJ;

        /// <summary>
        /// NB05
        /// </summary>
        public static readonly string C700_Unpacked_EXE;

        public static readonly string C700_Unpacked_OBJ;

        #endregion
        #region 6. QuickC for Windows 1.0

        /// <summary>
        /// NB07
        /// </summary>
        public static readonly string QCWIN_EXE;

        public static readonly string QCWIN_OBJ;

        #endregion
        #region 7. Visual C++ 1.52

        /// <summary>
        /// Visual C++ 1.52 EXE New Executable<para/>
        /// NB09
        /// </summary>
        public static readonly string VC152_EXE;

        public static readonly string VC152_PDB;

        #endregion
        #region 8. Visual C++ 4.0

        /// <summary>
        /// NB10
        /// </summary>
        public static readonly string VC40_EXE;

        public static readonly string VC40_OBJ;
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
        #region 9. Visual C++ 5.0

        /// <summary>
        /// NB11
        /// </summary>
        public static readonly string VC50_EXE;

        public static readonly string VC50_OBJ;

        #endregion
        #region 10. Visual C++ 6.0

        //Misc pointing to Dbg which has NB10
        public static readonly string VC60_EXE;
        public static readonly string VC60_PDB;
        public static readonly string VC60_DBG;

        #endregion
        #region 11. Visual Studio 2022

        /// <summary>
        /// Visual Studio 2022 Portable Executable<para/>
        /// RSDS
        /// </summary>
        public static readonly string VS22_EXE;
        public static readonly string VS22_EXP;
        public static readonly string VS22_LIB;
        public static readonly string VS22_OBJ;
        public static readonly string VS22_PDB;

        public static readonly string VS22_LTCG_EXE;
        public static readonly string VS22_LTCG_EXP;
        public static readonly string VS22_LTCG_LIB;
        public static readonly string VS22_LTCG_OBJ;
        public static readonly string VS22_LTCG_PDB;

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
            #region 3. Basic PDS 7.0

            BC7_EXE = MakePath("3. bc7\\TESTAPP.EXE");
            BC7_OBJ = MakePath("3. bc7\\TESTAPP.OBj");

            #endregion
            #region 4. Microsoft C 6.0

            C600_NoSymbols_EXE = MakePath("4. C600\\NoSymbols\\TESTAPP.EXE");
            C600_NoSymbols_OBJ = MakePath("4. C600\\NoSymbols\\TESTAPP.OBJ");

            C600_Symbols_EXE = MakePath("4. C600\\Symbols\\TESTAPP.EXE");
            C600_Symbols_OBJ = MakePath("4. C600\\Symbols\\TESTAPP.OBJ");

            C600_Tiny_COM = MakePath("4. C600\\Tiny\\TESTAPP.COM");
            C600_Tiny_DBG = MakePath("4. C600\\Tiny\\TESTAPP.DBG");

            #endregion
            #region 5. Microsoft C/C++ 7.0

            C700_Packed_EXE = MakePath("5. C700\\Packed\\TESTAPP.EXE");
            C700_Packed_OBJ = MakePath("5. C700\\Packed\\TESTAPP.OBJ");

            C700_Unpacked_EXE = MakePath("5. C700\\Unpacked\\TESTAPP.EXE");
            C700_Unpacked_OBJ = MakePath("5. C700\\Unpacked\\TESTAPP.OBJ");

            #endregion
            #region 6. QuickC for Windows 1.0

            QCWIN_EXE = MakePath("6. qcwin\\TESTAPP.EXE");
            QCWIN_OBJ = MakePath("6. qcwin\\TESTAPP.OBJ");

            #endregion
            #region 7. Visual C++ 1.52

            VC152_EXE = MakePath("7. vc152\\TESTAPP.EXE");
            VC152_PDB = MakePath("7. vc152\\TESTAPP.PDB");

            #endregion
            #region 8. Visual C++ 4.0

            VC40_EXE = MakePath("8. vc40\\Normal\\TestApp.exe");
            VC40_OBJ = MakePath("8. vc40\\Normal\\main.obj");
            VC40_PDB = MakePath("8. vc40\\Normal\\TestApp.pdb");

            VC40_VXD_EXE = MakePath("8. vc40\\CVXD32\\con_samp.exe");
            VC40_LE = MakePath("8. vc40\\CVXD32\\cvxdsamp.vxd");

            #endregion
            #region 9. Visual C++ 5.0

            VC50_EXE = MakePath("9. vc50\\TestApp.exe");
            VC50_OBJ = MakePath("9. vc50\\main.obj");

            #endregion
            #region 10. Visual C++ 6.0

            VC60_EXE = MakePath("10. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.exe");
            VC60_PDB = MakePath("10. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.pdb");
            VC60_DBG = MakePath("10. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.dbg");

            #endregion
            #region 11. Visual Studio 2022

            VS22_EXE = MakePath("11. vs22\\Normal\\TestApp.exe");
            VS22_EXP = MakePath("11. vs22\\Normal\\TestApp.exp"); //Exports (relating to the lib file)
            VS22_LIB = MakePath("11. vs22\\Normal\\TestApp.lib");
            VS22_OBJ = MakePath("11. vs22\\Normal\\TestApp.obj");
            VS22_PDB = MakePath("11. vs22\\Normal\\TestApp.pdb");

            VS22_LTCG_EXE = MakePath("11. vs22\\LTCG\\TestApp.exe");
            VS22_LTCG_EXP = MakePath("11. vs22\\LTCG\\TestApp.exp");
            VS22_LTCG_LIB = MakePath("11. vs22\\LTCG\\TestApp.lib");
            VS22_LTCG_OBJ = MakePath("11. vs22\\LTCG\\TestApp.obj");
            VS22_LTCG_PDB = MakePath("11. vs22\\LTCG\\TestApp.pdb");

            #endregion
        }
    }
}
