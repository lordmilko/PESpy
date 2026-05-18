using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.PDB;
using SymHelp;

namespace PESpy.Tests
{
    [TestClass]
    public class PDBHashTests
    {
        //Tests that hash things using PDBFile/PDB1

        #region HashSym

        [TestMethod]
        public unsafe void PDBHash_Globals_V7()
        {
            TestHashSym(Locator.LocatePDB(WellKnownTestModule.ntdll));
        }

        //There's no GSIHashHdr
        [TestMethod]
        public unsafe void PDBHash_Globals_V2() =>
            TestHashSym(Sample.VC40_PDB);

        private void TestHashSym(string fileName)
        {
            using var pdbFile = PDBFile.FromFile(fileName);

            var ourGlobals = pdbFile.GSI;
            var ourPublics = pdbFile.PSGSI;

            using var pdb1 = VsPDB.OpenPDB(fileName);
            using var dbi1 = pdb1.OpenDBI(null, "r");
            using var theirGlobals = dbi1.OpenGlobals();
            using var theirPublics = dbi1.OpenPublics();

            TestHashSym(ourGlobals, theirGlobals, pdbFile);
            TestHashSym(ourPublics, theirPublics, pdbFile);
        }

        private unsafe void TestHashSym(MsfStream.GSI ourGsi, GSI1 theirGsi, PDBFile pdbFile)
        {
            foreach (var globalSym in ourGsi.Symbols)
            {
                var name = globalSym.ToString();

                SYMTYPE* theirResult = theirGsi.HashSymW(name, default);

                if (theirResult == default)
                {
                    if (ourGsi.TryGetSymbol(name, out var ourResult))
                        Assert.Fail("PDB1 did not return a result, but we did");
                }
                else
                {
                    var theirName = ((SymType) theirResult).GetName(pdbFile);

                    if (!ourGsi.TryGetSymbol(name, out var ourResult))
                        Assert.Fail("PDB1 got a result but we didn't");

                    //We both got results; were they the same?

                    var ourName = ourResult.GetName();

                    Assert.AreEqual(ourName, theirName);
                }
            }
        }

        #endregion
        #region Type Hash

        //Foreach type, try and lookup that type using its type index

        [TestMethod]
        public void TpiHash_impv80_StressTest() =>
            TestTpiHash(Locator.LocatePDB(WellKnownTestModule.coreclr));

        [TestMethod]
        public void TpiHash_impv80() =>
            TestTpiHash(Sample.VS22_PDB);

        [TestMethod]
        public void TpiHash_impv50() =>
            TestTpiHash(Sample.VC50_PDB);

        [TestMethod]
        public void TpiHash_impv40() =>
            TestTpiHash(Sample.VC40_PDB);

        [TestMethod]
        public void TpiHash_intvVC2() =>
            TestTpiHash(Sample.VC20_PDB, false);

        private unsafe void TestTpiHash(string fileName, bool compare = true)
        {
            using var pdbFile = PDBFile.FromFile(fileName);

            var tpi = pdbFile.TPI;
            var tpiHash = tpi.TpiHash;

            var hdr = tpi.Hdr;

            if (!compare)
            {
                for (var i = hdr.tiMin; i < hdr.tiMac; i++)
                {
                    var ourType = tpiHash.GetTypTypeFromIndex(i);

                    if (!tpiHash.TryGetIndexFromTypType(ourType, out var typeIndex))
                        throw new NotImplementedException();

                    Assert.AreEqual(i, typeIndex);
                }

                return;
            }

            using var pdb1 = VsPDB.OpenPDB(fileName);
            using var tpi1 = pdb1.OpenTpi("r");

            var missing = new List<(int i, LEAF_ENUM_e leaf)>();

            for (var i = hdr.tiMin; i < hdr.tiMac; i++)
            {
                var ourType = tpiHash.GetTypTypeFromIndex(i);

                var typType = tpi1.QueryPbCVRecordForTi(i);

                var leaf = typType->leaf;

                //QueryTi16ForCVRecord now always returns an error
                //LF_CLASS, LF_STRUCTURE, LF_UNION and LF_ENUM sometimes don't respond
                //to this for some reason, perhaps because they're UDT's
                if (tpi1.TryQueryTiForCVRecord(typType, out var ti32))
                {
                    if (!tpiHash.TryGetIndexFromTypType(ourType, out var typeIndex))
                        throw new NotImplementedException();

                    Assert.AreEqual(i, ti32);
                    Assert.AreEqual(i, typeIndex);
                }
                else
                {
                    if (!tpiHash.TryGetIndexFromTypType(ourType, out var typeIndex))
                        throw new NotImplementedException();

                    Assert.AreEqual(i, typeIndex);
                }
            }
        }

        #endregion
        #region Map Find

        [TestMethod]
        public void MapFind()
        {
            Assert.Inconclusive();

            using var symbolModule = SymbolProvider.LoadModule(Locator.Locate(WellKnownTestModule.WinForms), false);

            throw new NotImplementedException();
        }

        #endregion
        #region V1

        [TestMethod]
        public unsafe void PDBHash_V1()
        {
            using var pdbFile = (PDB1File) PDBFile.FromFile(Sample.VC152_PDB);

            for (var i = 0; i < pdbFile.Records.Length; i++)
            {
                var record = pdbFile.Records[i];

                Assert.IsTrue(pdbFile.TryGetIndexFromTypType(record.type, out var index));
                Assert.AreEqual(i, index - pdbFile.Hdr.tiMin);

                pdbFile.TryGetTypTypeFromIndex((CV_typ_t) (i + pdbFile.Hdr.tiMin), out var typType);
                Assert.IsTrue(typType.AsSpan().SequenceEqual(record.type.AsSpan()));
            }
        }

        #endregion
    }
}
