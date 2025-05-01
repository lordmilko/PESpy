using System.IO;

namespace PESpy.Tests
{
    public static class Sample
    {
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

            #region 1. Microsoft C 4.0

            C400_EXE = Path.Combine(sampleDir, "1. C400\\TESTAPP.EXE");
            C400_MAP = Path.Combine(sampleDir, "1. C400\\TESTAPP.MAP");
            C400_OBJ = Path.Combine(sampleDir, "1. C400\\TESTAPP.OBJ");

            #endregion
            #region 2. Microsoft C 5.0

            C500_EXE = Path.Combine(sampleDir, "2. C500\\TESTAPP.EXE");
            C500_MAP = Path.Combine(sampleDir, "2. C500\\TESTAPP.MAP");
            C500_OBJ = Path.Combine(sampleDir, "2. C500\\TESTAPP.OBJ");

            #endregion
            #region 3. Basic PDS 7.0

            BC7_EXE = Path.Combine(sampleDir, "3. bc7\\TESTAPP.EXE");
            BC7_OBJ = Path.Combine(sampleDir, "3. bc7\\TESTAPP.OBj");

            #endregion
            #region 4. Microsoft C 6.0

            C600_NoSymbols_EXE = Path.Combine(sampleDir, "4. C600\\NoSymbols\\TESTAPP.EXE");
            C600_NoSymbols_OBJ = Path.Combine(sampleDir, "4. C600\\NoSymbols\\TESTAPP.OBJ");

            C600_Symbols_EXE = Path.Combine(sampleDir, "4. C600\\Symbols\\TESTAPP.EXE");
            C600_Symbols_OBJ = Path.Combine(sampleDir, "4. C600\\Symbols\\TESTAPP.OBJ");

            C600_Tiny_COM = Path.Combine(sampleDir, "4. C600\\Tiny\\TESTAPP.COM");
            C600_Tiny_DBG = Path.Combine(sampleDir, "4. C600\\Tiny\\TESTAPP.DBG");

            #endregion
            #region 5. Microsoft C/C++ 7.0

            C700_Packed_EXE = Path.Combine(sampleDir, "5. C700\\Packed\\TESTAPP.EXE");
            C700_Packed_OBJ = Path.Combine(sampleDir, "5. C700\\Packed\\TESTAPP.OBJ");

            C700_Unpacked_EXE = Path.Combine(sampleDir, "5. C700\\Unpacked\\TESTAPP.EXE");
            C700_Unpacked_OBJ = Path.Combine(sampleDir, "5. C700\\Unpacked\\TESTAPP.OBJ");

            #endregion
            #region 6. QuickC for Windows 1.0

            QCWIN_EXE = Path.Combine(sampleDir, "6. qcwin\\TESTAPP.EXE");
            QCWIN_OBJ = Path.Combine(sampleDir, "6. qcwin\\TESTAPP.OBJ");

            #endregion
            #region 7. Visual C++ 1.52

            VC152_EXE = Path.Combine(sampleDir, "7. vc152\\TESTAPP.EXE");
            VC152_PDB = Path.Combine(sampleDir, "7. vc152\\TESTAPP.PDB");

            #endregion
            #region 8. Visual C++ 4.0

            VC40_EXE = Path.Combine(sampleDir, "8. vc40\\TestApp.exe");
            VC40_OBJ = Path.Combine(sampleDir, "8. vc40\\TestApp.obj");
            VC40_PDB = Path.Combine(sampleDir, "8. vc40\\TestApp.pdb");

            #endregion
            #region 9. Visual C++ 5.0

            VC50_EXE = Path.Combine(sampleDir, "9. vc50\\TestApp.exe");
            VC50_OBJ = Path.Combine(sampleDir, "9. vc50\\main.obj");

            #endregion
            #region 10. Visual C++ 6.0

            VC60_EXE = Path.Combine(sampleDir, "10. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.exe");
            VC60_PDB = Path.Combine(sampleDir, "10. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.pdb");
            VC60_DBG = Path.Combine(sampleDir, "10. vc60\\CoffAndPdbSymbols_PostSplit\\TestApp.dbg");

            #endregion
            #region 11. Visual Studio 2022

            VS22_EXE = Path.Combine(sampleDir, "11. vs22\\Normal\\TestApp.exe");
            VS22_EXP = Path.Combine(sampleDir, "11. vs22\\Normal\\TestApp.exp"); //Exports (relating to the lib file)
            VS22_LIB = Path.Combine(sampleDir, "11. vs22\\Normal\\TestApp.lib");
            VS22_OBJ = Path.Combine(sampleDir, "11. vs22\\Normal\\TestApp.obj");
            VS22_PDB = Path.Combine(sampleDir, "11. vs22\\Normal\\TestApp.pdb");

            VS22_LTCG_EXE = Path.Combine(sampleDir, "11. vs22\\LTCG\\TestApp.exe");
            VS22_LTCG_EXP = Path.Combine(sampleDir, "11. vs22\\LTCG\\TestApp.exp");
            VS22_LTCG_LIB = Path.Combine(sampleDir, "11. vs22\\LTCG\\TestApp.lib");
            VS22_LTCG_OBJ = Path.Combine(sampleDir, "11. vs22\\LTCG\\TestApp.obj");
            VS22_LTCG_PDB = Path.Combine(sampleDir, "11. vs22\\LTCG\\TestApp.pdb");

            #endregion
        }
    }
}
