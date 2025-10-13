using System;
using System.Linq;
using ClrDebug.OMF;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.PDB;

namespace PESpy.Tests
{
    [TestClass]
    public class SymbolTests
    {
        #region DNRB

        [TestMethod]
        public void Symbols_DNRB()
        {
            //DNRB has 5 fixed sections

            using var dosFile = DOSFile.FromFile(Sample.C400_EXE);

            var data = (DNRBData) dosFile.CodeViewData;

            //Section 0
            Assert.AreEqual(29, data.Modules.Length);
            Assert.AreEqual("TESTAPP.OBJ", data.Modules[0].ToString());

            //Section 1
            Assert.AreEqual(86, data.Publics.Value.Length);
            Assert.AreEqual("_main", data.Publics.Value[0].ToString());

            //Section 2
            Assert.AreEqual(17, data.Types.Value.Length);
            Assert.AreEqual("OLF_STRUCTURE", data.Types.Value[0].ToString());

            //Section 3
            Assert.AreEqual(6, data.Symbols.Value.Length);
            Assert.AreEqual("S_PROC", data.Symbols.Value[0].ToString());

            //Section 4
            Assert.AreEqual(1, data.SourceLines.Value.Length);
            Assert.AreEqual("C:\\TESTAPP.C", data.SourceLines.Value[0].ToString());
        }

        #endregion
        #region NB02

