using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using ClrDebug;
using ClrDebug.DIA;
using ClrDebug.OMF;
using ClrDebug.PDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.OBJ;
using PESpy.PDB;
using PESpy.PowerShell;
using PESpy.View;
using PESpy.View.Builder;
using Stream = System.IO.Stream;

namespace PESpy.Tests
{
    [TestClass]
    public class SymbolTests : BaseTest
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
            //16-bit
            WithNB02<smd, smd32>(
                SST.SSTMODULE,
                _16: v => Assert.AreEqual("testapp.obj", v.ToString()),
                _32: v => Assert.AreEqual("ebios.obj", v.ToString())
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTPUBLIC()
        {
            //16-bit
            WithNB02<RawValue<pbi[]>, RawValue<pbi32[]>>(
                SST.SSTPUBLIC,
                _16: v =>
                {
                    Assert.AreEqual(1, v.Value.Length);

                    Assert.AreEqual("_main", v.Value[0].ToString());
                },
                _32: v =>
                {
                    Assert.AreEqual(12, v.Value.Length);

                    Assert.AreEqual("EBIOS_Device_Init", v.Value[11].ToString());
                }
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTTYPES()
        {
            //16-bit
            WithNB02<RawValue<OldTypType[]>>(
                SST.SSTTYPES,
                _16: v => Assert.AreEqual(76, v.Value.Length),
                _32: v => Assert.AreEqual(41, v.Value.Length)
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTSYMBOLS()
        {
            //16-bit
            WithNB02<RawValue<OldSymType[]>>(
                SST.SSTSYMBOLS,
                _16: v => Assert.AreEqual(6, v.Value.Length),
                _32: v => Assert.AreEqual(37, v.Value.Length)
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTSRCLINES()
        {
            //16-bit
            WithNB02<RawValue<loe[]>>(
                SST.SSTSRCLINES,
                _16: v =>
                {
                    Assert.AreEqual(1, v.Value.Length);

                    var item = v.Value[0];

                    Assert.AreEqual("TESTAPP.C", item.ToString());
                    Assert.IsNull(item.Seg);
                },
                _32: v => throw new NotImplementedException()
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTLIBRARIES()
        {
            //16-bit
            WithNB02<RawValue<SymString[]>>(
                SST.SSTLIBRARIES,
                _16: v => Assert.AreEqual(2, v.Value.Length),
                _32: v => throw new NotImplementedException()
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTIMPORTS()
        {
            //16-bit
            WithNB02<object>(
                SST.SSTIMPORTS,
                _16: v => throw new NotImplementedException(),
                _32: v => throw new NotImplementedException()
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTCOMPACTED()
        {
            //16-bit
            WithNB02<object>(
                SST.SSTCOMPACTED,
                _16: v => throw new NotImplementedException(),
                _32: v => throw new NotImplementedException()
            );
        }

        [TestMethod]
        public void Symbols_NB02_SSTSRCLNSEG()
        {
            //16-bit
            WithNB02<RawValue<loe[]>, RawValue<loe32[]>>(
                SST.SSTSRCLNSEG,
                _16: v =>
                {
                    Assert.AreEqual(1, v.Value.Length);

                    var item = v.Value[0];

                    Assert.AreEqual("testapp.c", item.ToString());
                    Assert.AreEqual((ushort) 0, item.Seg);
                },
                _32: v =>
                {
                    Assert.AreEqual(3, v.Value.Length);

                    var item = v.Value[0];

                    Assert.AreEqual("ebios.ASM", item.ToString());
                    Assert.AreEqual((ushort) 2, item.Seg);
                }
            );
        }

        private void WithNB02<T>(SST sst, Action<T> _16, Action<T> _32) =>
            WithNB02<T, T>(sst, _16, _32);

        private void WithNB02<T16, T32>(SST sst, Action<T16> _16, Action<T32> _32)
        {
            DOSFile dosFile = null;
            LEFile leFile = null;

            T16 GetData16(string path)
            {
                dosFile = DOSFile.FromFile(path);

                var fileView = dosFile.GetView();
                var verifier = new ViewAlignmentVerifier();
                fileView.Accept(verifier);

                var nb02 = (NB02Data) dosFile.CodeViewData;

                return (T16) nb02.DirEntries.First(e => e.SubSection == sst).Data;
            }

            T32 GetData32(string path)
            {
                leFile = LEFile.FromFile(path);

                var fileView = leFile.GetView();
                var verifier = new ViewAlignmentVerifier();
                fileView.Accept(verifier);

                var nb02 = (NB02Data) leFile.CodeViewData;

                return (T32) nb02.DirEntries.First(e => e.SubSection == sst).Data;
            }

            try
            {
                //16-bit
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
                        _16(GetData16(Sample.C600_Symbols_EXE)); //NB02
                        break;

                    case SST.SSTSRCLINES:  //NB00
                        _16(GetData16(Sample.C500_EXE)); //NB00
                        break;

                    case SST.SSTIMPORTS:
                    case SST.SSTCOMPACTED: //Has the same format as sstType
                        throw new AssertInconclusiveException();

                    default:
                        throw new NotImplementedException();
                }

                //32-bit
                switch (sst)
                {
                    case SST.SSTMODULE:
                    case SST.SSTPUBLIC:
                    case SST.SSTTYPES:
                    case SST.SSTSYMBOLS:
                    case SST.SSTIMPORTS:
                    case SST.SSTCOMPACTED:
                    case SST.SSTSRCLNSEG:
                        _32(GetData32(Sample.MASM5_NB00_VXD));
                        break;

                    case SST.SSTSRCLINES:
                    case SST.SSTLIBRARIES:
                        throw new AssertInconclusiveException();

                    default:
                        throw new NotImplementedException();
                }
            }
            finally
            {
                dosFile?.Dispose();
                leFile?.Dispose();
            }
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
            WithNB05<RawValue<SymString[]>>(SST.sstLibraries, v =>
            {
                Assert.AreEqual(15, v.Value.Length);
                Assert.AreEqual(string.Empty, v.Value[0].ToString());
                Assert.AreEqual("C:\\Program Files (x86)\\DevStudio\\VC\\LIB\\kernel32.lib", v.Value[1].ToString());
                Assert.AreEqual("C:\\Program Files (x86)\\DevStudio\\VC\\LIB\\OLDNAMES.lib", v.Value[14].ToString());
            });
        }

        [TestMethod]
        public void Symbols_NB05_sstGlobalSym()
        {
            WithNB05<OMFHashedSymbols>(SST.sstGlobalSym, v =>
            {
                Assert.AreEqual(448, v.Symbols.Count);

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
                Assert.AreEqual(457, v.Symbols.Count);

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
            WithNB05<RawValue<AnsiString[]>>(SST.sstSegName, v =>
            {
                Assert.AreEqual(87, v.Value.Length);
                Assert.AreEqual("_TEXT", v.Value[0].ToString());
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

        private void WithNB05<T>(SST sst, Action<T> verify)
        {
            IFile file = null;

            T GetData(string path)
            {
                file = Detector.OpenFile(path);

                if (file is PEFile peFile)
                {
                    var nb05Data = (NB05Data) peFile.DebugTable.First(t => t.Type == IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_CODEVIEW).Data;

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

        #endregion
        #region SymType

        [TestMethod]
        public void SymType_AlignSym_Test()
        {
            //I thought I had seen one of these in my con_samp.exe sample, but that can't be;
            //it doesn't have a PDB!
            var str = GenerateTest<AlignSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_AnnotationSym_Test()
        {
            var bytes = new byte[]
            {
                0x16, 0x00, 0x19, 0x10, 0x23, 0xB0, 0x08, 0x00, 0x01, 0x00, 0x01, 0x00, 0x4E, 0x4F, 0x5F, 0x43, 0x4F, 0x4E, 0x54, 0x52,
                0x41, 0x43, 0x54, 0x00
            };

            TestStruct<AnnotationSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 22),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_ANNOTATION),
                c => c.VerifyField(name: "off", value: 569379),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "csz", value: (short) 1)
            );
        }

        [TestMethod]
        public void SymType_ArmSwitchTable_Test()
        {
            var str = GenerateTest<ArmSwitchTable>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_AttrManyRegSym2_Test()
        {
            var str = GenerateTest<AttrManyRegSym2>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_AttrRegRel_Test()
        {
            var str = GenerateTest<AttrRegRel>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_AttrRegSym_Test()
        {
            var str = GenerateTest<AttrRegSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_AttrSlotSym_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x20, 0x11, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00,
                0x74, 0x68, 0x69, 0x73, 0x00, 0x00, 0x00, 0x00
            };

            TestStruct<AttrSlotSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 26),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_MANSLOT),
                c => c.VerifyField(name: "iSlot", value: 0),
                c => c.VerifyField(name: "typind", value: 0x11000004),
                c => c.VerifyField(name: "attr", value: new CV_lvar_attr { flags = { fIsParam = true } }),
                c => c.VerifyField(name: "name", value: "this"),
                c => c.VerifyByteBlob(offset: 4121, value: new byte[] { 0, 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_BlockSym16_Test()
        {
            var str = GenerateTest<BlockSym16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_BlockSym32_Test()
        {
            var bytes = new byte[]
            {
                0x16, 0x00, 0x03, 0x11, 0x04, 0x00, 0x00, 0x00, 0xBC, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00
            };

            TestStruct<BlockSym32>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 22),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_BLOCK32),
                c => c.VerifyField(name: "pParent", value: 4),
                c => c.VerifyField(name: "pEnd", value: 188),
                c => c.VerifyField(name: "len", value: 2),
                c => c.VerifyField(name: "off", value: 0),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "name", value: ""),
                c => c.VerifyByteBlob(offset: 4119, value: new byte[] { 0 })
            );
        }

        [TestMethod]
        public void SymType_BPRelSym16_Test()
        {
            var str = GenerateTest<BPRelSym16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_BPRelSym32_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x06, 0x10, 0x08, 0x00, 0x00, 0x00, 0x74, 0x00, 0x00, 0x00, 0x04, 0x61, 0x72, 0x67, 0x63, 0x00, 0x00, 0x00
            };

            TestStruct<BPRelSym32>(
                bytes,
                true,
                c => c.VerifyField(name: "reclen", value: (ushort) 18),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_BPREL32_ST),
                c => c.VerifyField(name: "off", value: 8),
                c => c.VerifyField(name: "typind", value: 0x74),
                c => c.VerifyField(name: "name", value: "argc"),
                c => c.VerifyByteBlob(offset: 4113, value: new byte[] { 0, 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_BPRelSym3216t_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x00, 0x02, 0x08, 0x00, 0x00, 0x00, 0x74, 0x00, 0x04, 0x61, 0x72, 0x67, 0x63, 0x00
            };

            TestStruct<BPRelSym3216t>(
                bytes,
                true,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_BPREL32_16t),
                c => c.VerifyField(name: "off", value: 8),
                c => c.VerifyField(name: "typind", value: (short) 0x74),
                c => c.VerifyField(name: "name", value: "argc"),
                c => c.VerifyByteBlob(offset: 4111, value: new byte[] { 0 })
            );
        }

        [TestMethod]
        public void SymType_BuildInfoSym_Test()
        {
            var bytes = new byte[]
            {
                0x06, 0x00, 0x4C, 0x11, 0x05, 0x10, 0x00, 0x00
            };

            TestStruct<BuildInfoSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 6),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_BUILDINFO),
                c => c.VerifyField(name: "id", value: (CV_ItemId) 0x1005)
            );
        }

        [TestMethod]
        public void SymType_CallSiteInfo_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x39, 0x11, 0x06, 0xA0, 0x08, 0x00, 0x01, 0x00, 0x00, 0x00, 0x79, 0x1F, 0x00, 0x00
            };

            TestStruct<CallSiteInfo>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_CALLSITEINFO),
                c => c.VerifyField(name: "off", value: 565254),
                c => c.VerifyField(name: "sect", value: (ISECT) 1),
                c => c.VerifyField(name: "__reserved_0", value: (short) 0),
                c => c.VerifyField(name: "typind", value: 0x1F79)
            );
        }

        [TestMethod]
        public void SymType_CExMSym16_Test()
        {
            var str = GenerateTest<CExMSym16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_CExMSym32_Test()
        {
            var str = GenerateTest<CExMSym32>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_CFlagSym_Test()
        {
            var bytes = new byte[]
            {
                0x4A, 0x00, 0x01, 0x00, 0x05, 0x01, 0x00, 0x00, 0x43, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20, 0x28,
                0x52, 0x29, 0x20, 0x33, 0x32, 0x2D, 0x62, 0x69, 0x74, 0x20, 0x43, 0x2F, 0x43, 0x2B, 0x2B, 0x20, 0x4F, 0x70, 0x74, 0x69,
                0x6D, 0x69, 0x7A, 0x69, 0x6E, 0x67, 0x20, 0x43, 0x6F, 0x6D, 0x70, 0x69, 0x6C, 0x65, 0x72, 0x20, 0x56, 0x65, 0x72, 0x73,
                0x69, 0x6F, 0x6E, 0x20, 0x31, 0x32, 0x2E, 0x30, 0x30, 0x2E, 0x38, 0x31, 0x36, 0x38, 0x2E, 0x30
            };

            TestStruct<CFlagSym>(
                bytes,
                true,
                c => c.VerifyField(name: "reclen", value: (ushort) 74),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_COMPILE),
                c => c.VerifyField(name: "machine", value: CV_CPU_TYPE_e.CV_CFL_PENTIUM),
                c => c.VerifyField(name: "language", value: CV_CFL_LANG.CV_CFL_CXX),
                c => c.VerifyBitField(name: "pcode", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "floatprec", value: (byte) 0, bits: 2),
                c => c.VerifyBitField(name: "floatpkg", value: CV_CFL_FPKG_e.CV_CFL_NDP, bits: 2),
                c => c.VerifyBitField(name: "ambdata", value: CV_CFL_DATA.CV_CFL_DNEAR, bits: 3),
                c => c.VerifyBitField(name: "ambcode", value: CV_CFL_CODE_e.CV_CFL_CNEAR, bits: 3),
                c => c.VerifyBitField(name: "mode32", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "pad", value: (byte) 0, bits: 4),
                c => c.VerifyField(name: "ver", value: "Microsoft (R) 32-bit C/C++ Optimizing Compiler Version 12.00.8168.0")
            );
        }

        [TestMethod]
        public void SymType_CoffGroupSym_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x37, 0x11, 0x03, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x2E, 0x74,
                0x65, 0x78, 0x74, 0x24, 0x6D, 0x6E, 0x00, 0x00
            };

            TestStruct<CoffGroupSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 26),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_COFFGROUP),
                c => c.VerifyField(name: "cb", value: 3),
                c => c.VerifyField(name: "characteristics", value: (IMAGE_SCN.CNT_CODE | IMAGE_SCN.MEM_EXECUTE | IMAGE_SCN.MEM_READ)),
                c => c.VerifyField(name: "off", value: 0x0),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "name", value: ".text$mn"),
                c => c.VerifyByteBlob(offset: 4123, value: new byte[] { 0 })
            );
        }

        [TestMethod]
        public void SymType_CompileSym_Test()
        {
            var bytes = new byte[]
            {
                0x2A, 0x00, 0x16, 0x11, 0x07, 0x00, 0x00, 0x00, 0xD0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E, 0x00, 0x1E, 0x00,
                0x4B, 0x78, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20, 0x28, 0x52, 0x29, 0x20, 0x4C, 0x49, 0x4E, 0x4B,
                0x00, 0x00, 0x00, 0x00
            };

            TestStruct<CompileSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 42),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_COMPILE2),
                c => c.VerifyBitField(name: "iLanguage", value: CV_CFL_LANG.CV_CFL_LINK, bits: 8),
                c => c.VerifyBitField(name: "fEC", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fNoDbgInfo", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fLTCG", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fNoDataAlign", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fManagedPresent", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fSecurityChecks", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fHotPatch", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fCVTCIL", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fMSILModule", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "pad", value: 0, bits: 15),
                c => c.VerifyField(name: "machine", value: CV_CPU_TYPE_e.CV_CFL_X64),
                c => c.VerifyField(name: "verFEMajor", value: (short) 0),
                c => c.VerifyField(name: "verFEMinor", value: (short) 0),
                c => c.VerifyField(name: "verFEBuild", value: (short) 0),
                c => c.VerifyField(name: "verMajor", value: (short) 14),
                c => c.VerifyField(name: "verMinor", value: (short) 30),
                c => c.VerifyField(name: "verBuild", value: (short) 30795),
                c => c.VerifyField(name: "verSt", value: "Microsoft (R) LINK"),
                c => c.VerifyByteBlob(offset: 4137, value: new byte[] { 0, 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_CompileSym3_Test()
        {
            var bytes = new byte[]
            {
                0x2E, 0x00, 0x3C, 0x11, 0x07, 0x00, 0x08, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E, 0x00,
                0x1D, 0x00, 0xBC, 0x75, 0x00, 0x00, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20, 0x28, 0x52, 0x29, 0x20,
                0x4C, 0x49, 0x4E, 0x4B, 0x00, 0x00, 0x00, 0x00
            };

            TestStruct<CompileSym3>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 46),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_COMPILE3),
                c => c.VerifyBitField(name: "iLanguage", value: CV_CFL_LANG.CV_CFL_LINK, bits: 8),
                c => c.VerifyBitField(name: "fEC", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fNoDbgInfo", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fLTCG", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fNoDataAlign", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fManagedPresent", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fSecurityChecks", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fHotPatch", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fCVTCIL", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fMSILModule", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fSdl", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fPGO", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fExp", value: (byte) 1, bits: 1),
                c => c.VerifyBitField(name: "pad", value: 0, bits: 12),
                c => c.VerifyField(name: "machine", value: CV_CPU_TYPE_e.CV_CFL_80386),
                c => c.VerifyField(name: "verFEMajor", value: (short) 0),
                c => c.VerifyField(name: "verFEMinor", value: (short) 0),
                c => c.VerifyField(name: "verFEBuild", value: (short) 0),
                c => c.VerifyField(name: "verFEQFE", value: (short) 0),
                c => c.VerifyField(name: "verMajor", value: (short) 14),
                c => c.VerifyField(name: "verMinor", value: (short) 29),
                c => c.VerifyField(name: "verBuild", value: (short) 30140),
                c => c.VerifyField(name: "verQFE", value: (short) 0),
                c => c.VerifyField(name: "verSz", value: "Microsoft (R) LINK"),
                c => c.VerifyByteBlob(offset: 4141, value: new byte[] { 0, 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_ConstSym_Test()
        {
            var bytes = new byte[]
            {
                0x2A, 0x00, 0x07, 0x11, 0x35, 0x14, 0x00, 0x00, 0x00, 0x00, 0x73, 0x74, 0x64, 0x3A, 0x3A, 0x5F, 0x49, 0x6E, 0x76, 0x6F,
                0x6B, 0x65, 0x72, 0x5F, 0x66, 0x75, 0x6E, 0x63, 0x74, 0x6F, 0x72, 0x3A, 0x3A, 0x5F, 0x53, 0x74, 0x72, 0x61, 0x74, 0x65,
                0x67, 0x79, 0x00, 0x00
            };

            TestStruct<ConstSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 42),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_CONSTANT),
                c => c.VerifyField(name: "typind", value: 0x1435),
                c => c.VerifyStructField(name: "value", type: "Numeric Data", offset: 0x1008, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x1008, value: (ushort) 0)
                }),
                c => c.VerifyField(name: "name", value: "std::_Invoker_functor::_Strategy"),
                c => c.VerifyByteBlob(offset: 4139, value: new byte[] { 0 })
            );
        }

        [TestMethod]
        public void SymType_ConstSym16t_Test()
        {
            var str = GenerateTest<ConstSym16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DataSym16_Test()
        {
            var str = GenerateTest<DataSym16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DataSym32_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x0D, 0x11, 0x25, 0x11, 0x00, 0x00, 0x50, 0x06, 0x00, 0x00, 0x02, 0x00, 0x5F, 0x5F, 0x6D, 0x6F, 0x64, 0x75,
                0x6C, 0x65, 0x73, 0x5F, 0x61, 0x00, 0x00, 0x00
            };

            TestStruct<DataSym32>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 26),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_GDATA32),
                c => c.VerifyField(name: "typind", value: 0x1125),
                c => c.VerifyField(name: "off", value: 1616),
                c => c.VerifyField(name: "seg", value: (ISECT) 2),
                c => c.VerifyField(name: "name", value: "__modules_a"),
                c => c.VerifyByteBlob(offset: 4122, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_DataSym3216t_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x03, 0x02, 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x05, 0x5F, 0x6D, 0x61, 0x69, 0x6E, 0x00, 0x00
            };

            TestStruct<DataSym3216t>(
                bytes,
                true,
                c => c.VerifyField(name: "reclen", value: (ushort) 18),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_PUB32_16t),
                c => c.VerifyField(name: "off", value: 16),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "typind", value: (short) 0x0),
                c => c.VerifyField(name: "name", value: "_main"),
                c => c.VerifyByteBlob(offset: 4114, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_DataSymHLSL_Test()
        {
            var str = GenerateTest<DataSymHLSL>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DataSymHLSL32_Test()
        {
            var str = GenerateTest<DataSymHLSL32>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DataSymHLSL32Ex_Test()
        {
            var str = GenerateTest<DataSymHLSL32Ex>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DefRangeSym_Test()
        {
            var str = GenerateTest<DefRangeSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DefRangeSymFramePointerRel_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x42, 0x11, 0x30, 0x00, 0x00, 0x00, 0x33, 0xA1, 0x08, 0x00, 0x01, 0x00, 0x68, 0x00
            };

            TestStruct<DefRangeSymFramePointerRel>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL),
                c => c.VerifyField(name: "offFramePointer", value: 48),
                c => c.VerifyFieldIgnoreValue(name: "range")
            );
        }

        [TestMethod]
        public void SymType_DefRangeSymFramePointerRelFullScope_Test()
        {
            var bytes = new byte[]
            {
                0x06, 0x00, 0x44, 0x11, 0x08, 0x00, 0x00, 0x00
            };

            TestStruct<DefRangeSymFramePointerRelFullScope>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 6),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE),
                c => c.VerifyField(name: "offFramePointer", value: 0x8)
            );
        }

        [TestMethod]
        public void SymType_DefRangeSymHLSL_Test()
        {
            var str = GenerateTest<DefRangeSymHLSL>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DefRangeSymRegister_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x41, 0x11, 0x4A, 0x01, 0x00, 0x00, 0xA0, 0x10, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00
            };

            TestStruct<DefRangeSymRegister>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_DEFRANGE_REGISTER),
                c => c.VerifyField(name: "reg", value: CV_HREG_e.CV_ARM64_V20),
                c => c.VerifyField(name: "attr", value: new CV_RANGEATTR()),
                c => c.VerifyFieldIgnoreValue(name: "range")
            );
        }

        [TestMethod]
        public void SymType_DefRangeSymRegisterRel_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x45, 0x11, 0x4E, 0x01, 0x00, 0x00, 0xE4, 0xFF, 0xFF, 0xFF, 0xE1, 0x14, 0x00, 0x00, 0x01, 0x00, 0x63, 0x00
            };

            TestStruct<DefRangeSymRegisterRel>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 18),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_DEFRANGE_REGISTER_REL),
                c => c.VerifyField(name: "baseReg", value: CV_HREG_e.CV_REG_YMM4F2),
                c => c.VerifyBitField(name: "spilledUdtMember", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "padding", value: (short) 0, bits: 3),
                c => c.VerifyBitField(name: "offsetParent", value: (short) 0, bits: 12),
                c => c.VerifyField(name: "offBasePointer", value: -28),
                c => c.VerifyFieldIgnoreValue(name: "range")
            );
        }

        [TestMethod]
        public void SymType_DefRangeSymSubField_Test()
        {
            var str = GenerateTest<DefRangeSymSubField>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DefRangeSymSubfieldRegister_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x43, 0x11, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0xBB, 0x08, 0x00, 0x01, 0x00, 0x1D, 0x00
            };

            TestStruct<DefRangeSymSubfieldRegister>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 18),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_DEFRANGE_SUBFIELD_REGISTER),
                c => c.VerifyField(name: "reg", value: CV_HREG_e.CV_TRI_D10),
                c => c.VerifyField(name: "attr", value: new CV_RANGEATTR()),
                c => c.VerifyBitField(name: "offParent", value: (CV_uoff32_t) 0x0, bits: 12),
                c => c.VerifyBitField(name: "padding", value: (CV_uoff32_t) 0x0, bits: 20),
                c => c.VerifyFieldIgnoreValue(name: "range")
            );
        }

        [TestMethod]
        public void SymType_DiscardedSym_Test()
        {
            var str = GenerateTest<DiscardedSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_DPCSymTagMap_Test()
        {
            var str = GenerateTest<DPCSymTagMap>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_EntryThisSym_Test()
        {
            var str = GenerateTest<EntryThisSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_EnvBlockSym_Test()
        {
            var bytes = new byte[]
            {
                0x8A, 0x00, 0x3D, 0x11, 0x00, 0x63, 0x77, 0x64, 0x00, 0x43, 0x3A, 0x5C, 0x54, 0x65, 0x73, 0x74, 0x41, 0x70, 0x70, 0x00,
                0x65, 0x78, 0x65, 0x00, 0x43, 0x3A, 0x5C, 0x50, 0x72, 0x6F, 0x67, 0x72, 0x61, 0x6D, 0x20, 0x46, 0x69, 0x6C, 0x65, 0x73,
                0x20, 0x28, 0x78, 0x38, 0x36, 0x29, 0x5C, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20, 0x56, 0x69, 0x73,
                0x75, 0x61, 0x6C, 0x20, 0x53, 0x74, 0x75, 0x64, 0x69, 0x6F, 0x5C, 0x32, 0x30, 0x31, 0x39, 0x5C, 0x45, 0x6E, 0x74, 0x65,
                0x72, 0x70, 0x72, 0x69, 0x73, 0x65, 0x5C, 0x56, 0x43, 0x5C, 0x54, 0x6F, 0x6F, 0x6C, 0x73, 0x5C, 0x4D, 0x53, 0x56, 0x43,
                0x5C, 0x31, 0x34, 0x2E, 0x32, 0x39, 0x2E, 0x33, 0x30, 0x31, 0x33, 0x33, 0x5C, 0x62, 0x69, 0x6E, 0x5C, 0x48, 0x6F, 0x73,
                0x74, 0x58, 0x38, 0x36, 0x5C, 0x78, 0x38, 0x36, 0x5C, 0x6C, 0x69, 0x6E, 0x6B, 0x2E, 0x65, 0x78, 0x65, 0x00, 0x00, 0x00
            };

            TestStruct<EnvBlockSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 138),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_ENVBLOCK),
                c => c.VerifyBitField(name: "rev", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "pad", value: (byte) 0, bits: 7),
                c => c.VerifyFieldIgnoreValue(name: "rgsz")
            );
        }

        [TestMethod]
        public void SymType_ExportSym_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x38, 0x11, 0x01, 0x00, 0x00, 0x00, 0x5F, 0x6D, 0x61, 0x69, 0x6E, 0x00, 0x00, 0x00
            };

            TestStruct<ExportSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_EXPORT),
                c => c.VerifyField(name: "ordinal", value: (short) 1),
                c => c.VerifyBitField(name: "fConstant", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fData", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fPrivate", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fNoName", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fOrdinal", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fForwarder", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "reserved", value: (short) 0, 10),
                c => c.VerifyField(name: "name", value: "_main"),
                c => c.VerifyByteBlob(offset: 4110, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_FileStaticSym_Test()
        {
            var bytes = new byte[]
            {
                0x2E, 0x00, 0x53, 0x11, 0x6C, 0x16, 0x00, 0x00, 0x99, 0x75, 0x00, 0x00, 0x00, 0x06, 0x67, 0x5F, 0x52, 0x75, 0x6E, 0x74,
                0x69, 0x6D, 0x65, 0x49, 0x6E, 0x69, 0x74, 0x69, 0x61, 0x6C, 0x69, 0x7A, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x43, 0x61, 0x6C,
                0x6C, 0x62, 0x61, 0x63, 0x6B, 0x00, 0x00, 0x00
            };

            TestStruct<FileStaticSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 46),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_FILESTATIC),
                c => c.VerifyField(name: "typind", value: 0x166C),
                c => c.VerifyField(name: "modOffset", value: 30105),
                c => c.VerifyField(name: "flags", value: (short) 1536),
                c => c.VerifyField(name: "name", value: "g_RuntimeInitializationCallback"),
                c => c.VerifyByteBlob(offset: 4142, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_FrameCookie_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x3A, 0x11, 0x50, 0x02, 0x00, 0x00, 0x4F, 0x01, 0x01, 0x00
            };

            TestStruct<FrameCookie>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 10),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_FRAMECOOKIE),
                c => c.VerifyField(name: "off", value: 592),
                c => c.VerifyField(name: "reg", value: CV_HREG_e.CV_ARM64_V25),
                c => c.VerifyField(name: "cookietype", value: CV_cookietype_e.CV_COOKIETYPE_XOR_SP),
                c => c.VerifyField(name: "flags", value: (byte) 0)
            );
        }

        [TestMethod]
        public void SymType_FrameProcSym_Test()
        {
            var bytes = new byte[]
            {
                0x1E, 0x00, 0x12, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x12, 0x00, 0x00, 0x00
            };

            TestStruct<FrameProcSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 30),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_FRAMEPROC),
                c => c.VerifyField(name: "cbFrame", value: 0),
                c => c.VerifyField(name: "cbPad", value: 0),
                c => c.VerifyField(name: "offPad", value: 0),
                c => c.VerifyField(name: "cbSaveRegs", value: 0),
                c => c.VerifyField(name: "offExHdlr", value: 0),
                c => c.VerifyField(name: "sectExHdlr", value: (ISECT) 0),
                c => c.VerifyBitField(name: "fHasAlloca", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fHasSetJmp", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fHasLongJmp", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fHasInlAsm", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fHasEH", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fInlSpec", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fHasSEH", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fNaked", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fSecurityChecks", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fAsyncEH", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fGSNoStackOrdering", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fWasInlined", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fGSCheck", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fSafeBuffers", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "encodedLocalBasePointer", value: 2, bits: 2),
                c => c.VerifyBitField(name: "encodedParamBasePointer", value: 2, bits: 2),
                c => c.VerifyBitField(name: "fPogoOn", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fValidCounts", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fOptSpeed", value: (byte) 1, bits: 1),
                c => c.VerifyBitField(name: "fGuardCF", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "fGuardCFW", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "pad", value: 0, bits: 9),
                c => c.VerifyByteBlob(offset: 4126, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_FrameRelSym_Test()
        {
            var str = GenerateTest<FrameRelSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_FunctionList_Test()
        {
            var bytes = new byte[]
            {
                0x16, 0x00, 0x5A, 0x11, 0x04, 0x00, 0x00, 0x00, 0x1E, 0x10, 0x00, 0x00, 0x1F, 0x10, 0x00, 0x00, 0x20, 0x10, 0x00, 0x00,
                0x21, 0x10, 0x00, 0x00
            };

            TestStruct<FunctionList>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 22),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_CALLEES),
                c => c.VerifyField(name: "count", value: 4),
                c => c.VerifyFieldIgnoreValue(name: "funcs")
            );
        }

        [TestMethod]
        public void SymType_HeapAllocSite_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x5E, 0x11, 0xA9, 0xBC, 0x08, 0x00, 0x01, 0x00, 0x05, 0x00, 0xE0, 0x15, 0x00, 0x00
            };

            TestStruct<HeapAllocSite>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_HEAPALLOCSITE),
                c => c.VerifyField(name: "off", value: 572585),
                c => c.VerifyField(name: "sect", value: (ISECT) 1),
                c => c.VerifyField(name: "cbInstr", value: (short) 5),
                c => c.VerifyField(name: "typind", value: 0x15E0)
            );
        }

        [TestMethod]
        public void SymType_InlineSiteSym_Test()
        {
            var bytes = new byte[]
            {
                0x22, 0x00, 0x4D, 0x11, 0x98, 0x02, 0x00, 0x00, 0xCC, 0x03, 0x00, 0x00, 0x1D, 0x10, 0x00, 0x00, 0x06, 0x02, 0x03, 0x19,
                0x06, 0x12, 0x03, 0x0B, 0x06, 0x06, 0x0C, 0x49, 0x0C, 0x06, 0x12, 0x0C, 0x28, 0x50, 0x00, 0x00
            };

            TestStruct<InlineSiteSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 34),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_INLINESITE),
                c => c.VerifyField(name: "pParent", value: 664),
                c => c.VerifyField(name: "pEnd", value: 972),
                c => c.VerifyField(name: "inlinee", value: (CV_ItemId) 0x101D),
                c => c.VerifyFieldIgnoreValue(name: "binaryAnnotations")
            );
        }

        [TestMethod]
        public void SymType_InlineSiteSym2_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x5D, 0x11, 0xEC, 0x02, 0x00, 0x00, 0xC4, 0x03, 0x00, 0x00, 0x0A, 0x39, 0x00, 0x00, 0xB2, 0x51, 0x01, 0x00,
                0x06, 0x30, 0x0C, 0x04, 0x08, 0x00, 0x00, 0x00
            };

            TestStruct<InlineSiteSym2>(
                bytes,
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_INLINESITE2),
                c => c.VerifyField(name: "reclen", value: (ushort) 26),
                c => c.VerifyField(name: "pParent", value: 748),
                c => c.VerifyField(name: "pEnd", value: 964),
                c => c.VerifyField(name: "inlinee", value: (CV_ItemId) 0x390A),
                c => c.VerifyField(name: "invocations", value: 86450),
                c => c.VerifyFieldIgnoreValue(name: "binaryAnnotations")
            );
        }

        [TestMethod]
        public void SymType_LabelSym16_Test()
        {
            var str = GenerateTest<LabelSym16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_LabelSym32_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x05, 0x11, 0x1E, 0xB9, 0x08, 0x00, 0x01, 0x00, 0x10, 0x24, 0x4C, 0x4E, 0x33, 0x00
            };

            TestStruct<LabelSym32>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_LABEL32),
                c => c.VerifyField(name: "off", value: 571678),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "flags", value: (byte) 16),
                c => c.VerifyField(name: "name", value: "$LN3")
            );
        }

        [TestMethod]
        public void SymType_LocalDPCGroupSharedSym_Test()
        {
            var str = GenerateTest<LocalDPCGroupSharedSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_LocalSym_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x3E, 0x11, 0x74, 0x00, 0x00, 0x00, 0x01, 0x00, 0x61, 0x00
            };

            TestStruct<LocalSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 10),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_LOCAL),
                c => c.VerifyField(name: "typind", value: 0x74),
                c => c.VerifyField(name: "flags", value: (short) 1),
                c => c.VerifyField(name: "name", value: "a")
            );
        }

        [TestMethod]
        public void SymType_ManProcSym_Test()
        {
            var bytes = new byte[]
            {
                0x2E, 0x00, 0x2A, 0x11, 0x00, 0x00, 0x00, 0x00, 0xF4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                0x00, 0x4D, 0x61, 0x69, 0x6E, 0x00, 0x00, 0x00
            };

            TestStruct<ManProcSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 46),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_GMANPROC),
                c => c.VerifyField(name: "pParent", value: 0),
                c => c.VerifyField(name: "pEnd", value: 244),
                c => c.VerifyField(name: "pNext", value: 0),
                c => c.VerifyField(name: "len", value: 2),
                c => c.VerifyField(name: "DbgStart", value: 0),
                c => c.VerifyField(name: "DbgEnd", value: 0),
                c => c.VerifyField(name: "token", value: 100663297),
                c => c.VerifyField(name: "off", value: 0),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "flags", value: (byte) 0),
                c => c.VerifyField(name: "retReg", value: (short) 0),
                c => c.VerifyField(name: "name", value: "Main"),
                c => c.VerifyByteBlob(offset: 4142, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_ManTypRef_Test()
        {
            var str = GenerateTest<ManTypRef>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ManyRegSym_Test()
        {
            var str = GenerateTest<ManyRegSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ManyRegSym16t_Test()
        {
            var str = GenerateTest<ManyRegSym16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ManyRegSym2_Test()
        {
            var str = GenerateTest<ManyRegSym2>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ModTypeRef_Test()
        {
            var str = GenerateTest<ModTypeRef>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ObjNameSym_Test()
        {
            var bytes = new byte[]
            {
                0x1E, 0x00, 0x01, 0x11, 0x00, 0x00, 0x00, 0x00, 0x43, 0x3A, 0x5C, 0x54, 0x65, 0x73, 0x74, 0x41, 0x70, 0x70, 0x5C, 0x54,
                0x65, 0x73, 0x74, 0x41, 0x70, 0x70, 0x2E, 0x65, 0x78, 0x70, 0x00, 0x00
            };

            TestStruct<ObjNameSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 30),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_OBJNAME),
                c => c.VerifyField(name: "signature", value: 0),
                c => c.VerifyField(name: "name", value: "C:\\TestApp\\TestApp.exp"),
                c => c.VerifyByteBlob(offset: 4127, value: new byte[] { 0 })
            );
        }

        [TestMethod]
        public void SymType_OemSymbol_Test()
        {
            var bytes = new byte[]
            {
                0x32, 0x00, 0x04, 0x04, 0xC9, 0x3F, 0xEA, 0xC6, 0xB3, 0x59, 0xD6, 0x49, 0xBC, 0x25, 0x09, 0x02, 0xBB, 0xAB, 0xB4, 0x60,
                0x00, 0x00, 0x00, 0x00, 0x4D, 0x00, 0x44, 0x00, 0x32, 0x00, 0x00, 0x00, 0x04, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                0x10, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00
            };

            TestStruct<OemSymbol>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 50),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_OEM),
                c => c.VerifyField(name: "idOem", value: new Guid("c6ea3fc9-59b3-49d6-bc25-0902bbabb460")),
                c => c.VerifyField(name: "typind", value: 0x0),
                c => c.VerifyField("rgl", value: new byte[]
                {
                    //This starts with "MD2" but I don't know what it means
                    0x4D, 0x00, 0x44, 0x00, 0x32, 0x00, 0x00, 0x00, 0x04, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                    0x10, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00
                })
            );
        }

        [TestMethod]
        public void SymType_PdbMap_Test()
        {
            var str = GenerateTest<PdbMap>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_PogoInfo_Test()
        {
            var bytes = new byte[]
            {
                0x16, 0x00, 0x5C, 0x11, 0xF2, 0x63, 0x00, 0x00, 0x74, 0xC9, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00,
                0x0F, 0x00, 0x00, 0x00
            };

            TestStruct<PogoInfo>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 22),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_POGODATA),
                c => c.VerifyField(name: "invocations", value: 25586),
                c => c.VerifyField(name: "dynCount", value: (long) 2673012),
                c => c.VerifyField(name: "numInstrs", value: 15),
                c => c.VerifyField(name: "staInstLive", value: 15)
            );
        }

        [TestMethod]
        public void SymType_ProcSym16_Test()
        {
            var str = GenerateTest<ProcSym16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ProcSym32_Test()
        {
            var bytes = new byte[]
            {
                0x2A, 0x00, 0x10, 0x11, 0x00, 0x00, 0x00, 0x00, 0x20, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x80, 0x6D,
                0x61, 0x69, 0x6E, 0x00
            };

            TestStruct<ProcSym32>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 42),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_GPROC32),
                c => c.VerifyField(name: "pParent", value: 0),
                c => c.VerifyField(name: "pEnd", value: 288),
                c => c.VerifyField(name: "pNext", value: 0),
                c => c.VerifyField(name: "len", value: 3),
                c => c.VerifyField(name: "DbgStart", value: 0),
                c => c.VerifyField(name: "DbgEnd", value: 2),
                c => c.VerifyField(name: "typind", value: 0x1001),
                c => c.VerifyField(name: "off", value: 0x0),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "flags", value: (byte) 128),
                c => c.VerifyField(name: "name", value: "main")
            );
        }

        [TestMethod]
        public void SymType_ProcSym3216t_Test()
        {
            var bytes = new byte[]
            {
                0x2A, 0x00, 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0xB0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00,
                0x06, 0x00, 0x00, 0x00, 0x1A, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x10, 0x01, 0x04, 0x6D, 0x61,
                0x69, 0x6E, 0x00, 0x00
            };

            TestStruct<ProcSym3216t>(
                bytes,
                true,
                c => c.VerifyField(name: "reclen", value: (ushort) 42),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_GPROC32_16t),
                c => c.VerifyField(name: "pParent", value: 0),
                c => c.VerifyField(name: "pEnd", value: 176),
                c => c.VerifyField(name: "pNext", value: 0),
                c => c.VerifyField(name: "len", value: 31),
                c => c.VerifyField(name: "DbgStart", value: 6),
                c => c.VerifyField(name: "DbgEnd", value: 26),
                c => c.VerifyField(name: "off", value: 16),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "typind", value: (short) 0x1002),
                c => c.VerifyField(name: "flags", value: (byte) 1),
                c => c.VerifyField(name: "name", value: "main"),
                c => c.VerifyByteBlob(offset: 4138, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_ProcSymIA64_Test()
        {
            var str = GenerateTest<ProcSymIA64>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ProcSymMips_Test()
        {
            var str = GenerateTest<ProcSymMips>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ProcSymMips16t_Test()
        {
            var str = GenerateTest<ProcSymMips16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_PubSym32_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x0E, 0x11, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x5F, 0x6D, 0x61, 0x69, 0x6E, 0x00
            };

            TestStruct<PubSym32>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 18),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_PUB32),
                c => c.VerifyField(name: "pubsymflags", value: 2),
                c => c.VerifyField(name: "off", value: 0x0),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "name", value: "_main")
            );
        }

        [TestMethod]
        public void SymType_RefMiniPdb_Test()
        {
            var str = GenerateTest<RefMiniPdb>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_RefSym_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0xA0, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x6D, 0x61, 0x69,
                0x6E, 0x00, 0x00, 0x00
            };

            TestStruct<RefSym>(
                bytes,
                true,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_PROCREF_ST),
                c => c.VerifyField(name: "sumName", value: 0),
                c => c.VerifyField(name: "ibSym", value: 160),
                c => c.VerifyField(name: "imod", value: (ushort) 1),
                c => c.VerifyField(name: "usFill", value: (short) 0),
                c => c.VerifyField(name: "name", value: "main"),
                c => c.VerifyByteBlob(offset: 4117, value: new byte[] { 0, 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_RefSym2_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x25, 0x11, 0x00, 0x00, 0x00, 0x00, 0x9C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x6D, 0x61, 0x69, 0x6E, 0x00, 0x00
            };

            TestStruct<RefSym2>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 18),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_PROCREF),
                c => c.VerifyField(name: "sumName", value: 0),
                c => c.VerifyField(name: "ibSym", value: 156),
                c => c.VerifyField(name: "imod", value: (ushort) 2),
                c => c.VerifyField(name: "name", value: "main"),
                c => c.VerifyByteBlob(offset: 4115, value: new byte[] {0})
            );
        }

        [TestMethod]
        public void SymType_RegRel16_Test()
        {
            var str = GenerateTest<RegRel16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_RegRel32_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x11, 0x11, 0x08, 0x00, 0x00, 0x00, 0x74, 0x00, 0x00, 0x00, 0x16, 0x00, 0x61, 0x00
            };

            TestStruct<RegRel32>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_REGREL32),
                c => c.VerifyField(name: "off", value: 0x8),
                c => c.VerifyField(name: "typind", value: 0x74),
                c => c.VerifyField(name: "reg", value: CV_HREG_e.CV_ARM_R12),
                c => c.VerifyField(name: "name", value: "a")
            );
        }

        [TestMethod]
        public void SymType_RegRel3216t_Test()
        {
            var str = GenerateTest<RegRel3216t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_RegSym_Test()
        {
            var str = GenerateTest<RegSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_RegSym16t_Test()
        {
            var str = GenerateTest<RegSym16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ReturnSym_Test()
        {
            var str = GenerateTest<ReturnSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_SearchSym_Test()
        {
            var str = GenerateTest<SearchSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_SectionSym_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x36, 0x11, 0x01, 0x00, 0x0C, 0x00, 0x00, 0x10, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x60,
                0x2E, 0x74, 0x65, 0x78, 0x74, 0x00, 0x00, 0x00
            };

            TestStruct<SectionSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 26),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_SECTION),
                c => c.VerifyField(name: "isec", value: (short) 1),
                c => c.VerifyField(name: "align", value: (byte) 12),
                c => c.VerifyField(name: "bReserved", value: (byte) 0),
                c => c.VerifyField(name: "rva", value: 4096),
                c => c.VerifyField(name: "cb", value: 3),
                c => c.VerifyField(name: "characteristics", value: (IMAGE_SCN.CNT_CODE | IMAGE_SCN.MEM_EXECUTE | IMAGE_SCN.MEM_READ)),
                c => c.VerifyField(name: "name", value: ".text"),
                c => c.VerifyByteBlob(offset: 0x101A, new byte[] {0, 0})
            );
        }

        [TestMethod]
        public void SymType_SepCodeSym_Test()
        {
            var bytes = new byte[]
            {
                0x1E, 0x00, 0x32, 0x11, 0x00, 0x00, 0x00, 0x00, 0xE8, 0x2D, 0x00, 0x00, 0x29, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xDE, 0xAE, 0x1A, 0x00, 0xC4, 0xC9, 0x08, 0x00, 0x01, 0x00, 0x01, 0x00
            };

            TestStruct<SepCodeSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 30),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_SEPCODE),
                c => c.VerifyField(name: "pParent", value: 0),
                c => c.VerifyField(name: "pEnd", value: 11752),
                c => c.VerifyField(name: "length", value: 41),
                c => c.VerifyField(name: "scf", value: (CV_SEPCODEFLAGS) 0),
                c => c.VerifyField(name: "off", value: 1748702),
                c => c.VerifyField(name: "offParent", value: 575940),
                c => c.VerifyField(name: "sect", value: (ISECT) (short) 1),
                c => c.VerifyField(name: "sectParent", value: (ISECT) (short) 1)
            );
        }

        [TestMethod]
        public void SymType_SLink32_Test()
        {
            var str = GenerateTest<SLink32>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_SlotSym32_Test()
        {
            var str = GenerateTest<SlotSym32>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ThunkSym16_Test()
        {
            var str = GenerateTest<ThunkSym16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_ThunkSym32_Test()
        {
            var bytes = new byte[]
            {
                0x26, 0x00, 0x02, 0x11, 0x00, 0x00, 0x00, 0x00, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3F, 0x9E, 0x0E, 0x00,
                0x01, 0x00, 0x06, 0x00, 0x00, 0x52, 0x65, 0x70, 0x6F, 0x72, 0x74, 0x45, 0x76, 0x65, 0x6E, 0x74, 0x57, 0x00, 0x00, 0x00
            };

            TestStruct<ThunkSym32>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 38),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_THUNK32),
                c => c.VerifyField(name: "pParent", value: 0),
                c => c.VerifyField(name: "pEnd", value: 116),
                c => c.VerifyField(name: "pNext", value: 0),
                c => c.VerifyField(name: "off", value: 958015),
                c => c.VerifyField(name: "seg", value: (ISECT) 1),
                c => c.VerifyField(name: "len", value: (short) 6),
                c => c.VerifyField(name: "ord", value: THUNK_ORDINAL.THUNK_ORDINAL_NOTYPE),
                c => c.VerifyField(name: "name", value: "ReportEventW"),
                c => c.VerifyByteBlob(offset: 4134, value: new byte[] { 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_TrampolineSym_Test()
        {
            var str = GenerateTest<TrampolineSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void SymType_UdtSym_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x08, 0x11, 0x30, 0x11, 0x00, 0x00, 0x70, 0x66, 0x6E, 0x00
            };

            TestStruct<UdtSym>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 10),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_UDT),
                c => c.VerifyField(name: "typind", value: 0x1130),
                c => c.VerifyField(name: "name", value: "pfn")
            );
        }

        [TestMethod]
        public void SymType_UdtSym16t_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x04, 0x00, 0x21, 0x00, 0x06, 0x77, 0x69, 0x6E, 0x74, 0x5F, 0x74, 0x00, 0x00, 0x00
            };

            TestStruct<UdtSym16t>(
                bytes,
                true,
                c => c.VerifyField(name: "reclen", value: (ushort) 14),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_UDT_16t),
                c => c.VerifyField(name: "typind", value: (short) 0x21),
                c => c.VerifyField(name: "name", value: "wint_t"),
                c => c.VerifyByteBlob(offset: 4109, value: new byte[] { 0, 0, 0 })
            );
        }

        [TestMethod]
        public void SymType_UNameSpace_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x24, 0x11, 0x5F, 0x5F, 0x76, 0x63, 0x5F, 0x61, 0x74, 0x74, 0x72, 0x69, 0x62, 0x75, 0x74, 0x65, 0x73, 0x00
            };

            TestStruct<UNameSpace>(
                bytes,
                c => c.VerifyField(name: "reclen", value: (ushort) 18),
                c => c.VerifyField(name: "rectyp", value: SYM_ENUM_e.S_UNAMESPACE),
                c => c.VerifyField(name: "name", value: "__vc_attributes")
            );
        }

        private unsafe void TestStruct<T>(byte[] bytes, params Action<IView>[] verify) =>
            TestStruct<T>(bytes, false, false, verify);

        private unsafe void TestStruct<T>(byte[] bytes, bool lengthPrefixed, params Action<IView>[] verify) =>
            TestStruct<T>(bytes, lengthPrefixed, false, verify);

        private unsafe void TestStruct<T>(
            byte[] bytes,
            bool lengthPrefixed,
            bool isFieldListMember,
            params Action<IView>[] verify)
        {
            fixed (byte* pBytes = bytes)
            {
                var codeViewAccessor = new MockSymbolAccessor()
                {
                    HasLengthPrefixedStrings = lengthPrefixed
                };

                SymbolMemoryTracker.RegisterSymbolMemory(pBytes, bytes.Length, codeViewAccessor, null);

                try
                {
                    object dispatched;

                    if (typeof(T).Name.StartsWith("Lf"))
                    {
                        if (isFieldListMember)
                            dispatched = ObjectTypTypeDispatcher.Instance.Dispatch((LfEasy) (lfEasy*) pBytes);
                        else
                            dispatched = ObjectTypTypeDispatcher.Instance.Dispatch((TypType) (TYPTYPE*) pBytes);
                    }
                    else
                    {
                        dispatched = ObjectSymTypeDispatcher.Instance.Dispatch((SymType) (SYMTYPE*) pBytes);
                    }

                    var rva = 0x1000;

                    var writer = new MockViewWriter(codeViewAccessor, pBytes - rva, bytes.Length);
                    writer.UnmanagedOffset = rva;

                    var structView = (IStructView) ((IViewable) dispatched).WriteStruct(writer);

                    var children = structView.Children.ToArray();

                    Assert.AreEqual(verify.Length, children.Length, "Number of views was different from expected");

                    for (var i = 0; i < children.Length; i++)
                    {
                        verify[i](children[i]);
                    }
                }
                finally
                {
                    SymbolMemoryTracker.ClearSymbolMemory(pBytes);
                }
            }
        }

        private unsafe static new string GenerateTest<T>()
        {
            //Get all symbols, dispatch to ObjectSymTypeDispatcher, and if the result is of type T get the underlying pointer,
            //cast it to SymType, call GetStringLength, create a span around the bytes and return them

            ClrDebug.Extensions.DiaStringsUseComHeap = true;

            using var pdbFile = PDBFile.FromFile(
                //Change this as required to try and get symbols from various PDBs
                Sample.VC40_PDB
            );

            var type = typeof(T);

            if (type.Name.StartsWith("Lf"))
            {
                IEnumerable<TypType> types = null;

                if (pdbFile.TPI != null)
                    types = pdbFile.TPI.Types;

                if (pdbFile.IPI != null)
                {
                    if (types != null)
                        types = types.Concat(pdbFile.IPI.Types);
                    else
                        types = pdbFile.IPI.Types;
                }

                if (types != null)
                {
                    foreach (var typType in types)
                    {
                        var dispatched = ObjectTypTypeDispatcher.Instance.Dispatch(typType);

                        if (dispatched.GetType() == type)
                        {
                            return CreateTest(dispatched, (byte*) (TYPTYPE*) typType, typType.len + sizeof(short), type, pdbFile);
                        }
                    }
                }
            }
            else
            {
                foreach (var symType in pdbFile.EnumerateSymbols(false))
                {
                    var dispatched = ObjectSymTypeDispatcher.Instance.Dispatch(symType);

                    if (dispatched.GetType() == typeof(T))
                    {
                        return CreateTest(dispatched, (byte*) (SYMTYPE*) symType, SymType.GetSymbolLength(symType, pdbFile), type, pdbFile);
                    }
                }
            }

            throw new AssertInconclusiveException();
        }

        private unsafe static string GenerateFieldListTest<T>()
        {
            using var pdbFile = PDBFile.FromFile(Sample.VC20_PDB);

            var type = typeof(T);

            IEnumerable<TypType> types = null;

            if (pdbFile.TPI != null)
                types = pdbFile.TPI.Types;

            if (pdbFile.IPI != null)
            {
                if (types != null)
                    types = types.Concat(pdbFile.IPI.Types);
                else
                    types = pdbFile.IPI.Types;
            }

            if (types != null)
            {
                foreach (var typType in types)
                {
                    LfEasy[] fields;

                    switch (typType.leaf)
                    {
                        case LEAF_ENUM_e.LF_FIELDLIST:
                            fields = ((LfFieldList) typType).fields;
                            break;

                        case LEAF_ENUM_e.LF_FIELDLIST_16t:
                            fields = ((LfFieldList16t) typType).fields;
                            break;

                        default:
                            continue;
                    }

                    foreach (var field in fields)
                    {
                        var dispatched = ObjectTypTypeDispatcher.Instance.Dispatch(field);

                        if (dispatched.GetType() == type)
                        {
                            return CreateTest(dispatched, (byte*) (lfEasy*) field, field.typlen, type, pdbFile);
                        }
                    }
                }
            }

            throw new AssertInconclusiveException();
        }

        private static unsafe string CreateTest(object dispatched, byte* ptr, int length, Type type, PDBFile pdbFile)
        {
            var bytes = new Span<byte>(ptr, length);

            var builder = new StringBuilder();
            builder.AppendLine("var bytes = new byte[]");
            builder.AppendLine("{");
            builder.Append("    ");

            for (var i = 0; i < bytes.Length; i++)
            {
                builder.AppendFormat("0x{0:X2}", bytes[i]);

                if (i < bytes.Length - 1)
                    builder.Append(",");

                if (((i + 1) % 20) == 0 && i < bytes.Length - 1)
                {
                    builder.AppendLine();
                    builder.Append("    ");
                }
                else
                {
                    if (i < bytes.Length - 1)
                        builder.Append(" ");
                }
            }

            builder.AppendLine();
            builder.AppendLine("};");
            builder.AppendLine();

            builder.AppendLine($"TestStruct<{type.Name}>(");
            builder.AppendLine("    bytes,");

            var byteViewProvider = (LocalByteViewProvider) pdbFile.CreateByteViewProvider(null);

            var writer = new MockViewWriter(pdbFile, byteViewProvider);
            writer.UnmanagedOffset = (int) (ptr - byteViewProvider.mmf);

            var structView = (IStructView) ((IViewable) dispatched).WriteStruct(writer);

            var children = structView.Children.ToArray();

            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];

                if (child is IFieldView f)
                {
                    var value = FormatValue(f.Value);

                    builder.Append($"    c => c.VerifyField(name: \"{f.Name}\", value: {value})");
                }
                else if (child is IBitFieldView b)
                {
                    var value = FormatValue(b.Value);

                    builder.Append($"    c => c.VerifyBitField(name: \"{b.Name}\", value: {value}, bits: {b.Bits})");
                }
                else if (child is IStructView s)
                {
                    builder.Append($"    c => c.VerifyStructIgnoreChildren(name: \"{s.Name}\", offset: {s.Offset}, size: {s.Size})");
                }
                else if (child is ByteBlobView l)
                {
                    var value = string.Join(", ", l.Bytes.ToArray());

                    builder.Append($"    c => c.VerifyByteBlob(offset: {l.Offset}, value: new byte[] {{{value}}})");
                }
                else if (child is IValueView v)
                {
                    var value = FormatValue(v.Value);

                    builder.Append($"    c => c.VerifyValue(offset: {v.Offset}, value: {value})");
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (i < children.Length - 1)
                    builder.Append(",");

                builder.AppendLine();
            }

            builder.Append(");");

            var str = builder.ToString();

            return str;
        }

        private static string FormatValue(object value)
        {
            if (value.GetType().IsEnum)
                return $"{value.GetType().Name}.{value}";

            if (value is string || value.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IString<,>)))
                return $"\"{value}\"";

            if (value is uint u)
                return $"(uint) {value}";

            if (value is ushort us)
                return $"(ushort) {value}";

            if (value is short s)
                return $"(short) {s}";

            if (value is byte b)
                return $"(byte) {value}";

            if (value is PDB.SN sn)
                value = (ushort) sn;

            return value.ToString();
        }

        #endregion
        #region TypType

        [TestMethod]
        public void TypType_LfAlias_Test()
        {
            var str = GenerateTest<LfAlias>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfArgList_Test()
        {
            var bytes = new byte[]
            {
                0x06, 0x00, 0x01, 0x12, 0x00, 0x00, 0x00, 0x00
            };

            TestStruct<LfArgList>(
                bytes,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_ARGLIST),
                c => c.VerifyField(name: "count", value: 0)
            );
        }

        [TestMethod]
        public void TypType_LfArgList16t_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x01, 0x02, 0x02, 0x00, 0x74, 0x00, 0x00, 0x10, 0xF2, 0xF1
            };

            TestStruct<LfArgList16t>(
                bytes,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_ARGLIST_16t),
                c => c.VerifyField(name: "count", value: (short) 2),
                c => c.VerifyFieldIgnoreValue(name: "arg")
            );
        }

        [TestMethod]
        public void TypType_LfArray_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x03, 0x15, 0x70, 0x00, 0x00, 0x00, 0x23, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0xF1
            };

            TestStruct<LfArray>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 14),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_ARRAY),
                c => c.VerifyField(name: "elemtype", value: 0x70),
                c => c.VerifyField(name: "idxtype", value: 0x23),
                c => c.VerifyStructField(name: "length", type: "Numeric Data", offset: 0x100C, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x100C, value: (ushort) 128)
                }),
                c => c.VerifyField(name: "name", value: ""),
                c => c.VerifyByteBlob(offset: 0x100E, value: new byte[] { 0x00, 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfArray16t_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x03, 0x00, 0x1F, 0x10, 0x11, 0x00, 0x00, 0x00, 0x00, 0xF1
            };

            TestStruct<LfArray16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: (ushort) 10),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_ARRAY_16t),
                c => c.VerifyField(name: "elemtype", value: (short) 0x101F),
                c => c.VerifyField(name: "idxtype", value: (short) 0x11),
                c => c.VerifyStructField(name: "length", type: "Numeric Data", offset:  0x1008, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x1008, value: (ushort) 0)
                }),
                c => c.VerifyField(name: "name", value: ""),
                c => c.VerifyByteBlob(offset: 0x100A, value: new byte[] { 0x00, 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfBArray_Test()
        {
            var str = GenerateTest<LfBArray>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfBArray16t_Test()
        {
            var str = GenerateTest<LfBArray16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfBClass_Test()
        {
            var bytes = new byte[]
            {
                0x00, 0x14, 0x03, 0x00, 0x76, 0x11, 0x00, 0x00, 0x00, 0x00
            };

            TestStruct<LfBClass>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_BCLASS),
                c => c.VerifyField(name: "attr", value: (short) 3),
                c => c.VerifyField(name: "index", value: 4470),
                c => c.VerifyStructField(name: "offset", type: "Numeric Data", offset: 0x1008, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x1008, value: (ushort) 0)
                })
            );
        }

        [TestMethod]
        public void TypType_LfBClass16t_Test()
        {
            var bytes = new byte[]
            {
                0x00, 0x04, 0xB8, 0x10, 0x03, 0x00, 0x00, 0x00
            };

            TestStruct<LfBClass16t>(
                bytes,
                lengthPrefixed: true,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_BCLASS_16t),
                c => c.VerifyField(name: "index", value: (short) 4280),
                c => c.VerifyField(name: "attr", value: (short) 3),
                c => c.VerifyStructField(name: "offset", type: "Numeric Data", offset: 0x1006, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x1006, value: (ushort) 0)
                })
            );
        }

        [TestMethod]
        public void TypType_LfBitfield_Test()
        {
            var str = GenerateTest<LfBitfield>();

            var bytes = new byte[]
            {
                0x0A, 0x00, 0x05, 0x12, 0x22, 0x00, 0x00, 0x00, 0x01, 0x00, 0xF2, 0xF1
            };

            TestStruct<LfBitfield>(
                bytes,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_BITFIELD),
                c => c.VerifyField(name: "type", value: 0x22),
                c => c.VerifyField(name: "length", value: (byte) 1),
                c => c.VerifyField(name: "position", value: (byte) 0)
            );
        }

        [TestMethod]
        public void TypType_LfBitfield16t_Test()
        {
            var str = GenerateTest<LfBitfield16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfBuildInfo_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x03, 0x16, 0x05, 0x00, 0x16, 0x10, 0x00, 0x00, 0x16, 0x10, 0x00, 0x00, 0x16, 0x10, 0x00, 0x00, 0x16, 0x10,
                0x00, 0x00, 0x1A, 0x10, 0x00, 0x00, 0xF2, 0xF1
            };

            TestStruct<LfBuildInfo>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 26),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_BUILDINFO),
                c => c.VerifyField(name: "count", value: (short) 5),
                c => c.VerifyFieldIgnoreValue(name: "arg"),
                c => c.VerifyByteBlob(offset: 0x101A, value: new byte[] { 0xf2, 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfChar_Test()
        {
            var str = GenerateTest<LfChar>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfClass_Test()
        {
            var bytes = new byte[]
            {
                0x3E, 0x00, 0x05, 0x15, 0x0B, 0x00, 0x00, 0x02, 0x0F, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x9C, 0x00, 0x5F, 0x4F, 0x53, 0x56, 0x45, 0x52, 0x53, 0x49, 0x4F, 0x4E, 0x49, 0x4E, 0x46, 0x4F, 0x45, 0x58, 0x41, 0x00,
                0x2E, 0x3F, 0x41, 0x55, 0x5F, 0x4F, 0x53, 0x56, 0x45, 0x52, 0x53, 0x49, 0x4F, 0x4E, 0x49, 0x4E, 0x46, 0x4F, 0x45, 0x58,
                0x41, 0x40, 0x40, 0x00
            };

            TestStruct<LfClass>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 62),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_STRUCTURE),
                c => c.VerifyField(name: "count", value: (short) 11),
                c => c.VerifyField(name: "property", value: (short) 512),
                c => c.VerifyField(name: "field", value: 0x100F),
                c => c.VerifyField(name: "derived", value: 0x0),
                c => c.VerifyField(name: "vshape", value: 0x0),
                c => c.VerifyStructField(name: "length", type: "Numeric Data", offset: 0x1014, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x1014, value: (ushort) 156)
                }),
                c => c.VerifyField(name: "name", value: "_OSVERSIONINFOEXA"),
                c => c.VerifyField(name: "uniquename", value: ".?AU_OSVERSIONINFOEXA@@")
            );
        }

        [TestMethod]
        public void TypType_LfClass16t_Test()
        {
            var bytes = new byte[]
            {
                0x16, 0x00, 0x05, 0x00, 0x08, 0x00, 0x12, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x06, 0x5F, 0x69, 0x6F,
                0x62, 0x75, 0x66, 0xF1
            };

            TestStruct<LfClass16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: (ushort) 22),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_STRUCTURE_16t),
                c => c.VerifyField(name: "count", value: (short) 8),
                c => c.VerifyField(name: "field", value: (short) 0x1012),
                c => c.VerifyField(name: "property", value: (short) 0),
                c => c.VerifyField(name: "derived", value: (short) 0x0),
                c => c.VerifyField(name: "vshape", value: (short) 0x0),
                c => c.VerifyStructField(name: "length", type: "Numeric Data", offset: 0x100E, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x100E, value: (ushort) 32)
                }),
                c => c.VerifyField(name: "name", value: "_iobuf"),
                c => c.VerifyByteBlob(offset: 0x1017, value: new byte[] { 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfCmplx128_Test()
        {
            var str = GenerateTest<LfCmplx128>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfCmplx32_Test()
        {
            var str = GenerateTest<LfCmplx32>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfCmplx64_Test()
        {
            var str = GenerateTest<LfCmplx64>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfCmplx80_Test()
        {
            var str = GenerateTest<LfCmplx80>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfCobol0_Test()
        {
            var str = GenerateTest<LfCobol0>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfCobol016t_Test()
        {
            var str = GenerateTest<LfCobol016t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfCobol1_Test()
        {
            var str = GenerateTest<LfCobol1>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDefArg_Test()
        {
            var str = GenerateTest<LfDefArg>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDefArg16t_Test()
        {
            var str = GenerateTest<LfDefArg16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDerived_Test()
        {
            var str = GenerateTest<LfDerived>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDerived16t_Test()
        {
            var str = GenerateTest<LfDerived16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDimArray_Test()
        {
            var str = GenerateTest<LfDimArray>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDimArray16t_Test()
        {
            var str = GenerateTest<LfDimArray16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDimCon_Test()
        {
            var str = GenerateTest<LfDimCon>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDimCon16t_Test()
        {
            var str = GenerateTest<LfDimCon16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDimVar_Test()
        {
            var str = GenerateTest<LfDimVar>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfDimVar16t_Test()
        {
            var str = GenerateTest<LfDimVar16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfEasy_Test()
        {
            var str = GenerateTest<LfEasy>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfEndPreComp_Test()
        {
            var str = GenerateTest<LfEndPreComp>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfEnum_Test()
        {
            var bytes = new byte[]
            {
                0x52, 0x00, 0x07, 0x15, 0x19, 0x00, 0x00, 0x02, 0x74, 0x00, 0x00, 0x00, 0x02, 0x10, 0x00, 0x00, 0x52, 0x65, 0x70, 0x6C,
                0x61, 0x63, 0x65, 0x73, 0x43, 0x6F, 0x72, 0x48, 0x64, 0x72, 0x4E, 0x75, 0x6D, 0x65, 0x72, 0x69, 0x63, 0x44, 0x65, 0x66,
                0x69, 0x6E, 0x65, 0x73, 0x00, 0x2E, 0x3F, 0x41, 0x57, 0x34, 0x52, 0x65, 0x70, 0x6C, 0x61, 0x63, 0x65, 0x73, 0x43, 0x6F,
                0x72, 0x48, 0x64, 0x72, 0x4E, 0x75, 0x6D, 0x65, 0x72, 0x69, 0x63, 0x44, 0x65, 0x66, 0x69, 0x6E, 0x65, 0x73, 0x40, 0x40,
                0x00, 0xF3, 0xF2, 0xF1
            };

            TestStruct<LfEnum>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 82),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_ENUM),
                c => c.VerifyField(name: "count", value: (short) 25),
                c => c.VerifyField(name: "property", value: (short) 512),
                c => c.VerifyField(name: "utype", value: 0x74),
                c => c.VerifyField(name: "field", value: 0x1002),
                c => c.VerifyField(name: "Name", value: "ReplacesCorHdrNumericDefines"),
                c => c.VerifyField(name: "uniquename", value: ".?AW4ReplacesCorHdrNumericDefines@@"),
                c => c.VerifyByteBlob(offset: 0x1051, value: new byte[] { 0xf3, 0xf2, 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfEnum16t_Test()
        {
            var str = GenerateTest<LfEnum16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfEnumerate_Test()
        {
            var bytes = new byte[]
            {
                0x02, 0x15, 0x03, 0x00, 0x01, 0x00, 0x43, 0x4F, 0x4D, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x46, 0x4C, 0x41, 0x47, 0x53,
                0x5F, 0x49, 0x4C, 0x4F, 0x4E, 0x4C, 0x59, 0x00
            };

            TestStruct<LfEnumerate>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_ENUMERATE),
                c => c.VerifyField(name: "attr", value: (short) 3),
                c => c.VerifyStructField(name: "value", type: "Numeric Data", offset: 0x1004, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x1004, value: (ushort) 1)
                }),
                c => c.VerifyField(name: "name", value: "COMIMAGE_FLAGS_ILONLY")
            );
        }

        [TestMethod]
        public void TypType_LfFieldList_Test()
        {
            var bytes = new byte[]
            {
                0x2A, 0x03, 0x03, 0x12, 0x02, 0x15, 0x03, 0x00, 0x01, 0x00, 0x43, 0x4F, 0x4D, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x46,
                0x4C, 0x41, 0x47, 0x53, 0x5F, 0x49, 0x4C, 0x4F, 0x4E, 0x4C, 0x59, 0x00, 0x02, 0x15, 0x03, 0x00, 0x02, 0x00, 0x43, 0x4F,
                0x4D, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x46, 0x4C, 0x41, 0x47, 0x53, 0x5F, 0x33, 0x32, 0x42, 0x49, 0x54, 0x52, 0x45,
                0x51, 0x55, 0x49, 0x52, 0x45, 0x44, 0x00, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x04, 0x00, 0x43, 0x4F, 0x4D, 0x49, 0x4D, 0x41,
                0x47, 0x45, 0x5F, 0x46, 0x4C, 0x41, 0x47, 0x53, 0x5F, 0x49, 0x4C, 0x5F, 0x4C, 0x49, 0x42, 0x52, 0x41, 0x52, 0x59, 0x00,
                0x02, 0x15, 0x03, 0x00, 0x08, 0x00, 0x43, 0x4F, 0x4D, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x46, 0x4C, 0x41, 0x47, 0x53,
                0x5F, 0x53, 0x54, 0x52, 0x4F, 0x4E, 0x47, 0x4E, 0x41, 0x4D, 0x45, 0x53, 0x49, 0x47, 0x4E, 0x45, 0x44, 0x00, 0xF2, 0xF1,
                0x02, 0x15, 0x03, 0x00, 0x10, 0x00, 0x43, 0x4F, 0x4D, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x46, 0x4C, 0x41, 0x47, 0x53,
                0x5F, 0x4E, 0x41, 0x54, 0x49, 0x56, 0x45, 0x5F, 0x45, 0x4E, 0x54, 0x52, 0x59, 0x50, 0x4F, 0x49, 0x4E, 0x54, 0x00, 0xF1,
                0x02, 0x15, 0x03, 0x00, 0x04, 0x80, 0x00, 0x00, 0x01, 0x00, 0x43, 0x4F, 0x4D, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x46,
                0x4C, 0x41, 0x47, 0x53, 0x5F, 0x54, 0x52, 0x41, 0x43, 0x4B, 0x44, 0x45, 0x42, 0x55, 0x47, 0x44, 0x41, 0x54, 0x41, 0x00,
                0x02, 0x15, 0x03, 0x00, 0x04, 0x80, 0x00, 0x00, 0x02, 0x00, 0x43, 0x4F, 0x4D, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x46,
                0x4C, 0x41, 0x47, 0x53, 0x5F, 0x33, 0x32, 0x42, 0x49, 0x54, 0x50, 0x52, 0x45, 0x46, 0x45, 0x52, 0x52, 0x45, 0x44, 0x00,
                0x02, 0x15, 0x03, 0x00, 0x02, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x56, 0x45, 0x52, 0x53, 0x49, 0x4F, 0x4E, 0x5F, 0x4D, 0x41,
                0x4A, 0x4F, 0x52, 0x5F, 0x56, 0x32, 0x00, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x02, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x56, 0x45,
                0x52, 0x53, 0x49, 0x4F, 0x4E, 0x5F, 0x4D, 0x41, 0x4A, 0x4F, 0x52, 0x00, 0x02, 0x15, 0x03, 0x00, 0x05, 0x00, 0x43, 0x4F,
                0x52, 0x5F, 0x56, 0x45, 0x52, 0x53, 0x49, 0x4F, 0x4E, 0x5F, 0x4D, 0x49, 0x4E, 0x4F, 0x52, 0x00, 0x02, 0x15, 0x03, 0x00,
                0x08, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x44, 0x45, 0x4C, 0x45, 0x54, 0x45, 0x44, 0x5F, 0x4E, 0x41, 0x4D, 0x45, 0x5F, 0x4C,
                0x45, 0x4E, 0x47, 0x54, 0x48, 0x00, 0xF2, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x08, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x56, 0x54,
                0x41, 0x42, 0x4C, 0x45, 0x47, 0x41, 0x50, 0x5F, 0x4E, 0x41, 0x4D, 0x45, 0x5F, 0x4C, 0x45, 0x4E, 0x47, 0x54, 0x48, 0x00,
                0x02, 0x15, 0x03, 0x00, 0x01, 0x00, 0x4E, 0x41, 0x54, 0x49, 0x56, 0x45, 0x5F, 0x54, 0x59, 0x50, 0x45, 0x5F, 0x4D, 0x41,
                0x58, 0x5F, 0x43, 0x42, 0x00, 0xF3, 0xF2, 0xF1, 0x02, 0x15, 0x03, 0x00, 0xFF, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x49, 0x4C,
                0x4D, 0x45, 0x54, 0x48, 0x4F, 0x44, 0x5F, 0x53, 0x45, 0x43, 0x54, 0x5F, 0x53, 0x4D, 0x41, 0x4C, 0x4C, 0x5F, 0x4D, 0x41,
                0x58, 0x5F, 0x44, 0x41, 0x54, 0x41, 0x53, 0x49, 0x5A, 0x45, 0x00, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x01, 0x00, 0x49, 0x4D,
                0x41, 0x47, 0x45, 0x5F, 0x43, 0x4F, 0x52, 0x5F, 0x4D, 0x49, 0x48, 0x5F, 0x4D, 0x45, 0x54, 0x48, 0x4F, 0x44, 0x52, 0x56,
                0x41, 0x00, 0xF2, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x02, 0x00, 0x49, 0x4D, 0x41, 0x47, 0x45, 0x5F, 0x43, 0x4F, 0x52, 0x5F,
                0x4D, 0x49, 0x48, 0x5F, 0x45, 0x48, 0x52, 0x56, 0x41, 0x00, 0xF2, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x08, 0x00, 0x49, 0x4D,
                0x41, 0x47, 0x45, 0x5F, 0x43, 0x4F, 0x52, 0x5F, 0x4D, 0x49, 0x48, 0x5F, 0x42, 0x41, 0x53, 0x49, 0x43, 0x42, 0x4C, 0x4F,
                0x43, 0x4B, 0x00, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x01, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x56, 0x54, 0x41, 0x42, 0x4C, 0x45,
                0x5F, 0x33, 0x32, 0x42, 0x49, 0x54, 0x00, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x02, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x56, 0x54,
                0x41, 0x42, 0x4C, 0x45, 0x5F, 0x36, 0x34, 0x42, 0x49, 0x54, 0x00, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x04, 0x00, 0x43, 0x4F,
                0x52, 0x5F, 0x56, 0x54, 0x41, 0x42, 0x4C, 0x45, 0x5F, 0x46, 0x52, 0x4F, 0x4D, 0x5F, 0x55, 0x4E, 0x4D, 0x41, 0x4E, 0x41,
                0x47, 0x45, 0x44, 0x00, 0x02, 0x15, 0x03, 0x00, 0x08, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x56, 0x54, 0x41, 0x42, 0x4C, 0x45,
                0x5F, 0x46, 0x52, 0x4F, 0x4D, 0x5F, 0x55, 0x4E, 0x4D, 0x41, 0x4E, 0x41, 0x47, 0x45, 0x44, 0x5F, 0x52, 0x45, 0x54, 0x41,
                0x49, 0x4E, 0x5F, 0x41, 0x50, 0x50, 0x44, 0x4F, 0x4D, 0x41, 0x49, 0x4E, 0x00, 0xF3, 0xF2, 0xF1, 0x02, 0x15, 0x03, 0x00,
                0x10, 0x00, 0x43, 0x4F, 0x52, 0x5F, 0x56, 0x54, 0x41, 0x42, 0x4C, 0x45, 0x5F, 0x43, 0x41, 0x4C, 0x4C, 0x5F, 0x4D, 0x4F,
                0x53, 0x54, 0x5F, 0x44, 0x45, 0x52, 0x49, 0x56, 0x45, 0x44, 0x00, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x20, 0x00, 0x49, 0x4D,
                0x41, 0x47, 0x45, 0x5F, 0x43, 0x4F, 0x52, 0x5F, 0x45, 0x41, 0x54, 0x4A, 0x5F, 0x54, 0x48, 0x55, 0x4E, 0x4B, 0x5F, 0x53,
                0x49, 0x5A, 0x45, 0x00, 0x02, 0x15, 0x03, 0x00, 0x00, 0x04, 0x4D, 0x41, 0x58, 0x5F, 0x43, 0x4C, 0x41, 0x53, 0x53, 0x5F,
                0x4E, 0x41, 0x4D, 0x45, 0x00, 0xF3, 0xF2, 0xF1, 0x02, 0x15, 0x03, 0x00, 0x00, 0x04, 0x4D, 0x41, 0x58, 0x5F, 0x50, 0x41,
                0x43, 0x4B, 0x41, 0x47, 0x45, 0x5F, 0x4E, 0x41, 0x4D, 0x45, 0x00, 0xF1
            };

            TestStruct<LfFieldList>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 810),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_FIELDLIST),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4100, size: 28),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4128, size: 35),
                c => c.VerifyValue(offset: 4163, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4164, size: 32),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4196, size: 38),
                c => c.VerifyValue(offset: 4234, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4235, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4236, size: 39),
                c => c.VerifyValue(offset: 4275, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4276, size: 40),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4316, size: 40),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4356, size: 27),
                c => c.VerifyValue(offset: 4383, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4384, size: 24),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4408, size: 24),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4432, size: 30),
                c => c.VerifyValue(offset: 4462, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4463, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4464, size: 32),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4496, size: 25),
                c => c.VerifyValue(offset: 4521, value: LEAF_ENUM_e.LF_PAD3),
                c => c.VerifyValue(offset: 4522, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4523, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4524, size: 43),
                c => c.VerifyValue(offset: 4567, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4568, size: 30),
                c => c.VerifyValue(offset: 4598, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4599, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4600, size: 26),
                c => c.VerifyValue(offset: 4626, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4627, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4628, size: 31),
                c => c.VerifyValue(offset: 4659, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4660, size: 23),
                c => c.VerifyValue(offset: 4683, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4684, size: 23),
                c => c.VerifyValue(offset: 4707, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4708, size: 32),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4740, size: 49),
                c => c.VerifyValue(offset: 4789, value: LEAF_ENUM_e.LF_PAD3),
                c => c.VerifyValue(offset: 4790, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4791, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4792, size: 35),
                c => c.VerifyValue(offset: 4827, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4828, size: 32),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4860, size: 21),
                c => c.VerifyValue(offset: 4881, value: LEAF_ENUM_e.LF_PAD3),
                c => c.VerifyValue(offset: 4882, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4883, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfEnumerate", offset: 4884, size: 23),
                c => c.VerifyValue(offset: 4907, value: LEAF_ENUM_e.LF_PAD1)
            );
        }

        [TestMethod]
        public void TypType_LfFieldList16t_Test()
        {
            var bytes = new byte[]
            {
                0x8A, 0x00, 0x04, 0x02, 0x06, 0x04, 0x70, 0x04, 0x03, 0x00, 0x00, 0x00, 0x04, 0x5F, 0x70, 0x74, 0x72, 0xF3, 0xF2, 0xF1,
                0x06, 0x04, 0x74, 0x00, 0x03, 0x00, 0x04, 0x00, 0x04, 0x5F, 0x63, 0x6E, 0x74, 0xF3, 0xF2, 0xF1, 0x06, 0x04, 0x70, 0x04,
                0x03, 0x00, 0x08, 0x00, 0x05, 0x5F, 0x62, 0x61, 0x73, 0x65, 0xF2, 0xF1, 0x06, 0x04, 0x74, 0x00, 0x03, 0x00, 0x0C, 0x00,
                0x05, 0x5F, 0x66, 0x6C, 0x61, 0x67, 0xF2, 0xF1, 0x06, 0x04, 0x74, 0x00, 0x03, 0x00, 0x10, 0x00, 0x05, 0x5F, 0x66, 0x69,
                0x6C, 0x65, 0xF2, 0xF1, 0x06, 0x04, 0x74, 0x00, 0x03, 0x00, 0x14, 0x00, 0x08, 0x5F, 0x63, 0x68, 0x61, 0x72, 0x62, 0x75,
                0x66, 0xF3, 0xF2, 0xF1, 0x06, 0x04, 0x74, 0x00, 0x03, 0x00, 0x18, 0x00, 0x07, 0x5F, 0x62, 0x75, 0x66, 0x73, 0x69, 0x7A,
                0x06, 0x04, 0x70, 0x04, 0x03, 0x00, 0x1C, 0x00, 0x09, 0x5F, 0x74, 0x6D, 0x70, 0x66, 0x6E, 0x61, 0x6D, 0x65, 0xF2, 0xF1
            };

            TestStruct<LfFieldList16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: (ushort) 138),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_FIELDLIST_16t),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4100, size: 13),
                c => c.VerifyValue(offset: 4113, value: LEAF_ENUM_e.LF_PAD3),
                c => c.VerifyValue(offset: 4114, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4115, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4116, size: 13),
                c => c.VerifyValue(offset: 4129, value: LEAF_ENUM_e.LF_PAD3),
                c => c.VerifyValue(offset: 4130, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4131, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4132, size: 14),
                c => c.VerifyValue(offset: 4146, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4147, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4148, size: 14),
                c => c.VerifyValue(offset: 4162, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4163, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4164, size: 14),
                c => c.VerifyValue(offset: 4178, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4179, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4180, size: 17),
                c => c.VerifyValue(offset: 4197, value: LEAF_ENUM_e.LF_PAD3),
                c => c.VerifyValue(offset: 4198, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4199, value: LEAF_ENUM_e.LF_PAD1),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4200, size: 16),
                c => c.VerifyStructIgnoreChildren(name: "lfMember_16t", offset: 4216, size: 18),
                c => c.VerifyValue(offset: 4234, value: LEAF_ENUM_e.LF_PAD2),
                c => c.VerifyValue(offset: 4235, value: LEAF_ENUM_e.LF_PAD1)
            );
        }

        [TestMethod]
        public void TypType_LfFriendCls_Test()
        {
            var str = GenerateFieldListTest<LfFriendCls>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfFriendCls16t_Test()
        {
            var str = GenerateFieldListTest<LfFriendCls16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfFriendFcn_Test()
        {
            var str = GenerateFieldListTest<LfFriendFcn>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfFriendFcn16t_Test()
        {
            var str = GenerateFieldListTest<LfFriendFcn16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfFuncId_Test()
        {
            var bytes = new byte[]
            {
                0x22, 0x00, 0x01, 0x16, 0x00, 0x00, 0x00, 0x00, 0x65, 0x10, 0x00, 0x00, 0x6F, 0x70, 0x65, 0x72, 0x61, 0x74, 0x6F, 0x72,
                0x20, 0x6E, 0x65, 0x77, 0x5B, 0x5D, 0x00, 0xA2, 0x0F, 0xEA, 0x80, 0x4D, 0x1B, 0x86, 0xD1, 0xF1
            };

            TestStruct<LfFuncId>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 34),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_FUNC_ID),
                c => c.VerifyField(name: "scopeId", value: (CV_ItemId) 0),
                c => c.VerifyField(name: "type", value: 0x1065),
                c => c.VerifyField(name: "name", value: "operator new[]"),
                c => c.VerifyByteBlob(offset: 0x101B, value: new byte[] { 0xA2, 0x0F, 0xEA, 0x80, 0x4D, 0x1B, 0x86, 0xD1, 0xF1 })
            );
        }

        [TestMethod]
        public void TypType_LfHLSL_Test()
        {
            var str = GenerateTest<LfHLSL>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfIndex_Test()
        {
            var bytes = new byte[]
            {
                0x04, 0x14, 0x00, 0x00, 0x12, 0x10, 0x00, 0x00
            };

            TestStruct<LfIndex>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_INDEX),
                c => c.VerifyField(name: "pad0", value: (short) 0),
                c => c.VerifyField(name: "index", value: 4114)
            );
        }

        [TestMethod]
        public void TypType_LfIndex16t_Test()
        {
            var str = GenerateFieldListTest<LfIndex16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfLabel_Test()
        {
            var str = GenerateTest<LfLabel>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfList_Test()
        {
            var str = GenerateTest<LfList>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfLong_Test()
        {
            var str = GenerateTest<LfLong>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfManaged_Test()
        {
            var str = GenerateTest<LfManaged>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfMatrix_Test()
        {
            var str = GenerateTest<LfMatrix>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfMember_Test()
        {
            var bytes = new byte[]
            {
                0x0D, 0x15, 0x03, 0x00, 0x22, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64, 0x77, 0x4F, 0x53, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6F,
                0x6E, 0x49, 0x6E, 0x66, 0x6F, 0x53, 0x69, 0x7A, 0x65, 0x00
            };

            TestStruct<LfMember>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_MEMBER),
                c => c.VerifyField(name: "attr", value: (short) 3),
                c => c.VerifyField(name: "index", value: 34),
                c => c.VerifyStructField(name: "offset", type: "Numeric Data", offset: 0x1008, size: 2, new Action<IView>[]
                {
                    c1 => c1.VerifyValue(offset: 0x1008, value: (ushort) 0)
                }),
                c => c.VerifyField(name: "name", value: "dwOSVersionInfoSize")
            );
        }

        [TestMethod]
        public void TypType_LfMember16t_Test()
        {
            var bytes = new byte[]
            {
                0x06, 0x04, 0x70, 0x04, 0x03, 0x00, 0x00, 0x00, 0x04, 0x5F, 0x70, 0x74, 0x72
            };

            TestStruct<LfMember16t>(
                bytes,
                lengthPrefixed: true,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_MEMBER_16t),
                c => c.VerifyField(name: "index", value: (short) 1136),
                c => c.VerifyField(name: "attr", value: (short) 3)
            );
        }

        [TestMethod]
        public void TypType_LfMemberModify_Test()
        {
            var str = GenerateTest<LfMemberModify>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfMethod_Test()
        {
            var bytes = new byte[]
            {
                0x0F, 0x15, 0x03, 0x00, 0x81, 0x11, 0x00, 0x00, 0x62, 0x61, 0x64, 0x5F, 0x65, 0x78, 0x63, 0x65, 0x70, 0x74, 0x69, 0x6F,
                0x6E, 0x00
            };

            TestStruct<LfMethod>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_METHOD),
                c => c.VerifyField(name: "count", value: (short) 3),
                c => c.VerifyField(name: "mList", value: 0x1181),
                c => c.VerifyField(name: "Name", value: "bad_exception")
            );
        }

        [TestMethod]
        public void TypType_LfMethod16t_Test()
        {
            var str = GenerateFieldListTest<LfMethod16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfMethodList_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x06, 0x12, 0x03, 0x01, 0x00, 0x00, 0x7B, 0x11, 0x00, 0x00, 0x03, 0x01, 0x00, 0x00, 0x7F, 0x11, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x80, 0x11, 0x00, 0x00
            };

            TestStruct<LfMethodList>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 26),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_METHODLIST),
                c => c.VerifyFieldIgnoreValue(name: "mList")
            );
        }

        [TestMethod]
        public void TypType_LfMethodList16t_Test()
        {
            var str = GenerateTest<LfMethodList16t>();
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x07, 0x02, 0x03, 0x00, 0xBD, 0x10, 0x03, 0x00, 0xC2, 0x10, 0x03, 0x00, 0xC3, 0x10
            };

            TestStruct<LfMethodList16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: LEAF_ENUM_e.LF_METHODLIST_16t),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_METHODLIST_16t),
                c => c.VerifyStructFieldArray(name: "mList", verifyStructs: new Action<IView>[]
                    {
                        c1 => c1.VerifyStruct(name: "mlMethod_16t", offset: 04100, size: 4,
                            c2 => c2.VerifyField(name: "attr", (short) 3),
                            c2 => c2.VerifyField(name: "index", (short) 4285)
                        ),
                        c1 => c1.VerifyStruct(name: "mlMethod_16t", offset: 04104, size: 4,
                            c2 => c2.VerifyField(name: "attr", (short) 3),
                            c2 => c2.VerifyField(name: "index", (short) 4290)
                        ),
                        c1 => c1.VerifyStruct(name: "mlMethod_16t", offset: 04108, size: 4,
                            c2 => c2.VerifyField(name: "attr", (short) 3),
                            c2 => c2.VerifyField(name: "index", (short) 4291)
                        ),
                    }
                )
            );
        }

        [TestMethod]
        public void TypType_LfMFunc_Test()
        {
            var bytes = new byte[]
            {
                0x1A, 0x00, 0x09, 0x10, 0x74, 0x00, 0x00, 0x00, 0xEF, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00,
                0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            TestStruct<LfMFunc>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 26),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_MFUNCTION),
                c => c.VerifyField(name: "rvtype", value: 0x74),
                c => c.VerifyField(name: "classtype", value: 0x10EF),
                c => c.VerifyField(name: "thistype", value: 0x0),
                c => c.VerifyField(name: "calltype", value: CV_call_e.CV_CALL_NEAR_C),
                c => c.VerifyField(name: "funcattr", value: (byte) 8),
                c => c.VerifyField(name: "parmcount", value: (short) 0),
                c => c.VerifyField(name: "arglist", value: 0x1000),
                c => c.VerifyField(name: "thisadjust", value: 0)
            );
        }

        [TestMethod]
        public void TypType_LfMFunc16t_Test()
        {
            var bytes = new byte[]
            {
                0x12, 0x00, 0x09, 0x00, 0x03, 0x00, 0xB8, 0x10, 0xB9, 0x10, 0x0B, 0x00, 0x01, 0x00, 0xBC, 0x10, 0x00, 0x00, 0x00, 0x00
            };

            TestStruct<LfMFunc16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: (ushort) 18),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_MFUNCTION_16t),
                c => c.VerifyField(name: "rvtype", value: (short) 0x3),
                c => c.VerifyField(name: "classtype", value: (short) 0x10B8),
                c => c.VerifyField(name: "thistype", value: (short) 0x10B9),
                c => c.VerifyField(name: "calltype", value: CV_call_e.CV_CALL_THISCALL),
                c => c.VerifyField(name: "funcattr", value: (byte) 0),
                c => c.VerifyField(name: "parmcount", value: (short) 1),
                c => c.VerifyField(name: "arglist", value: (short) 0x10BC),
                c => c.VerifyField(name: "thisadjust", value: 0)
            );
        }

        [TestMethod]
        public void TypType_LfMFuncId_Test()
        {
            var bytes = new byte[]
            {
                0x22, 0x00, 0x02, 0x16, 0x06, 0x11, 0x00, 0x00, 0x07, 0x11, 0x00, 0x00, 0x63, 0x6F, 0x6E, 0x66, 0x69, 0x67, 0x75, 0x72,
                0x65, 0x5F, 0x61, 0x72, 0x67, 0x76, 0x00, 0x78, 0x92, 0xA0, 0xA6, 0x8D, 0xE0, 0x1E, 0xA9, 0xF1
            };

            //I don't know what the random bytes at the end mean

            TestStruct<LfMFuncId>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 34),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_MFUNC_ID),
                c => c.VerifyField(name: "parentType", value: 0x1106),
                c => c.VerifyField(name: "type", value: 0x1107),
                c => c.VerifyField(name: "name", value: "configure_argv"),
                c => c.VerifyByteBlob(offset: 0x101B, value: new byte[] { 0x78, 0x92, 0xA0, 0xA6, 0x8D, 0xE0, 0x1E, 0xA9, 0xF1 })
            );
        }

        [TestMethod]
        public void TypType_LfModifier_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x01, 0x10, 0x71, 0x00, 0x00, 0x00, 0x04, 0x00, 0xF2, 0xF1
            };

            TestStruct<LfModifier>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 10),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_MODIFIER),
                c => c.VerifyField(name: "type", value: 0x71),
                c => c.VerifyField(name: "attr", value: (short) 4)
            );
        }

        [TestMethod]
        public void TypType_LfModifier16t_Test()
        {
            var bytes = new byte[]
            {
                0x06, 0x00, 0x01, 0x00, 0x01, 0x00, 0x61, 0x10
            };

            TestStruct<LfModifier16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: (ushort) 6),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_MODIFIER_16t),
                c => c.VerifyField(name: "attr", value: (short) 1),
                c => c.VerifyField(name: "type", value: (short) 0x1061)
            );
        }

        [TestMethod]
        public void TypType_LfModifierEx_Test()
        {
            var str = GenerateTest<LfModifierEx>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfNestType_Test()
        {
            var bytes = new byte[]
            {
                0x10, 0x15, 0x00, 0x00, 0x37, 0x10, 0x00, 0x00, 0x3C, 0x75, 0x6E, 0x6E, 0x61, 0x6D, 0x65, 0x64, 0x2D, 0x74, 0x79, 0x70,
                0x65, 0x2D, 0x75, 0x3E, 0x00
            };

            TestStruct<LfNestType>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_NESTTYPE),
                c => c.VerifyField(name: "pad0", value: (short) 0),
                c => c.VerifyField(name: "index", value: 4151),
                c => c.VerifyField(name: "Name", value: "<unnamed-type-u>")
            );
        }

        [TestMethod]
        public void TypType_LfNestType16t_Test()
        {
            var str = GenerateFieldListTest<LfNestType16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfNestTypeEx_Test()
        {
            var str = GenerateFieldListTest<LfNestTypeEx>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfOct_Test()
        {
            var str = GenerateTest<LfOct>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfOEM_Test()
        {
            var str = GenerateTest<LfOEM>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfOEM16t_Test()
        {
            var str = GenerateTest<LfOEM16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfOEM2_Test()
        {
            var str = GenerateTest<LfOEM2>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfOneMethod_Test()
        {
            var bytes = new byte[]
            {
                0x11, 0x15, 0x0B, 0x00, 0xF0, 0x10, 0x00, 0x00, 0x63, 0x6F, 0x6E, 0x66, 0x69, 0x67, 0x75, 0x72, 0x65, 0x5F, 0x61, 0x72,
                0x67, 0x76, 0x00
            };

            TestStruct<LfOneMethod>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_ONEMETHOD),
                c => c.VerifyField(name: "attr", value: (short) 11),
                c => c.VerifyField(name: "index", value: 4336),
                c => c.VerifyField(name: "name", value: "configure_argv")
            );
        }

        [TestMethod]
        public void TypType_LfOneMethod16t_Test()
        {
            var str = GenerateFieldListTest<LfOneMethod16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfPad_Test()
        {
            var str = GenerateTest<LfPad>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfPointer_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x02, 0x10, 0x11, 0x10, 0x00, 0x00, 0x0C, 0x00, 0x01, 0x00
            };

            TestStruct<LfPointer>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 10),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_POINTER),
                c => c.VerifyField(name: "utype", value: 0x1011),
                c => c.VerifyBitField(name: "ptrtype", value: CV_ptrtype_e.CV_PTR_64, bits: 5),
                c => c.VerifyBitField(name: "ptrmode", value: CV_ptrmode_e.CV_PTR_MODE_PTR, bits: 3),
                c => c.VerifyBitField(name: "isflat32", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isvolatile", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isconst", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isunaligned", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isrestrict", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "size", value: 8, bits: 6),
                c => c.VerifyBitField(name: "ismocom", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "islref", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isrref", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "unused", value: 0, bits: 10)
            );
        }

        [TestMethod]
        public void TypType_LfPointer16t_Test()
        {
            var bytes = new byte[]
            {
                0x06, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x70, 0x04
            };

            TestStruct<LfPointer16t>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 6),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_POINTER_16t),
                c => c.VerifyField(name: "utype", value: (short) 1136),
                c => c.VerifyBitField(name: "ptrtype", value: CV_ptrtype_e.CV_PTR_NEAR32, bits: 5),
                c => c.VerifyBitField(name: "ptrmode", value: CV_ptrmode_e.CV_PTR_MODE_PTR, bits: 3),
                c => c.VerifyBitField(name: "isflat32", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isvolatile", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isconst", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "isunaligned", value: (byte) 0, bits: 1),
                c => c.VerifyBitField(name: "unused", value: (short) 0, bits: 4)
            );
        }

        [TestMethod]
        public void TypType_LfPreComp_Test()
        {
            var str = GenerateTest<LfPreComp>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfPreComp16t_Test()
        {
            var str = GenerateTest<LfPreComp16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfProc_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x00, 0x08, 0x10, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00
            };

            TestStruct<LfProc>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 14),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_PROCEDURE),
                c => c.VerifyField(name: "rvtype", value: 0x3),
                c => c.VerifyField(name: "calltype", value: CV_call_e.CV_CALL_NEAR_C),
                c => c.VerifyField(name: "funcattr", value: (byte) 0),
                c => c.VerifyField(name: "parmcount", value: (short) 0),
                c => c.VerifyField(name: "arglist", value: 0x1000)
            );
        }

        [TestMethod]
        public void TypType_LfProc16t_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x08, 0x00, 0x74, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x10
            };

            TestStruct<LfProc16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: (ushort) 10),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_PROCEDURE_16t),
                c => c.VerifyField(name: "rvtype", value: (short) 0x74),
                c => c.VerifyField(name: "calltype", value: CV_call_e.CV_CALL_NEAR_C),
                c => c.VerifyField(name: "funcattr", value: (byte) 0),
                c => c.VerifyField(name: "parmcount", value: (short) 2),
                c => c.VerifyField(name: "arglist", value: (short) 0x1001)
            );
        }

        [TestMethod]
        public void TypType_LfQuad_Test()
        {
            var str = GenerateTest<LfQuad>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfReal128_Test()
        {
            var str = GenerateTest<LfReal128>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfReal16_Test()
        {
            var str = GenerateTest<LfReal16>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfReal32_Test()
        {
            var str = GenerateTest<LfReal32>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfReal48_Test()
        {
            var str = GenerateTest<LfReal48>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfReal64_Test()
        {
            var str = GenerateTest<LfReal64>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfReal80_Test()
        {
            var str = GenerateTest<LfReal80>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfRefSym_Test()
        {
            var str = GenerateTest<LfRefSym>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfShort_Test()
        {
            var str = GenerateTest<LfShort>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfSkip_Test()
        {
            var str = GenerateTest<LfSkip>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfSkip16t_Test()
        {
            var str = GenerateTest<LfSkip16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfSTMember_Test()
        {
            var bytes = new byte[]
            {
                0x0E, 0x15, 0x03, 0x00, 0x7C, 0x13, 0x00, 0x00, 0x6C, 0x65, 0x73, 0x73, 0x00
            };

            TestStruct<LfSTMember>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_STMEMBER),
                c => c.VerifyField(name: "attr", value: (short) 3),
                c => c.VerifyField(name: "index", value: 4988),
                c => c.VerifyField(name: "Name", value: "less")
            );
        }

        [TestMethod]
        public void TypType_LfSTMember16t_Test()
        {
            var str = GenerateFieldListTest<LfSTMember16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfStridedArray_Test()
        {
            var str = GenerateTest<LfStridedArray>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfStringId_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x00, 0x05, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF3, 0xF2, 0xF1
            };

            TestStruct<LfStringId>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 10),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_STRING_ID),
                c => c.VerifyField(name: "id", value: (CV_ItemId) 0),
                c => c.VerifyField(name: "name", value: ""),
                c => c.VerifyByteBlob(offset: 0x1009, value: new byte[] { 0xf3, 0xf2, 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfTypeServer_Test()
        {
            var str = GenerateTest<LfTypeServer>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfTypeServer2_Test()
        {
            var str = GenerateTest<LfTypeServer2>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfUdtModSrcLine_Test()
        {
            var bytes = new byte[]
            {
                0x10, 0x00, 0x07, 0x16, 0x03, 0x10, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x79, 0x50, 0x00, 0x00, 0x16, 0x00
            };

            TestStruct<LfUdtModSrcLine>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 16),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_UDT_MOD_SRC_LINE),
                c => c.VerifyField(name: "type", value: 0x1003),
                c => c.VerifyField(name: "src", value: (CV_ItemId) 1),
                c => c.VerifyField(name: "line", value: 20601),
                c => c.VerifyField(name: "imod", value: (ushort) 22)
            );
        }

        [TestMethod]
        public void TypType_LfUdtSrcLine_Test()
        {
            var str = GenerateTest<LfUdtSrcLine>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfULong_Test()
        {
            var str = GenerateTest<LfULong>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfUnion_Test()
        {
            var bytes = new byte[]
            {
                0x66, 0x00, 0x06, 0x15, 0x00, 0x00, 0x88, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5F, 0x54, 0x50, 0x5F, 0x43, 0x41,
                0x4C, 0x4C, 0x42, 0x41, 0x43, 0x4B, 0x5F, 0x45, 0x4E, 0x56, 0x49, 0x52, 0x4F, 0x4E, 0x5F, 0x56, 0x33, 0x3A, 0x3A, 0x3C,
                0x75, 0x6E, 0x6E, 0x61, 0x6D, 0x65, 0x64, 0x2D, 0x74, 0x79, 0x70, 0x65, 0x2D, 0x75, 0x3E, 0x00, 0x2E, 0x3F, 0x41, 0x54,
                0x3C, 0x75, 0x6E, 0x6E, 0x61, 0x6D, 0x65, 0x64, 0x2D, 0x74, 0x79, 0x70, 0x65, 0x2D, 0x75, 0x3E, 0x40, 0x5F, 0x54, 0x50,
                0x5F, 0x43, 0x41, 0x4C, 0x4C, 0x42, 0x41, 0x43, 0x4B, 0x5F, 0x45, 0x4E, 0x56, 0x49, 0x52, 0x4F, 0x4E, 0x5F, 0x56, 0x33,
                0x40, 0x40, 0x00, 0xF1
            };

            TestStruct<LfUnion>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 102),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_UNION),
                c => c.VerifyField(name: "count", value: (short) 0),
                c => c.VerifyField(name: "property", value: (short) 648),
                c => c.VerifyField(name: "field", value: 0x0),
                c => c.VerifyField(name: "length", value: 0),
                c => c.VerifyField(name: "name", value: "_TP_CALLBACK_ENVIRON_V3::<unnamed-type-u>"),
                c => c.VerifyField(name: "uniquename", value: ".?AT<unnamed-type-u>@_TP_CALLBACK_ENVIRON_V3@@"),
                c => c.VerifyByteBlob(offset: 4199, value: new byte[] { 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfUnion16t_Test()
        {
            var bytes = new byte[]
            {
                0x16, 0x00, 0x06, 0x00, 0x02, 0x00, 0x87, 0x11, 0x08, 0x00, 0x04, 0x00, 0x09, 0x5F, 0x5F, 0x75, 0x6E, 0x6E, 0x61, 0x6D,
                0x65, 0x64, 0xF2, 0xF1
            };

            TestStruct<LfUnion16t>(
                bytes,
                lengthPrefixed: true,
                c => c.VerifyField(name: "typlen", value: (ushort) 22),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_UNION_16t),
                c => c.VerifyField(name: "count", value: (short) 2),
                c => c.VerifyField(name: "field", value: (short) 0x1187),
                c => c.VerifyField(name: "property", value: (short) 8),
                c => c.VerifyField(name: "length", value: 4),
                c => c.VerifyField(name: "name", value: "__unnamed"),
                c => c.VerifyByteBlob(offset: 0x1016, value: new byte[] { 0xf2, 0xf1 })
            );
        }

        [TestMethod]
        public void TypType_LfUOct_Test()
        {
            var str = GenerateTest<LfUOct>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfUQuad_Test()
        {
            var str = GenerateTest<LfUQuad>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfUShort_Test()
        {
            var str = GenerateTest<LfUShort>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVarString_Test()
        {
            var str = GenerateTest<LfVarString>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVBClass_Test()
        {
            var str = GenerateFieldListTest<LfVBClass>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVBClass16t_Test()
        {
            var str = GenerateFieldListTest<LfVBClass16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVector_Test()
        {
            var str = GenerateTest<LfVector>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVftable_Test()
        {
            var str = GenerateTest<LfVftable>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVFTPath_Test()
        {
            var str = GenerateTest<LfVFTPath>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVFTPath16t_Test()
        {
            var str = GenerateTest<LfVFTPath16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVFuncOff_Test()
        {
            var str = GenerateTest<LfVFuncOff>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVFuncOff16t_Test()
        {
            var str = GenerateTest<LfVFuncOff16t>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void TypType_LfVFuncTab_Test()
        {
            var bytes = new byte[]
            {
                0x09, 0x14, 0x00, 0x00, 0x8B, 0x11, 0x00, 0x00
            };

            TestStruct<LfVFuncTab>(
                bytes,
                lengthPrefixed: false,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_VFUNCTAB),
                c => c.VerifyField(name: "pad0", value: (short) 0),
                c => c.VerifyField(name: "type", value: 0x118B)
            );
        }

        [TestMethod]
        public void TypType_LfVFuncTab16t_Test()
        {
            var bytes = new byte[]
            {
                0x0A, 0x04, 0xB7, 0x10
            };

            TestStruct<LfVFuncTab16t>(
                bytes,
                lengthPrefixed: true,
                isFieldListMember: true,
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_VFUNCTAB_16t),
                c => c.VerifyField(name: "type", value: (short) 0x10B7)
            );
        }

        [TestMethod]
        public void TypType_LfVTShape_Test()
        {
            var bytes = new byte[]
            {
                0x06, 0x00, 0x0A, 0x00, 0x02, 0x00, 0x55, 0xF1
            };

            TestStruct<LfVTShape>(
                bytes,
                c => c.VerifyField(name: "typlen", value: (ushort) 6),
                c => c.VerifyField(name: "leaf", value: LEAF_ENUM_e.LF_VTSHAPE),
                c => c.VerifyField(name: "count", value: (short) 2)
            );
        }

        #endregion
        #region C13

        [TestMethod]
        public void Symbols_C13_Symbols()
        {
            TestC13<SymTypeList>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS,
                v =>
                {
                    Assert.AreEqual(6, v.Count);
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_Lines()
        {
            TestC13<CvDebugSLinesHeader>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES,
                v =>
                {
                    //We don't show the name in the debugger display because that would require allocating a fake memory block and/or shipping
                    //the MODI all over the place, which we don't want to do
                    Assert.AreEqual(1, v.FileBlocks.Length);
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_StringTable()
        {
            //Haven't seen it in any PDBs, but it is in OBJ files

            TestC13<RawValue<Utf8String>[]>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE,
                v =>
                {
                    Assert.AreEqual(3, v.Length);

                    Assert.AreEqual("C:\\TestApp\\TestApp.cpp", v[1].ToString());
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_FileCheckSums()
        {
            TestC13<CvFileCheckSum[]>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS,
                v =>
                {
                    v.Verify(
                        "C:\\TestApp\\TestApp.cpp"
                    );
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_FrameData()
        {
            TestC13<RvaAndFrameData>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA,
                v =>
                {
                    Assert.AreEqual(1, v.FrameData.Length);
                    Assert.AreEqual(true, v.FrameData[0].fIsFunctionStart);
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_InlineeLines()
        {
            TestC13<InlineeSigAndLines>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES,
                v =>
                {
                    Assert.AreEqual("__dyn_tls_on_demand_init", ((PESpy.PDB.InlineeSourceLine[]) v.Lines)[0].inlinee.ToString());
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_CrossScopeImports()
        {
            TestC13<PDB.CrossScopeReferences[]>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEIMPORTS,
                v =>
                {
                    Assert.AreEqual(1, v.Length);

                    var ids = v[0].referenceIds;
                    Assert.AreEqual(1, ids.Count);

                    var id = ids[0];

                    Assert.AreEqual("0x80004527", id.ToString());
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_CrossScopeExports()
        {
            //Note that this crosses a page's worth of data, so we can't verify view alignment
            TestC13<PDB.LocalIdAndGlobalIdPair[]>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS,
                v =>
                {
                    Assert.AreEqual("__vcrt_uninitialize_locks", v[5].localId.ToString());
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_ILLines()
        {
            TestC13<CvDebugSLinesHeader>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES,
                v =>
                {
                    Assert.AreEqual(1, v.FileBlocks.Length);

                    var lines = v.FileBlocks[0].lines;
                    Assert.AreEqual(5, lines.Length);
                }
            );
        }

        [TestMethod]
        public unsafe void Symbols_C13_FuncMDTokenMap()
        {
            TestC13<FuncMDTokenMap>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FUNC_MDTOKEN_MAP,
                v =>
                {
                    Assert.AreEqual(68367, v.Entries.Length);

                    var item = v.Entries[0];

                    Assert.AreEqual(2, item.NumGenericParameters);

                    var provider = new MockSignatureProvider();
                    var decoder = new SignatureDecoder<object, object>(provider, null, null);
                    var blobReader = new BlobReader((byte*) item.TypeSpecBlobs, item.TypeSpecBlobs.Length);

                    var type1 = decoder.DecodeType(ref blobReader);
                    var type2 = decoder.DecodeType(ref blobReader);

                    Assert.AreEqual(0, blobReader.RemainingBytes);
                }
            );
        }

        [TestMethod]
        public unsafe void Symbols_C13_TypeMDTokenMap()
        {
            TestC13<TypeMDTokenMap>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_TYPE_MDTOKEN_MAP,
                v =>
                {
                    Assert.AreEqual(14454, v.Entries.Length);

                    var entry = v.Entries[0];

                    Assert.AreEqual("System::EventHandler$1<System::Diagnostics::Tracing::EventCommandEventArgs>", entry.ToString());

                    var provider = new MockSignatureProvider();
                    var decoder = new SignatureDecoder<object, object>(provider, null, null);
                    var blobReader = new BlobReader((byte*) entry.LargeTypeSig, entry.LargeTypeSig.Length);

                    var (genericType, typeArguments) = ((object, ImmutableArray<object>)) decoder.DecodeType(ref blobReader);

                    Assert.AreEqual("0x10000EE", genericType.ToString());
                    Assert.AreEqual(1, typeArguments.Length);
                    Assert.AreEqual("0x10004AD", typeArguments[0].ToString());

                    Assert.AreEqual(0, blobReader.RemainingBytes);
                }
            );
        }

        [TestMethod]
        public void Symbols_C13_MergedAssemblyInput()
        {
            TestC13<MergedAssemblyInfo[]>(
                DEBUG_S_SUBSECTION_TYPE.DEBUG_S_MERGED_ASSEMBLYINPUT,
                v =>
                {
                    Assert.AreEqual(25, v.Length);

                    Assert.AreEqual("System.Collections, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", v[0].ToString());
                }
            );
        }

        //I haven't found any references on how to parse DEBUG_S_COFF_SYMBOL_RVA
        //DEBUG_S_XFGHASH_TYPE and DEBUG_S_XFGHASH_VIRTUAL are new internal formats,
        //nobody knows how to parse these

        private void TestC13<T>(
            DEBUG_S_SUBSECTION_TYPE type,
            Action<T> verify)
        {
            Stream fs = null;
            IFile file = null;

            try
            {
                CvDebugSSubsectionHeader sectionHeader = type switch
                {
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS              => ((OBJSymbolsTable) GetSampleFile<OBJFile>(Sample.VS22_OBJ, out file, out fs).SectionData[1]).C13SubSections[0],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES                =>                    GetSampleFile<PDBFile>(Sample.VS22_PDB, out file, out fs).DBI.Modules[1].C13Lines[0],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE          => ((OBJSymbolsTable) GetSampleFile<OBJFile>(Sample.VS22_OBJ, out file, out fs).SectionData[1]).C13SubSections[5],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS           =>                    GetSampleFile<PDBFile>(Sample.VS22_PDB, out file, out fs).DBI.Modules[1].C13Lines[1],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA            => ((OBJSymbolsTable) GetSampleFile<OBJFile>(Sample.VS22_OBJ, out file, out fs).SectionData[1]).C13SubSections[1],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES         => GetSampleFile<PDBFile>(Locator.LocatePDB(WellKnownTestModule.coreclr), out file, out fs).DBI.Modules[25].C13Lines[1],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEIMPORTS    => GetSampleFile<PDBFile>(Locator.LocatePDB(WellKnownTestModule.coreclr), out file, out fs).DBI.Modules[112].C13Lines[1],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS    => GetSampleFile<PDBFile>(Locator.LocatePDB(WellKnownTestModule.coreclr), out file, out fs).DBI.Modules[112].C13Lines[0],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES             => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out file, out fs).DBI.Modules[0].C13Lines[1],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FUNC_MDTOKEN_MAP     => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out file, out fs).DBI.Modules[0].C13Lines[104775],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_TYPE_MDTOKEN_MAP     => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out file, out fs).DBI.Modules[0].C13Lines[104774],
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_MERGED_ASSEMBLYINPUT => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out file, out fs).DBI.Modules[0].C13Lines[104776],
                };

                var value = sectionHeader.Data;

                verify((T) value);

                //We can't verify alignment in PDBs, because simply calling WriteStruct isn't going to cause its children to be properly split across pages
                //e.g. C13 CrossScopeExports
                if (file.Kind != FileKind.PDB)
                {
                    //There's also value in us asserting that we can construct a view of the section containing the specific type of data

                    ViewWriter writer = file.Kind switch
                    {
                        FileKind.PDB => new PDBViewWriter((PDBFile) file),
                        FileKind.OBJ => new OBJViewWriter((OBJFile) file)
                    };

                    var view = (IStructView) ((IViewable) sectionHeader).WriteStruct(writer);

                    var verifier = new ViewAlignmentVerifier();
                    view.Accept(verifier);
                }
            }
            finally
            {
                file?.Dispose();
                fs?.Dispose();
            }
        }

        private static DEBUG_S_SUBSECTION_TYPE[] GetC13Types(string path)
        {
            if (Detector.TryDetectFile(path, out var kind, out _))
            {
                if (kind != FileKind.PDB)
                    return new DEBUG_S_SUBSECTION_TYPE[0];
            }

            using var pdbFile = PDBFile.FromFile(path);

            HashSet<DEBUG_S_SUBSECTION_TYPE> types = new();

            var modules = pdbFile.DBI.Modules;

            if (modules != null)
            {
                for (var i = 0; i < modules.Length; i++)
                {
                    var module = modules[i];

                    var c13 = module.C13Lines;

                    if (c13 != null)
                    {
                        foreach (var header in c13)
                        {
                            if (header.Type == DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES)
                            {
                                var xx = 0;
                            }
                        }
                    }
                }
            }

            return types.ToArray();
        }

        #endregion
    }
}
