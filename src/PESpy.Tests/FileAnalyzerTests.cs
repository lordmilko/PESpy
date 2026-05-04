using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    [TestClass]
    public class FileAnalyzerTests
    {
        [TestMethod]
        public void FileAnalyzer_PEFile() => Test(Sample.VC20_EXE, FileKind.PE);

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
    }
}
