using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.PDB;

namespace PESpy.Tests
{
    [TestClass]
    public class DetectorTests
    {
        [TestMethod]
        public void Detector_PE() => Test("C:\\Windows\\system32\\ntdll.dll", FileKind.PE);

        [TestMethod]
        public void Detector_NE() => Test(Sample.VC152_EXE, FileKind.NE);

        [TestMethod]
        public void Detector_DBG() => Test(Sample.VC60_DBG, FileKind.DBG);

        [TestMethod]
        public void Detector_PDB1() => Test<PDB1File>(Sample.VC152_PDB, FileKind.PDB, v => v.PDBKind == PDBFileKind.V1);

        [TestMethod]
        public void Detector_PDB2() => Test<PDB2File>(Sample.VC40_PDB, FileKind.PDB, v => v.PDBKind == PDBFileKind.V2);

        [TestMethod]
        public void Detector_PDB7() => Test<PDB7File>(Sample.VS22_PDB, FileKind.PDB, v => v.PDBKind == PDBFileKind.V7);

        [TestMethod]
        public void Detector_OBJ_Legacy() => Test<OBJFile>(Sample.VS22_OBJ, FileKind.OBJ, v => v.AnonObjectHeader == null);

        [TestMethod]
        public void Detector_OBJ_LTCG() => Test<OBJFile>(Sample.VS22_LTCG_OBJ, FileKind.OBJ, v => v.AnonObjectHeader != null);

        //Doesn't seem like LTCG LIBs use Anon Header; they use classic header
        [TestMethod]
        public void Detector_LIB() => Test(Sample.VS22_LTCG_LIB, FileKind.LIB);

        [TestMethod]
        public void Detector_VXD() => Test(Sample.MASM5_NB00_VXD, FileKind.LE);

        [TestMethod]
        public void Detector_OMF() => Test(Sample.C700_Packed_OBJ, FileKind.OMF);

        //I don't have an OMBLIB sample file

        [TestMethod]
        public void Detector_OMFDBG() => Test(Sample.C600_Tiny_DBG, FileKind.OMFDBG);

        private void Test(string path, FileKind expectedKind) =>
            Test<IFile>(path, expectedKind, null);

        private void Test<T>(string path, FileKind expectedKind, Func<T, bool> verify) where T : IFile
        {
            using var file = Detector.TryOpenFile(path);

            if (file == null)
                Assert.Fail("Failed to open file");

            Assert.AreEqual(expectedKind, file!.Kind);

            if (verify != null)
                Assert.IsTrue(verify.Invoke((T) file));
        }
    }
}
