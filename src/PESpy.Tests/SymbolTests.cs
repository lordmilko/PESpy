using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using ChaosLib;
using ClrDebug;
using ClrDebug.DIA;
using ClrDebug.OMF;
using ClrDebug.PDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PESpy.OBJ;
using PESpy.PDB;
using PESpy.PowerShell;
using PESpy.View;
using PInvoke;
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
            WithNB02<RawValue<FixedAnsiString[]>>(
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
            TestC13<CvFileCheckSum[]>(
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
                c => c.VerifyField(name: "seg", value: (ushort) 1),
                c => c.VerifyField(name: "csz", value: (short) 1)
            );
        }
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
                c => c.VerifyField(name: "seg", value: (ushort) 1),
                c => c.VerifyField(name: "name", value: ""),
                c => c.VerifyByteBlob(offset: 4119, value: new byte[] { 0 })
            );
        }
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

            try
            {
                object value = type switch
                {
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS              => ((OBJSymbolsTable) GetSampleFile<OBJFile>(Sample.VS22_OBJ, out fs).SectionData[1]).C13SubSections[0].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES                => GetSampleFile<PDBFile>(Sample.VS22_PDB, out fs).DBI.Modules[1].C13Lines[0].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE          => ((OBJSymbolsTable) GetSampleFile<OBJFile>(Sample.VS22_OBJ, out fs).SectionData[1]).C13SubSections[5].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS           => GetSampleFile<PDBFile>(Sample.VS22_PDB, out fs).DBI.Modules[1].C13Lines[1].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA            => ((OBJSymbolsTable) GetSampleFile<OBJFile>(Sample.VS22_OBJ, out fs).SectionData[1]).C13SubSections[1].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES         => GetSampleFile<PDBFile>(Locator.LocatePDB(WellKnownTestModule.coreclr), out fs).DBI.Modules[25].C13Lines[1].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEIMPORTS    => GetSampleFile<PDBFile>(Locator.LocatePDB(WellKnownTestModule.coreclr), out fs).DBI.Modules[112].C13Lines[1].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS    => GetSampleFile<PDBFile>(Locator.LocatePDB(WellKnownTestModule.coreclr), out fs).DBI.Modules[112].C13Lines[0].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES             => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out fs).DBI.Modules[0].C13Lines[1].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FUNC_MDTOKEN_MAP     => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out fs).DBI.Modules[0].C13Lines[104775].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_TYPE_MDTOKEN_MAP     => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out fs).DBI.Modules[0].C13Lines[104774].Data,
                    DEBUG_S_SUBSECTION_TYPE.DEBUG_S_MERGED_ASSEMBLYINPUT => GetSampleFile<PDBFile>(Locator.Locate(WellKnownTestModule.SharedLibraryPDB), out fs).DBI.Modules[0].C13Lines[104776].Data,
                };

                verify((T) value);
            }
            finally
            {
                fs?.Dispose();
            }
        }
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
        }

        #endregion
    }
}
