using System;
using System.Text.RegularExpressions;
using System.Threading;
using ClrDebug.DIA;
using ClrDebug.PDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.PDB;
using PESpy.View;
using SymHelp;
using SymHelp.Symbols.MicrosoftPdb;
using SymHelp.Symbols.PDBFile;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy.Tests
{
    struct ComparePDBContext
    {
        public DBI1 dbi;
        public GSI1 gsi;
        public GSI1 psgsi;
        public MicrosoftPdbSymbolModule diaModule;

        public PEFile peFile;
        public PDBFile pdbFile;
        public int sizeOfImage;
    }

    [TestClass]
    public class PDBFileTests : BaseTest
    {
        [TestMethod]
        public unsafe void PDBFile_NearestSym_StressTest()
        {
            var imageName = Sample.VS22_Debug_EXE;

            ComparePDB(imageName, ctx =>
            {
                for (var i = 0; i < ctx.sizeOfImage; i++)
                {
                    ctx.pdbFile.GetSectionAndOffset(i, out var seg, out var off, out _);

                    var ourPublic = ctx.pdbFile.PSGSI;

                    SymType theirSym = ctx.psgsi.NearestSym(seg, off, out var theirDisp);

                    if (theirSym != default)
                    {
                        Assert.IsTrue(ourPublic.TryGetNearestSymbol(off, seg, out var ourSym, out var ourDisp));

                        theirSym.TryGetRawOffSeg(out var theirOff, out var theirSeg);
                        ourSym.TryGetRawOffSeg(out var ourOff, out var ourSeg);

                        var theirName = CleanName(theirSym.ToString());

                        //Another difference we have is that we construct a single symbol
                        //against the start of the thunk (e.g. @ILT+0, @ILT+5, etc), however
                        //mspdbcore constructs a unique symbol for every single displacement,
                        //reporting a displacement of 0 every time. This is a bad design.
                        //So if we see that we have a displacement, we'll skip verifying the name
                        //and the displacement

                        if (theirName.StartsWith("@ILT+") && ourDisp != 0)
                        {
                            //Can't verify name, but we need to decrement their offset
                            //so it matches ours
                            theirOff -= ourDisp;
                        }
                        else
                        {
                            Assert.AreEqual(theirName, ourSym.ToString(), "Name was not correct");
                            Assert.AreEqual(theirDisp, ourDisp, "Displacement was not correct");
                        }

                        Assert.AreEqual(theirOff, ourOff, "Offset was not correct");
                        Assert.AreEqual(theirSeg, ourSeg, "Segment was not correct");
                    }
                    else
                    {
                        Assert.IsFalse(ourPublic.TryGetNearestSymbol(off, seg, out var ourSym, out var ourDisp));
                    }
                }
            });
        }

        private static string CleanName(string name)
        {
            //A difference between our symbols and mspdbcore's is we undecorate the name prior
            //to wrapping it in the ILT symbol. As such, if we encounter a name from PDB1
            //that needs undecorating, we need to update the name to match what our one will be

            if (name.StartsWith("@ILT+"))
            {
                var pattern = "(@ILT\\+\\d+\\()(.+?)(\\))";

                var str = Regex.Replace(name, pattern, "$2");

                var demangled = Demangler.ParseString(str, UNDNAME.UNDNAME_NAME_ONLY);

                var result = Regex.Replace(name, pattern, $"$1{demangled}$3");

                return result;
            }

            return name;
        }

        [TestMethod]
        public unsafe void PDBFile_EnumPubsByAddr_StressTest()
        {
            var imageName = Sample.VS22_Debug_EXE;

            ComparePDB(imageName, ctx =>
            {
                var theirPublics = ctx.psgsi;

                using var theirs = theirPublics.GetEnumByAddr();

                var ourPublics = ctx.pdbFile.PSGSI;
                Assert.IsTrue(ourPublics.TryEnumByAddr(out var ours));

                var sizeOfImage = ctx.peFile.OptionalHeader.SizeOfImage;

                for (var i = 0; i < sizeOfImage; i++)
                {
                    ctx.pdbFile.GetSectionAndOffset(i, out var seg, out var off, out _);

                    if (theirs.TryLocate(seg, off))
                        Assert.IsTrue(ours.Locate(seg, off));
                    else
                        Assert.IsFalse(ours.Locate(seg, off));

                    if (theirs.TryNext())
                        Assert.IsTrue(ours.Next());
                    else
                    {
                        Assert.IsFalse(ours.Next());
                        continue; //Don't try and read their SC; we'll read garbage, and our one will throw out of bounds
                    }

                    var theirSym = (PubSym32) (SymType) theirs.Get;
                    var ourSym = (PubSym32) ours.Current;

                    var theirName = CleanName(theirSym.ToString());

                    Assert.AreEqual(theirName, ourSym.ToString(), "Name was not correct");

                    Assert.AreEqual(theirSym.off, ourSym.off);
                    Assert.AreEqual(theirSym.seg, ourSym.seg);
                }
            });
        }

        [TestMethod]
        public void PDBFile_EnumSC_StressTest()
        {
            ClrDebug.Extensions.DiaStringsUseComHeap = true;

            var imageName = Sample.VS22_Debug_EXE;

            ComparePDB(imageName, ctx =>
            {
                using var theirs = ctx.dbi.GetEnumContrib();
                Assert.IsTrue(ctx.pdbFile.DBI.TryEnumContribs(out var ours));

                for (var i = 0; i < ctx.sizeOfImage; i++)
                {
                    ctx.pdbFile.GetSectionAndOffset(i, out var seg, out var off, out _);

                    if (theirs.TryLocate(seg, off))
                        Assert.IsTrue(ours.Locate(seg, off));
                    else
                        Assert.IsFalse(ours.Locate(seg, off));

                    if (theirs.TryNext())
                        Assert.IsTrue(ours.Next());
                    else
                    {
                        Assert.IsFalse(ours.Next());
                        continue; //Don't try and read their SC; we'll read garbage, and our one will throw out of bounds
                    }

                    var theirSC = theirs.Get;
                    var ourSC = ours.Current;

                    Assert.AreEqual(theirSC.poff, ourSC.off);
                    Assert.AreEqual(theirSC.pisect, (ushort) ourSC.isect);
                }
            });
        }

        [TestMethod]
        public void PDBFile_RVA_StressTest()
        {
            StressTestRVA(Sample.VS22_Debug_EXE);
        }

        [TestMethod]
        public void PDBFile_RVA_OMAP_StressTest()
        {
            StressTestRVA(Locator.Locate(WellKnownTestModule.CppDebug));
        }

        [TestMethod]
        public void NB05_RVA_StressTest()
        {
            using var peFile = PEFile.FromFile(Sample.NB11);

            var symbolAccessor = (NB09SymbolAccessor) peFile.GetSymbolAccessor();

            var sizeOfImage = peFile.OptionalHeader.SizeOfImage;

            for (var i = 0; i < sizeOfImage; i++)
            {
                symbolAccessor.TryGetSymbolByRVA(i, out var symType, out var displacement);
            }
        }

        private unsafe void StressTestRVA(string imageName)
        {
            ComparePDB(imageName, ctx =>
            {
                var pdb = ctx.diaModule.PDB1Module;

                var lastPerc = 0;

                using var pdbFileModule = SymbolProvider.LoadModule(ctx.peFile.FileName);

                var diaModule = ctx.diaModule;

                for (var i = 0; i < ctx.sizeOfImage; i++)
                {
                    var perc = (int) ((double) i / ctx.sizeOfImage * 100);

                    if (perc != lastPerc)
                    {
                        lastPerc = perc;
                        System.Diagnostics.Debug.WriteLine(perc);
                    }

                    var va = ctx.peFile.OptionalHeader.ImageBase + i;

                    if (pdbFileModule.TryGetSymbolFromAddress(va, out var ourSymbol))
                    {
                        if (!diaModule.TryGetSymbolFromAddress(va, out var theirSymbol))
                            Assert.Fail($"We erroneously returned symbol '{ourSymbol}' when we shouldn't have");

                        var diaSymbol = ((MicrosoftPdbSymbol) theirSymbol.Symbol).DiaSymbol;

                        diaSymbol.TryGetName(out var diaName);

                        if (theirSymbol.Name != "`string'")
                        {
                            if (diaName != ourSymbol.Name)
                            {
                                //The only difference we allow is if multiple symbols got compiled to the same address
                                //e.g. in coreclr the first listed symbol is EECodeManager::GetInstance, followed by
                                //EECodeManager::GetParamTypeArg. DIA returns GetParamTypeArg, however the actual implementation
                                //is that of GetInstance. A scenario where the first listed symbol is preferred is
                                //COMToCLR_ILStubState::`scalar deleting destructor'. There's another symbol PInvoke_ILStubState::`scalar deleting destructor'
                                //I can't tell which one we're supposed to use; I feel like DIA returning either the first or the second one is just an artifact
                                //of its sort or something...although that doesn't make complete sense because it builds the symbols using a map. In any case,
                                //for now we'll say if they're both SymTagFunction and have the same address, it's all good

                                var symTag = ((ISymbolInterop) ourSymbol.Symbol).SymTag;

                                Assert.AreEqual(symTag, diaSymbol.SymTag);

                                ((PDBFileSymbol) ourSymbol.Symbol).SymType.TryGetOffSeg(out var ourOff, out var ourSeg);

                                var theirOff = diaSymbol.AddressOffset;
                                var theirSeg = diaSymbol.AddressSection;

                                Assert.AreEqual(ourOff, theirOff);
                                Assert.AreEqual(ourSeg, theirSeg);
                            }
                        }
                        Assert.AreEqual(ourSymbol.Displacement, theirSymbol.Displacement);
                    }
                    else
                    {
                        if (diaModule.TryGetSymbolFromAddress(va, out var displacedSymbol))
                        {
                            //Didn't expect to have a symbol here

                            //Is the difference within DIA or PDB1?

                            Assert.Fail($"Got symbol '{displacedSymbol}' from DIA but we didn't get a symbol");
                        }
                    }
                }
            });
        }

        private void ComparePDB(string imageName, Action<ComparePDBContext> action)
        {
            ClrDebug.Extensions.DiaStringsUseComHeap = true;

            using var peFile = PEFile.FromFile(imageName);
            using var pdbFile = PDBFile.FromFile(Locator.LocatePDB(imageName));

            //Don't dispose the symbol module; there seems to be some sort of bug with DIA wherein its crashing
            //cleaning up a ModCache likely as a result of us touching every single address in the executable
            using var diaModule = (MicrosoftPdbSymbolModule) SymbolProvider.LoadModule(peFile.FileName, preferDIA: true);

            var pdb1 = new PDB1(diaModule.DiaDataSource.RawPDBPtr); //Do _not_ dispose this! We don't own it!
            using var dbi = pdb1.OpenDBI(null, PdbOpenMode.pdbRead);
            using var psgsi = dbi.OpenPublics();
            using var gsi = dbi.OpenGlobals();

            var sizeOfImage = peFile.OptionalHeader.SizeOfImage;

            var ctx = new ComparePDBContext
            {
                dbi = dbi,
                gsi = gsi,
                psgsi = psgsi,
                peFile = peFile,
                pdbFile = pdbFile,
                diaModule = diaModule,
                sizeOfImage = sizeOfImage
            };

            action(ctx);
        }
        #region DBI

        [TestMethod]
        public void DBIHdr_Test()
        {
            TestStruct<DBIHdr>(
                v => v.snGSSyms == 72,
                v => v.snPSSyms == 73,
                v => v.snSymRecs == 74,
                v => v.cbGpModi == 7232,
                v => v.cbSC == 9260,
                v => v.cbSecMap == 124,
                v => v.cbFileInfo == 1192
            );

            TestView<DBIHdr>(
                v => v.VerifyStruct(
                    name: "DBIHdr", offset: 300032, size: 24,
                    c => c.VerifyField(name: "snGSSyms", value: (ushort) 72),
                    c => c.VerifyField(name: "snPSSyms", value: (ushort) 73),
                    c => c.VerifyField(name: "snSymRecs", value: (ushort) 74),
                    c => c.VerifyByteBlob(offset: 300038, new byte[] {0, 0}),
                    c => c.VerifyField(name: "cbGpModi", value: 7232),
                    c => c.VerifyField(name: "cbSC", value: 9260),
                    c => c.VerifyField(name: "cbSecMap", value: 124),
                    c => c.VerifyField(name: "cbFileInfo", value: 1192)
                )
            );
        }

        [TestMethod]
        public void NewDBIHdr_Test()
        {
            TestStruct<NewDBIHdr>(
                v => v.verSignature == -1,
                v => v.verHdr == DBIImpv.DBIImpvV60,
                v => v.age == 1,
                v => v.snGSSyms == 6,
                v => v.usVerAll == default(DbiHdrVersion),
                v => v.snPSSyms == 7,
                v => v.usVerPdbDllBuild == 0,
                v => v.snSymRecs == 73,
                v => v.usVerPdbDllRBld == 0,
                v => v.cbGpModi == 10716,
                v => v.cbSC == 15600,
                v => v.cbSecMap == 84,
                v => v.cbFileInfo == 1136,
                v => v.cbTSMap == 392,
                v => v.iMFC == 0,
                v => v.cbDbgHdr == 12,
                v => v.cbECInfo == 25,
                v => v.flags.fIncLink == false,
                v => v.flags.fStripped == false,
                v => v.flags.fCTypes == false,
                v => v.flags.unused == 0,
                v => v.wMachine == IMAGE_FILE_MACHINE_UNKNOWN,
                v => v.rgulReserved == 0
            );

            TestView<NewDBIHdr>(
                v => v.VerifyStruct(
                    name: "NewDBIHdr", offset: 275456, size: 64,
                    c => c.VerifyField(name: "verSignature", value: -1),
                    c => c.VerifyField(name: "verHdr", value: DBIImpv.DBIImpvV60),
                    c => c.VerifyField(name: "age", value: 1),
                    c => c.VerifyField(name: "snGSSyms", value: (ushort) 6),
                    c => c.VerifyBitField(name: "usVerPdbDllRbld", value: (byte) 0, bits: 4),
                    c => c.VerifyBitField(name: "usVerPdbDllMin", value: (byte) 0, bits: 7),
                    c => c.VerifyBitField(name: "usVerPdbDllMaj", value: (byte) 0, bits: 5),
                    c => c.VerifyField(name: "snPSSyms", value: (ushort) 7),
                    c => c.VerifyField(name: "usVerPdbDllBuild", value: (ushort) 0),
                    c => c.VerifyField(name: "snSymRecs", value: (ushort) 73),
                    c => c.VerifyField(name: "usVerPdbDllRBld", value: (ushort) 0),
                    c => c.VerifyField(name: "cbGpModi", value: 10716),
                    c => c.VerifyField(name: "cbSC", value: 15600),
                    c => c.VerifyField(name: "cbSecMap", value: 84),
                    c => c.VerifyField(name: "cbFileInfo", value: 1136),
                    c => c.VerifyField(name: "cbTSMap", value: 392),
                    c => c.VerifyField(name: "iMFC", value: 0),
                    c => c.VerifyField(name: "cbDbgHdr", value: 12),
                    c => c.VerifyField(name: "cbECInfo", value: 25),
                    c => c.VerifyBitField(name: "fIncLink", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "fStripped", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "fCTypes", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "unused", value: (ushort) 0, bits: 13),
                    c => c.VerifyField(name: "wMachine", value: IMAGE_FILE_MACHINE_UNKNOWN),
                    c => c.VerifyField(name: "rgulReserved", value: 0)
                )
            );
        }

        [TestMethod]
        public void Modi_Test()
        {
            TestStruct<Modi>(
                v => v.pmod == 0,
                v => v.flags.fWritten == false,
                v => v.flags.unused == 0,
                v => v.sn == 4,
                v => v.cbSyms == 272,
                v => v.cbLines == 100,
                v => v.cbFpo == 0,
                v => v.iFileMac == 1,
                v => v.mpifileichFile == 16440800,
                v => v.szModule == ".\\Debug\\main.obj",
                v => v.szObjFile == ".\\Debug\\main.obj"
            );

            TestView<Modi>(
                WithIgnores(
                    before: 13,
                    v => v.VerifyStruct(
                        name: "MODI (v4)", offset: 300056, size: 84,
                        c => c.VerifyField(name: "pmod", value: (uint) 0),
                        c => c.VerifyFieldIgnoreValue(name: "sc"),
                        c => c.VerifyBitField(name: "fWritten", value: (byte) 0, bits: 1),
                        c => c.VerifyBitField(name: "unused", value: (ushort) 0, bits: 15),
                        c => c.VerifyField(name: "sn", value: (ushort) 4),
                        c => c.VerifyField(name: "cbSyms", value: 272),
                        c => c.VerifyField(name: "cbLines", value: 100),
                        c => c.VerifyField(name: "cbFpo", value: 0),
                        c => c.VerifyField(name: "iFileMac", value: (ushort) 1),
                        c => c.VerifyByteBlob(offset: 300098, new byte[] { 0, 0 }),
                        c => c.VerifyField(name: "mpifileichFile", value: 16440800),
                        c => c.VerifyField(name: "szModule", value: ".\\Debug\\main.obj"),
                        c => c.VerifyField(name: "szObjFile", value: ".\\Debug\\main.obj"),
                        c => c.VerifyByteBlob(offset: 300138, new byte[] { 0, 0 })
                    )
                )
            );
        }

        [TestMethod]
        public void Modi20_Test()
        {
            TestStruct<Modi20>(
                v => v.pmod == 9303804,
                v => v.flags.fWritten == false,
                v => v.flags.unused == 0,
                v => v.sn == 5,
                v => v.cbSyms == 272,
                v => v.cbLines == 84,
                v => v.cbFpo == 0,
                v => v.iFileMac == 1,
                v => v.mpifileichFile == 9304024,
                v => v.szModule == ".\\WinDebug\\main.obj",
                v => v.szObjFile == ".\\WinDebug\\main.obj"
            );

            TestView<Modi20>(
                WithIgnores(
                    before: 13,
                    v => v.VerifyStruct(
                        name: "MODI (v2)", offset: 86040, size: 84,
                        c => c.VerifyField(name: "pmod", value: (uint) 9303804),
                        c => c.VerifyFieldIgnoreValue(name: "sc"),
                        c => c.VerifyBitField(name: "fWritten", value: (byte) 0, bits: 1),
                        c => c.VerifyBitField(name: "unused", value: (ushort) 0, bits: 15),
                        c => c.VerifyField(name: "sn", value: (ushort) 5),
                        c => c.VerifyField(name: "cbSyms", value: 272),
                        c => c.VerifyField(name: "cbLines", value: 84),
                        c => c.VerifyField(name: "cbFpo", value: 0),
                        c => c.VerifyField(name: "iFileMac", value: (ushort) 1),
                        c => c.VerifyByteBlob(offset: 86078, new byte[] { 0, 0 }),
                        c => c.VerifyField(name: "mpifileichFile", value: 9304024),
                        c => c.VerifyField(name: "szModule", value: ".\\WinDebug\\main.obj"),
                        c => c.VerifyField(name: "szObjFile", value: ".\\WinDebug\\main.obj")
                    )
                )
            );
        }

        [TestMethod]
        public void Modi50_Test()
        {
            TestStruct<Modi50>(
                v => v.pmod == 0,
                 v => v.flags.fWritten == false,
                v => v.flags.unused == 0,
                v => v.flags.iTSM == 0,
                v => v.sn == 4,
                v => v.cbSyms == 284,
                v => v.cbLines == 76,
                v => v.cbFpo == 0,
                v => v.iFileMac == 1,
                v => v.mpifileichFile == 47049056,
                v => v.szModule == "main.obj",
                v => v.szObjFile == "main.obj"
            );

            TestView<Modi50>(
                WithIgnores(
                    before: 13,
                    v => v.VerifyStruct(
                        name: "MODI50", offset: 73792, size: 68,
                        c => c.VerifyField(name: "pmod", value: (uint) 0),
                        c => c.VerifyFieldIgnoreValue(name: "sc"),
                        c => c.VerifyBitField(name: "fWritten", value: (byte) 0, bits: 1),
                        c => c.VerifyBitField(name: "unused", value: (byte) 0, bits: 7),
                        c => c.VerifyBitField(name: "iTSM", value: (byte) 0, bits: 8),
                        c => c.VerifyField(name: "sn", value: (ushort) 4),
                        c => c.VerifyField(name: "cbSyms", value: 284),
                        c => c.VerifyField(name: "cbLines", value: 76),
                        c => c.VerifyField(name: "cbFpo", value: 0),
                        c => c.VerifyField(name: "iFileMac", value: (ushort) 1),
                        c => c.VerifyByteBlob(offset: 73834, new byte[] {0, 0}),
                        c => c.VerifyField(name: "mpifileichFile", value: 47049056),
                        c => c.VerifyField(name: "szModule", value: "main.obj"),
                        c => c.VerifyField(name: "szObjFile", value: "main.obj"),
                        c => c.VerifyByteBlob(offset: 73858, new byte[] { 0, 0 })
                    )
                )
            );
        }

        [TestMethod]
        public void Modi60_Test()
        {
            TestStruct<Modi60>(
                v => v.pmod == 0,
                v => v.flags.fWritten == false,
                v => v.flags.fECEnabled == false,
                v => v.flags.unused == 0,
                v => v.flags.iTSM == 1,
                v => v.sn == 9,
                v => v.cbSyms == 248,
                v => v.cbLines == 124,
                v => v.cbC13Lines == 0,
                v => v.ifileMac == 1,
                v => v.mpifileichFile == 52372024,
                v => v.szModule == ".\\Debug\\TestApp.obj",
                v => v.szObjFile == ".\\Debug\\TestApp.obj"
            );

            TestView<Modi60>(
                WithIgnores(
                    before: 7,
                    v => v.VerifyStruct(
                        name: "MODI_60_Persist", offset: 275520, size: 104,
                        c => c.VerifyField(name: "pmod", value: 0),
                        c => c.VerifyFieldIgnoreValue(name: "sc"),
                        c => c.VerifyBitField(name: "fWritten", value: (byte) 0, bits: 1),
                        c => c.VerifyBitField(name: "fECEnabled", value: (byte) 0, bits: 1),
                        c => c.VerifyBitField(name: "unused", value: (byte) 0, bits: 6),
                        c => c.VerifyBitField(name: "iTSM", value: (byte) 1, bits: 8),
                        c => c.VerifyField(name: "sn", value: (ushort) 9),
                        c => c.VerifyField(name: "cbSyms", value: 248),
                        c => c.VerifyField(name: "cbLines", value: 124),
                        c => c.VerifyField(name: "cbC13Lines", value: 0),
                        c => c.VerifyField(name: "ifileMac", value: (ushort) 1),
                        c => c.VerifyByteBlob(offset: 275570, new byte[] { 0, 0 }),
                        c => c.VerifyField(name: "mpifileichFile", value: 52372024),
                        c => c.VerifyStructFieldIgnoreChildren(name: "ecInfo", type: "ECInfo", offset: 275576, size: 8),
                        c => c.VerifyField(name: "szModule", value: ".\\Debug\\TestApp.obj"),
                        c => c.VerifyField(name: "szObjFile", value: ".\\Debug\\TestApp.obj")
                    )
                )
            );
        }

        [TestMethod]
        public void DbgDataHdr_Test()
        {
            TestStruct<DbgDataHdr>(
                v => v.FPO == PDB.SN.Nil,
                v => v.Exception == PDB.SN.Nil,
                v => v.Fixup == PDB.SN.Nil,
                v => v.OmapToSrc == PDB.SN.Nil,
                v => v.OmapFromSrc == PDB.SN.Nil,
                v => v.SectionHdr == 11,
                v => v.TokenRidMap == PDB.SN.Nil,
                v => v.XData == PDB.SN.Nil,
                v => v.PData == PDB.SN.Nil,
                v => v.NewFPO == 13,
                v => v.SectionHdrOrig == PDB.SN.Nil
            );

            TestView<DbgDataHdr>(
                v => v.VerifyStruct(
                    name: "DbgDataHdr", offset: 66260, size: 22,
                    c => c.VerifyField(name: "FPO", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "Exception", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "Fixup", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "OmapToSrc", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "OmapFromSrc", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "SectionHdr", value: (ushort) 11),
                    c => c.VerifyField(name: "TokenRidMap", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "XData", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "PData", value: PDB.SN.Nil),
                    c => c.VerifyField(name: "NewFPO", value: (ushort) 13),
                    c => c.VerifyField(name: "SectionHdrOrig", value: PDB.SN.Nil)
                )
            );
        }

        [TestMethod]
        public void CvDebugSSubsectionHeader_Test()
        {
            TestStruct<CvDebugSSubsectionHeader>(
                v => v.Type == DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES,
                v => v.Length == 0x30
            );

            TestView<CvDebugSSubsectionHeader>(
                v => v.VerifyStruct(
                    name: "CV_DebugSSubsectionHeader_t", offset: 37164, size: 56,
                    c => c.VerifyField(name: "type", value: DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES),
                    c => c.VerifyField(name: "cbLen", value: 0x30),
                    c => c.VerifyStructIgnoreChildren(name: "CV_DebugSLinesHeader_t", offset: 0x9134, size: 48)
                )
            );
        }

        [TestMethod]
        public void NMT_Test()
        {
            TestStruct<NMT>(
                v => v.NameBufferSize == 70,
                v => v.NumOffsets == 4,
                v => v.NumStrings == 3
            );

            TestView<NMT>(
                v => v.VerifyStruct(
                    name: "Name Table", offset: 73728, size: 106,
                    c => c.VerifyStructIgnoreChildren(name: "VHdr", offset: 0x12000, size: 8),
                    c => c.VerifyField(name: "Name Buffer Size", value: 70),
                    c => c.VerifyValue(offset: 0x1200c, value: ""),
                    c => c.VerifyValue(offset: 0x1200d, value: ""),
                    c => c.VerifyValue(offset: 0x1200e, value: "C:\\TestApp\\TestApp.cpp"),
                    c => c.VerifyValue(offset: 0x12025, value: "$T0 .raSearch = $eip $T0 ^ = $esp $T0 4 + = "),
                    c => c.VerifyField(name: "Num Offsets", value: 4),
                    c => c.VerifyFieldIgnoreValue(name: "Offsets"),
                    c => c.VerifyField(name: "Num Strings", value: 3)
                )
            );
        }

        [TestMethod]
        public void GSI_Test_V7()
        {
            using var pdbFile = PDBFile.FromKey(WellKnownTestModule.ntdll);

            var writer = new ViewWriter(pdbFile);

            ((IViewable) pdbFile.GSI).WriteGlobals(writer);
        }

        [TestMethod]
        public void GSI_Test_V2()
        {
            using var pdbFile = PDBFile.FromFile(Sample.VC40_PDB);

            var writer = new ViewWriter(pdbFile);

            ((IViewable) pdbFile.GSI).WriteGlobals(writer);
        }

        [TestMethod]
        public void PSGSI_PSGSI_Thunk_PDB2()
        {
            using var pdbFile = PDBFile.FromFile(Sample.VC20_PDB);

            var psgsi = pdbFile.PSGSI;

            //Has 3216t entries
            var thunkEntries = psgsi.ThunkEntries;

            Assert.AreEqual(1, thunkEntries.Length);

            var entry = thunkEntries[0];

            Assert.AreEqual("_main", entry.Target.SymType.ToString());
            Assert.AreEqual("@ILT+0(_main)", entry.Thunk.ToString());
        }

        [TestMethod]
        public void OMAP_Test()
        {
            var path = Locator.LocatePDB(WellKnownTestModule.ntdllWin7);

            using var pdbFile = PDBFile.FromFile(path);

            var omapFromSrc = pdbFile.DBI.OmapFromSrc;
            var omapToSrc = pdbFile.DBI.OmapToSrc;

            Assert.AreEqual(67620, omapFromSrc.Value.Length);
            Assert.AreEqual(67096, omapToSrc.Value.Length);
        }

        #endregion
        #region TPI

        [TestMethod]
        public void HDR_Test()
        {
            TestStruct<HDR>(
                v => v.vers == TPIImpv.impv50,
                v => v.cbHdr == 56,
                v => v.tiMin == 0x1000,
                v => v.tiMac == 0x1005,
                v => v.cbGprec == 72
            );

            TestView<HDR>(
                v => v.VerifyStruct(
                    name: "HDR", offset: 305152, size: 56,
                    c => c.VerifyField(name: "vers", value: TPIImpv.impv50),
                    c => c.VerifyField(name: "cbHdr", value: 56),
                    c => c.VerifyField(name: "tiMin", value: 0x1000),
                    c => c.VerifyField(name: "tiMac", value: 0x1005),
                    c => c.VerifyField(name: "cbGprec", value: 72),
                    c => c.VerifyFieldIgnoreValue(name: "tpihash")
                )
            );
        }

        [TestMethod]
        public void HDR_16t_Test()
        {
            TestStruct<HDR_16t>(
                v => v.vers == TPIImpv.impv40,
                v => v.tiMin == 4096,
                v => v.tiMac == 4511,
                v => v.cbGprec == 8280,
                v => v.snHash == (ushort) 75
            );

            TestView<HDR_16t>(
                v => v.VerifyStruct(
                    name: "HDR_16t", offset: 327680, size: 16,
                    c => c.VerifyField(name: "vers", value: TPIImpv.impv40),
                    c => c.VerifyField(name: "tiMin", value: (ushort) 4096),
                    c => c.VerifyField(name: "tiMac", value: (ushort) 4511),
                    c => c.VerifyField(name: "cbGprec", value: 8280),
                    c => c.VerifyField(name: "snHash", value: (ushort) 75),
                    c => c.VerifyByteBlob(offset: 327694, new byte[] {0, 0})
                )
            );
        }

        #endregion
    }
}