        [TestMethod]
        public void Symbols_NB02_SSTMODULE()
        {
            WithNB02<smd>(SST.SSTMODULE, v =>
            {
                Assert.AreEqual("testapp.obj", v.ToString());
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTPUBLIC()
        {
            WithNB02<RawValue<pbi[]>>(SST.SSTPUBLIC, v =>
            {
                Assert.AreEqual(1, v.Value.Length);
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTTYPES()
        {
            WithNB02<RawValue<OldTypType[]>>(SST.SSTTYPES, v =>
            {
                Assert.AreEqual(76, v.Value.Length);
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTSYMBOLS()
        {
            WithNB02<RawValue<OldSymType[]>>(SST.SSTSYMBOLS, v =>
            {
                Assert.AreEqual(6, v.Value.Length);
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTSRCLINES()
        {
            WithNB02<RawValue<loe[]>>(SST.SSTSRCLINES, v =>
            {
                Assert.AreEqual(1, v.Value.Length);

                var item = v.Value[0];

                Assert.AreEqual("TESTAPP.C", item.ToString());
                Assert.IsNull(item.Seg);
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTLIBRARIES()
        {
            WithNB02<RawValue<FixedAnsiString[]>>(SST.SSTLIBRARIES, v =>
            {
                Assert.AreEqual(2, v.Value.Length);
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTIMPORTS()
        {
            WithNB02<object>(SST.SSTIMPORTS, v =>
            {
                throw new NotImplementedException();
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTCOMPACTED()
        {
            WithNB02<object>(SST.SSTCOMPACTED, v =>
            {
                throw new NotImplementedException();
            });
        }

        [TestMethod]
        public void Symbols_NB02_SSTSRCLNSEG()
        {
            WithNB02<RawValue<loe[]>>(SST.SSTSRCLNSEG, v =>
            {
                Assert.AreEqual(1, v.Value.Length);

                var item = v.Value[0];

                Assert.AreEqual("testapp.c", item.ToString());
                Assert.AreEqual((ushort) 0, item.Seg);
            });
        }

        #endregion
        #region NB05

        [TestMethod]
        public void Symbols_NB05_sstModule()
        {
            WithNB05<OMFModule>(SST.sstModule, v =>
            {
                Assert.AreEqual(".\\Debug\\main.obj", v.ToString());
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstTypes()
        {
            WithNB05<OMFModuleTypes>(SST.sstTypes, v =>
            {
                Assert.AreEqual(5, v.List.Count);
            });
        }

        #region Not Implemented

        [TestMethod]
        public void Symbols_NB05_sstPublic()
        {
            WithNB05<object>(SST.sstPublic, v =>
            {
                throw new NotImplementedException();
            });
        }

        #endregion

        [TestMethod]
        public void Symbols_NB05_sstPublicSym()
        {
            WithNB05<OMFModuleSymbols>(SST.sstPublicSym, v =>
            {
                var pubSym = (DataSym16) v.List[0];

                Assert.AreEqual("_main", pubSym.ToString());

                //I haven't been able to figure out how DOS segments work
                Assert.ThrowsException<NotImplementedException>(
                    () => _ = pubSym.RelativeVirtualAddress.Value
                );
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstSymbols()
        {
            WithNB05<OMFModuleSymbols>(SST.sstSymbols, v =>
            {
                var procSym = (ProcSym16) v.List[0];
                Assert.AreEqual("main", procSym.ToString());

                //I haven't been able to figure out how DOS segments work
                Assert.ThrowsException<NotImplementedException>(
                    () => _ = procSym.RelativeVirtualAddress.Value
                );
            });
        }

        #endregion

        [TestMethod]
        public void Symbols_NB05_sstAlignSym()
        {
            WithNB05<OMFModuleSymbols>(SST.sstAlignSym, v =>
            {
                var procSym = (ProcSym32) v.List[3];
                Assert.AreEqual("main", procSym.ToString());
                Assert.AreEqual(0x1000, procSym.RelativeVirtualAddress.Value);
            });
        }

        #region Not Implemented

        [TestMethod]
        public void Symbols_NB05_sstSrcLnSeg()
        {
            //This is probably the same format as NB02 SSTSRCLNSEG, we just don't know what struct to use!

            WithNB05<object>(SST.sstSrcLnSeg, v =>
            {
                throw new NotImplementedException();
            });
        }

        #endregion

        [TestMethod]
        public void Symbols_NB05_sstSrcModule()
        {
            WithNB05<OMFSourceModule>(SST.sstSrcModule, v =>
            {
                Assert.AreEqual(1, v.baseSrcFile.Length);
                Assert.AreEqual("C:\\Program Files (x86)\\DevStudio\\MyProjects\\TestApp\\main.cpp", v.baseSrcFile[0].ToString());
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstLibraries()
        {
            WithNB05<FixedAnsiString[]>(SST.sstLibraries, v =>
            {
                Assert.AreEqual(15, v.Length);
                Assert.AreEqual(string.Empty, v[0].ToString());
                Assert.AreEqual("C:\\Program Files (x86)\\DevStudio\\VC\\LIB\\kernel32.lib", v[1].ToString());
                Assert.AreEqual("C:\\Program Files (x86)\\DevStudio\\VC\\LIB\\OLDNAMES.lib", v[14].ToString());
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstGlobalSym()
        {
            WithNB05<OMFHashedSymbols>(SST.sstGlobalSym, v =>
            {
                Assert.AreEqual(449, v.Symbols.Count);

                var refSym = (RefSym) v.Symbols[5];
                Assert.AreEqual("__crtMessageBoxA", refSym.ToString());

                var procSym = (ProcSym32) refSym.Symbol;
                Assert.AreEqual("__crtMessageBoxA", refSym.ToString());
                Assert.AreEqual(0x7790, procSym.RelativeVirtualAddress.Value);
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstGlobalPub()
        {
            WithNB05<OMFHashedSymbols>(SST.sstGlobalPub, v =>
            {
                Assert.AreEqual(458, v.Symbols.Count);

                var pubSym = (PubSym32) v.Symbols[5];

                Assert.AreEqual("__heapchk", pubSym.ToString());
                Assert.AreEqual(0x9080, pubSym.RelativeVirtualAddress.Value);
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstGlobalTypes()
        {
            WithNB05<OMFGlobalTypes>(SST.sstGlobalTypes, v =>
            {
                var typType = v[15];

                Assert.AreEqual("_iobuf", typType.ToString());
            });
        }

        #region Not Implemented

        [TestMethod]
        public void Symbols_NB05_sstMPC()
        {
            WithNB05<object>(SST.sstMPC, v =>
            {
                throw new NotImplementedException();
            });
        }

        #endregion

        [TestMethod]
        public void Symbols_NB05_sstSegMap()
        {
            WithNB05<OMFSegMap>(SST.sstSegMap, v =>
            {
                Assert.AreEqual(5, v.rgDesc.Length);
                Assert.AreEqual(-1, v.rgDesc[0].iSegName);
                Assert.AreEqual(-1, v.rgDesc[0].iClassName);
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstSegName()
        {
            WithNB05<AnsiString[]>(SST.sstSegName, v =>
            {
                Assert.AreEqual(87, v.Length);
                Assert.AreEqual("_TEXT", v[0].ToString());
            });
        }

        #region Not Implemented

        [TestMethod]
        public void Symbols_NB05_sstPreComp()
        {
            WithNB05<object>(SST.sstPreComp, v =>
            {
                throw new NotImplementedException();
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstPreCompMap()
        {
            WithNB05<object>(SST.sstPreCompMap, v =>
            {
                throw new NotImplementedException();
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstOffsetMap16()
        {
            WithNB05<object>(SST.sstOffsetMap16, v =>
            {
                throw new NotImplementedException();
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstOffsetMap32()
        {
            WithNB05<object>(SST.sstOffsetMap32, v =>
            {
                throw new NotImplementedException();
            });
        }

        #endregion

        [TestMethod]
        public void Symbols_NB05_sstFileIndex()
        {
            WithNB05<OMFFileIndex>(SST.sstFileIndex, v =>
            {
                var name = v.FileNames[0][0];

                Assert.AreEqual("C:\\Program Files (x86)\\DevStudio\\MyProjects\\TestApp\\main.cpp", name.ToString());
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstStaticSym()
        {
            WithNB05<OMFHashedSymbols>(SST.sstStaticSym, v =>
            {
                var refSym = (RefSym) v.Symbols[0];
                Assert.AreEqual("_printMemBlockData", refSym.ToString());

                var procSym = (ProcSym32) refSym.Symbol;
                Assert.AreEqual("_printMemBlockData", procSym.ToString());
                Assert.AreEqual(0x6380, procSym.RelativeVirtualAddress.Value);
            });
        }

        private void WithNB02<T>(SST sst, Action<T> verify)
        {
            DOSFile dosFile = null;

            T GetData(string path)
            {
                dosFile = DOSFile.FromFile(path);

                var nb02 = (NB02Data) dosFile.CodeViewData;

                return (T) nb02.DirEntries.First(e => e.SubSection == sst).Data;
            }

            try
            {
                switch (sst)
                {
                    //We have 3 samples: C500 (NB00), BC7 (NB01) and C600 (NB02)

                    //Our comments list which sections our samples have
                    case SST.SSTMODULE:    //NB00, NB01, NB02
                    case SST.SSTPUBLIC:    //NB00, NB01, NB02
                    case SST.SSTTYPES:     //NB00, NB01, NB02
                    case SST.SSTSYMBOLS:   //NB00, NB01, NB02
                    case SST.SSTLIBRARIES: //NB00, NB01, NB02
                    case SST.SSTSRCLNSEG:  //NB01, NB02
                        verify(GetData(Sample.C600_Symbols_EXE)); //NB02
                        break;

                    case SST.SSTSRCLINES:  //NB00
                        verify(GetData(Sample.C500_EXE)); //NB00
                        break;

                    case SST.SSTIMPORTS:
                    case SST.SSTCOMPACTED: //Has the same format as sstType
                        throw new AssertInconclusiveException();
                }
            }
            finally
            {
                dosFile?.Dispose();
            }
        }

        private void WithNB05<T>(SST sst, Action<T> verify)
        {
            IFile file = null;

            T GetData(string path)
            {
                file = Detector.OpenFile(path);

                if (file is PEFile peFile)
                {
                    var nb05Data = (NB05Data) peFile.DebugTable.First(t => t.Type == ImageDebugType.CodeView).Data;

                    return (T) nb05Data.DirEntries.First(e => e.SubSection == sst).Data;
                }
                else
                {
                    var dosFile = (DOSFile) file;

                    var nb05Data = (NB05Data) dosFile.CodeViewData;

                    return (T) nb05Data.DirEntries.First(e => e.SubSection == sst).Data;
                }
            }

            try
            {
                switch (sst)
                {
                    case SST.sstModule:
                    case SST.sstAlignSym:
                    case SST.sstSrcModule:
                    case SST.sstLibraries:
                    case SST.sstGlobalSym:
                    case SST.sstGlobalPub:
                    case SST.sstGlobalTypes:
                    case SST.sstSegMap:
                    case SST.sstFileIndex:
                    case SST.sstStaticSym:
                        verify(GetData(Sample.VC50_EXE)); //NB11
                        break;

                    case SST.sstTypes:
                    case SST.sstPublicSym:
                    case SST.sstSymbols:
                    case SST.sstSegName:
                        verify(GetData(Sample.C700_Unpacked_EXE)); //NB05
                        break;

                    case SST.sstPublic:    //The linker fills each subsection of this type with entries for the public symbols of a module. The CVPACK utility combines all of the sstPublics subsections into an sstGlobalPub subsection. This table has been replaced with the sstPublicSym, but is retained for compatibility with previous linkers.
                    case SST.sstSrcLnSeg:  //The linker fills in each subsection of this type with information obtained from any LINNUM records in the module. This table has been replaced by the sstSrcModule, but is retained for compatibility with previous linkers. CVPACK rewrites sstSrcLnSeg tables to sstSrcModule tables.
                    case SST.sstMPC:       //Sounds related to VB: "This table is emitted by the Pcode MPC program when a segmented executable is processed into a non-segmented executable file. The table contains the mapping from segment indices to frame numbers."
                    case SST.sstPreComp:   //The linker emits one of these sections for every OMF object that has the $$TYPES table flagged as sstPreComp and for every COFF object that contains a .debug$P section. During packing, the CVPACK utility processes modules with a types table having the sstPreComp index before modules with types table having the sstTypes index.
                    case SST.sstPreCompMap: //Not listed in the spec
                    case SST.sstOffsetMap16: //Not listed in the spec
                    case SST.sstOffsetMap32: //Not listed in the spec
                        throw new AssertInconclusiveException();
                }
            }
            finally
            {
                file?.Dispose();
            }
        }
    }
}
