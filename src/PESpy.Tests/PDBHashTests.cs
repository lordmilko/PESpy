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

            var sw = Stopwatch.StartNew();

            for (var i = hdr.tiMin; i < hdr.tiMac; i++)
            {
                var ourType = tpiHash.GetTypTypeFromIndex(i);

                var typType = tpi1.QueryPbCVRecordForTi(i);
        private unsafe void TestAddressMap(
            ImageSectionHeader[] sectionHeaders,
            MsfStream.PSGSI psgsi,
            GSI1 gsi,
            PDBFile pdbFile)
        {
            for (var i = 0; i < sectionHeaders.Length; i++)
            {
                var sectionHeader = sectionHeaders[i];

                for (var j = 0; j < sectionHeader.VirtualSize; j++)
                {
                    var theirResult = gsi.NearestSym((ushort) (i + 1), j, out var theirDisp);

                    if (theirResult! == null)
                    {
                        if (psgsi.TryGetNearestSymbol(j, i + 1, out var ourSym, out var ourDisp))
                            throw new NotImplementedException();
                    }
                    else
                    {
                        var theirName = ((SymType) theirResult).GetName(pdbFile);

                        if (!psgsi.TryGetNearestSymbol(j, i + 1, out var ourResult, out var ourDisp))
                            throw new NotImplementedException();

                        var ourName = ((SymType) ourResult).GetName(pdbFile);
                        static string CleanName(SymString name)
                        {
                            var str = name.ToString();

                            if (str.Contains("ILT") && str.Contains("(?"))
                            {
                                var openParen = str.IndexOf('(');
                                var closeParen = str.IndexOf(')');

                                var mangledStr = str.Substring(openParen + 1, closeParen - openParen - 1);

                                var demangled = Demangler.ParseString(mangledStr, ClrDebug.DIA.UNDNAME.UNDNAME_NAME_ONLY);

                                return str.Substring(0, openParen + 1) + demangled + str.Substring(closeParen);
                            }

                            return str;
                        }
                        if (theirName == ".Base")
                            continue;

                        //We generate thunk symbols with nice demangled names. PDB1 does not, so if there's a name difference,
                        //check if we need to demangle their name
                        Assert.AreEqual(ourName.ToString(), CleanName(theirName));

                        if (ourName.StartsWith("@ILT"))
                            continue; //PDB1 does not properly report displacements for thunks

                        Assert.AreEqual(ourDisp, theirDisp);
                    }
                }
            }
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
