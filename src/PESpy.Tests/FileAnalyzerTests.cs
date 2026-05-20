using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    [TestClass]
    public class FileAnalyzerTests
    {
        #region File

        [TestMethod]
        public void FileAnalyzer_PEFile() => Test(Sample.VC20_EXE, FileKind.PE);

        [TestMethod]
        public void FileAnalyzer_PEFile_Nested() => Test(Sample.SingleFileApp_EXE, FileKind.PE);

        [TestMethod]
        public void FileAnalyzer_NEFile() => Test(Sample.NE, FileKind.NE);

        [TestMethod]
        public void FileAnalyzer_LEFile() => Test(Sample.VC40_LE, FileKind.LE);

        [TestMethod]
        public void FileAnalyzer_DOSFile() => Test(Sample.C600_Symbols_EXE, FileKind.DOS);

        [TestMethod]
        public void FileAnalyzer_DBGFile() => Test(Sample.VC60_DBG, FileKind.DBG);

        [TestMethod]
        public void FileAnalyzer_PDBFile() => Test(Sample.VS22_PDB, FileKind.PDB);

        [TestMethod]
        public void FileAnalyzer_PortablePDBFile() => Test(Sample.R2R_PDB, FileKind.PortablePDB);

        [TestMethod]
        public void FileAnalyzer_OBJFile() => Test(Sample.VS22_OBJ, FileKind.OBJ);

        [TestMethod]
        public void FileAnalyzer_LIBFile() => Test(Sample.VS22_LIB, FileKind.LIB);

        [TestMethod]
        public void FileAnalyzer_OMFFile() => Test(Sample.C600_Symbols_OBJ, FileKind.OMF);

        //[TestMethod]
        //public void FileAnalyzer_OMFLIBFile() => Test();

        [TestMethod]
        public void FileAnalyzer_OMFDBGFile() => Test(Sample.C600_Tiny_DBG, FileKind.OMFDBG);

        [TestMethod]
        public void FileAnalyzer_SYMFile() => Test(Sample.C400_SYM, FileKind.SYM);

        private void Test(string path, FileKind fileKind)
        {
            using var file = Detector.OpenFile(path);

            Assert.AreEqual(fileKind, file.Kind);

            var view = file.GetView();

            view.Accept(NullViewWalker.Instance);
        }

        #endregion

        [TestMethod]
        public void FileAnalyzer_ILName()
        {
            //We need to apply names to all IL instructions, and need to be able to retrieve
            //those names later

            using var peFile = PEFile.FromFile(Sample.Framework_EXE);

            var view = peFile.GetView();

            var entity = view[1][2][1];

            var str = entity.ToString(ViewFormatFlags.None);

            Assert.AreEqual("TestApp.Program.Main", str);
        }

        [TestMethod]
        public void FileAnalyzer_KnownSymbolNames()
        {
            //When symbols aren't available, known entities should still have names applied to them.
            //i.e. many entities pointed to by the load config table

            using var peFile = PEFile.FromKey(WellKnownTestModule.ntdll);

            var view = peFile.GetView(excludeSymbols: true);

            var entity = (IValueView) view.GetViewFromOffset(0x196530);

            Assert.AreEqual("__security_cookie", entity.Name.ToString());
        }
    }
}
