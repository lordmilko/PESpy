#if PEFAST
using System;
using System.Linq;
using ClrDebug;
using ClrDebug.DIA;
using ClrDebug.PDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.OBJ;
using PESpy.PDB;
using PESpy.View;

namespace PESpy.Tests
{
    [TestClass]
    public class OBJTests
    {
        [TestMethod]
        public void OBJ_Classic_Minimal()
        {
            TestObj("OldObj", false,
            #region Header
                v => v.VerifyHeader(size: 220,
                    c => c.VerifyStruct(name: "IMAGE_FILE_HEADER", offset: 0, size: 20,
                        c1 => c1.VerifyField("Machine", IMAGE_FILE_MACHINE.I386),
                        c1 => c1.VerifyField("NumberOfSections", (short) 5),
                        c1 => c1.VerifyFieldIgnoreValue("TimeDateStamp"),
                        c1 => c1.VerifyField("PointerToSymbolTable", 1108),
                        c1 => c1.VerifyField("NumberOfSymbols", 14),
                        c1 => c1.VerifyField("SizeOfOptionalHeader", (short) 0),
                        c1 => c1.VerifyField("Characteristics", (ImageFile) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x14, size: 40,
                        c1 => c1.VerifyField("Name", ".drectve"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 47),
                        c1 => c1.VerifyField("PointerToRawData", 0xDC),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.LNK_INFO | IMAGE_SCN.LNK_REMOVE | IMAGE_SCN.ALIGN_1BYTES)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x3C, size: 40,
                        c1 => c1.VerifyField("Name", ".debug$S"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 632),
                        c1 => c1.VerifyField("PointerToRawData", 0x10B),
                        c1 => c1.VerifyField("PointerToRelocations", 0x383),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 7),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_DISCARDABLE | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x64, size: 40,
                        c1 => c1.VerifyField("Name", ".text$mn"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 3),
                        c1 => c1.VerifyField("PointerToRawData", 0x3C9),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_CODE | IMAGE_SCN.ALIGN_16BYTES | IMAGE_SCN.MEM_EXECUTE | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x8C, size: 40,
                        c1 => c1.VerifyField("Name", ".debug$T"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 68),
                        c1 => c1.VerifyField("PointerToRawData", 0x3CC),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_DISCARDABLE | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0xB4, size: 40,
                        c1 => c1.VerifyField("Name", ".chks64"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 40),
                        c1 => c1.VerifyField("PointerToRawData", 0x410),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.LNK_INFO | IMAGE_SCN.LNK_REMOVE)
                    )
                ),
            #endregion
                v => v.VerifySection(name: ".drectve", offset: 0xDC, size: 47,
                    c => c.VerifyValue(offset: 0xDC, "   /DEFAULTLIB:\"MSVCRT\" /DEFAULTLIB:\"OLDNAMES\" ")
                ),
            #region .debug$S
                v => v.VerifySection(name: ".debug$S", offset: 0x10B, size: 632,
                    c => c.VerifyValue(offset: 0x10B, value: CV_SIGNATURE.C13),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x10F, size: 208, //DEBUG_S_SYMBOLS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS),
                        c1 => c1.VerifyField("cbLen", 200),
                        c1 => c1.VerifySymType(offset: 0x117, size: 82, SYM_ENUM_e.S_OBJNAME),
                        c1 => c1.VerifySymType(offset: 0x169, size: 60, SYM_ENUM_e.S_COMPILE3),
                        c1 => c1.VerifySymType(offset: 0x1A5, size: 20, SYM_ENUM_e.S_UNAMESPACE),
                        c1 => c1.VerifySymType(offset: 0x1B9, size: 22, SYM_ENUM_e.S_UNAMESPACE),
                        c1 => c1.VerifySymType(offset: 0x1CF, size: 8, SYM_ENUM_e.S_UNAMESPACE),
                        c1 => c1.VerifySymType(offset: 0x1D7, size: 8, SYM_ENUM_e.S_UNAMESPACE)
                    ),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x1DF, size: 44, //DEBUG_S_FRAMEDATA
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA),
                        c1 => c1.VerifyField("cbLen", 36),
                        c1 => c1.VerifyStruct(name: "RVA + FrameData", offset: 0x1EF, size: 36,
                            c2 => c2.VerifyField(name: "RVA", value: 3),
                            c2 => c2.VerifyStruct(name: "FRAMEDATA", offset: 0x1F3, size: 32,
                                c3 => c3.VerifyField(name: "ulRvaStart", value: 0),
                                c3 => c3.VerifyField(name: "cbBlock", value: 4),
                                c3 => c3.VerifyField(name: "cbLocals", value: 0),
                                c3 => c3.VerifyField(name: "cbParams", value: 0x4B),
                                c3 => c3.VerifyField(name: "cbStkMax", value: 0),
                                c3 => c3.VerifyField(name: "frameFunc", value: 4),
                                c3 => c3.VerifyField(name: "cbProlog", value: (short) 241),
                                c3 => c3.VerifyField(name: "cbSavedRegs", value: (short) 0),
                                c3 => c3.VerifyBitField(name: "fHasSEH", value: (byte) 0, bits: 1),
                                c3 => c3.VerifyBitField(name: "fHasEH", value: (byte) 1, bits: 1),
                                c3 => c3.VerifyBitField(name: "fIsFunctionStart", value: (byte) 1, bits: 1),
                                c3 => c3.VerifyBitField(name: "reserved", value: 16, bits: 29) //todo: i dont think this is right, we shouldnt have a value
                            )
                        )
                    ),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x20B, size: 142, //DEBUG_S_SYMBOLS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS),
                        c1 => c1.VerifyField("cbLen", 134),
                        c1 => c1.VerifySymType(offset: 0x213, size: 44, SYM_ENUM_e.S_GPROC32_ID),
                        c1 => c1.VerifySymType(offset: 0x23F, size: 12, SYM_ENUM_e.S_LOCAL),
                        c1 => c1.VerifySymType(offset: 0x24B, size: 20, SYM_ENUM_e.S_DEFRANGE_REGISTER_REL),
                        c1 => c1.VerifySymType(offset: 0x25F, size: 8, SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE),
                        c1 => c1.VerifySymType(offset: 0x267, size: 30, SYM_ENUM_e.S_FRAMEPROC),
                        c1 => c1.VerifySymType(offset: 0x285, size: 16, SYM_ENUM_e.S_REGREL32),
                        c1 => c1.VerifySymType(offset: 0x295, size: 4, SYM_ENUM_e.S_PROC_ID_END)
                    ),
                    c => c.VerifyByteBlob(offset: 0x299, new byte[2]),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x29B, size: 52, //DEBUG_S_LINES
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES),
                        c1 => c1.VerifyField("cbLen", 48),
                        c1 => c1.VerifyStruct(name: "CV_DebugSLinesHeader_t", offset: 0x2A3, size: 44,
                            c2 => c2.VerifyField(name: "offCon", 0),
                            c2 => c2.VerifyField(name: "segCon", (short) 0),
                            c2 => c2.VerifyField(name: "flags", (CV_LINES) 0),
                            c2 => c2.VerifyField(name: "cbCon", 3),
                            c2 => c2.VerifyStruct(name: "CV_DebugSLinesFileBlockHeader_t", offset: 0x2AF, size: 32,
                                c3 => c3.VerifyField(name: "nLines", 3),
                                c3 => c3.VerifyField(name: "cbBlock", 36),
                                c3 => c3.VerifyStruct(name: "CV_Line_t", offset: 0x2BB, size: 8,
                                    c4 => c4.VerifyField(name: "offset", value: 0),
                                    c4 => c4.VerifyBitField(name: "linenumStart", value: 3, bits: 24),
                                    c4 => c4.VerifyBitField(name: "deltaLineEnd", value: 0, bits: 7),
                                    c4 => c4.VerifyBitField(name: "fStatement", value: (byte) 1, bits: 1)
                                ),
                                c3 => c3.VerifyStruct(name: "CV_Line_t", offset: 0x2C3, size: 8,
                                    c4 => c4.VerifyField(name: "offset", value: 0),
                                    c4 => c4.VerifyBitField(name: "linenumStart", value: 4, bits: 24),
                                    c4 => c4.VerifyBitField(name: "deltaLineEnd", value: 0, bits: 7),
                                    c4 => c4.VerifyBitField(name: "fStatement", value: (byte) 1, bits: 1)
                                ),
                                c3 => c3.VerifyStruct(name: "CV_Line_t", offset: 0x2CB, size: 8,
                                    c4 => c4.VerifyField(name: "offset", value: 2),
                                    c4 => c4.VerifyBitField(name: "linenumStart", value: 5, bits: 24),
                                    c4 => c4.VerifyBitField(name: "deltaLineEnd", value: 0, bits: 7),
                                    c4 => c4.VerifyBitField(name: "fStatement", value: (byte) 1, bits: 1)
                                )
                            )
                        )
                    ),

                    //For some reason I have an extra 4 bytes at the end. This is wrong, I don't know why it's doing this
                    c => c.VerifyByteBlob(offset: 0x2CF, value: new byte[] {0x05, 0x00, 0x00, 0x80}),

                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x2D3, size: 30, //DEBUG_S_FILECHKSMS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS),
                        c1 => c1.VerifyField("cbLen", 24),
                        c1 => c1.VerifyStruct(name: "CV_FileCheckSum", offset: 0x2DB, size: 22,
                            c2 => c2.VerifyField(name: "name", 1),
                            c2 => c2.VerifyField(name: "len", (byte) 16),
                            c2 => c2.VerifyField(name: "type", CV_SourceChksum_t.CHKSUM_TYPE_MD5),
                            c2 => c2.VerifyField(name: "hash", new byte[] { 0x6d, 0x81, 0xcf, 0xfd, 0xd7, 0x2f, 0xb8, 0x9f, 0xb1, 0xe8, 0x76, 0xe9, 0xe9, 0x8c, 0xe4, 0xe2 })
                        )
                    ),
                    c => c.VerifyByteBlob(offset: 0X2F1, new byte[2]),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x2F3, size: 128, //DEBUG_S_STRINGTABLE
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE),
                        c1 => c1.VerifyField("cbLen", 120),
                        c1 => c1.VerifyValue(offset: 0x2FB, value: string.Empty),
                        c1 => c1.VerifyValueIgnoreValue(offset: 0x2FC), //The path to the obj file
                        c1 => c1.VerifyValue(offset: 0x346, value: "$T0 .raSearch = $eip $T0 ^ = $esp $T0 4 + = ")
                    ),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x373, size: 16, //DEBUG_S_SYMBOLS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS),
                        c1 => c1.VerifyField("cbLen", 8),
                        c1 => c1.VerifySymType(offset: 0x37B, size: 8, SYM_ENUM_e.S_BUILDINFO)
                    )
                ),
            #endregion
            #region Relocations
                v => v.VerifyLogicalRegion(name: "Relocations", offset: 0x383, size: 70,
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x383, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0xDC),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", ImageRelI386.Dir32NB)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x38D, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x128),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", ImageRelI386.SecRel)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x397, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x12C),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", ImageRelI386.Section)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x3A1, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x14C),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", ImageRelI386.SecRel)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x3AB, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x150),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", ImageRelI386.Section)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x3B5, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x198),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", ImageRelI386.SecRel)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x3BF, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x19C),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", ImageRelI386.Section)
                    )
                ),
            #endregion
                v => v.VerifySection(name: ".text$mn", offset: 0x3C9, size: 3,
                    c => c.VerifyByteBlob(offset: 0x3C9, new byte[] {0x33, 0xC0, 0xC3})
                ),
                v => v.VerifySection(name: ".debug$T", offset: 0x3CC, size: 68,
                    c => c.VerifyValue(offset: 0x3CC, value: CV_SIGNATURE.C13),
                    c => c.VerifyTypType(offset: 0x3D0, size: 64, type: LEAF_ENUM_e.LF_TYPESERVER2)
                ),
                v => v.VerifySection(name: ".chks64", offset: 0x410, size: 40,
                    c => c.VerifyByteBlob(offset: 0x410, value: new byte[] {
                        0x76, 0xf6, 0xab, 0xfb, 0x56, 0x48, 0xde, 0xc7, 0x14, 0xbd,
                        0x94, 0x74, 0xe6, 0x1f, 0xa1, 0xfb, 0x4b, 0xc7, 0x24, 0xf3,
                        0xb1, 0xd0, 0xca, 0xf4, 0x93, 0xef, 0x81, 0xfe, 0x37, 0xec,
                        0x71, 0x6b, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                    })
                ),
            #region Coff Symbol Table
                v => v.VerifyStruct(name: "Coff Symbol Table", offset: 0x438, size: 256,
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x438, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@comp.id"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x10575BC),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) -1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x44A, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@feat.00"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x80010091),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) -1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x45C, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@vol.md"),
                        c1 => c1.VerifyField(name: "Value", (uint) 2),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) -1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x46E, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".drectve"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x480, size: 18,
                            c2 => c2.VerifyField("Bytes", value: new byte[]{47,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x492, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".debug$S"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 2),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x4A4, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{120,2,0,0,7,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x4B6, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".text$mn"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 3),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x4C8, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{3,0,0,0,0,0,0,0,141,31,186,239,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x4DA, size: 18,
                        c1 => c1.VerifyField(name: "Name", "_main"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 3),
                        c1 => c1.VerifyField(name: "Type", (ImageSymType) 0x20), //IMAGE_SYM_DTYPE_FUNCTION bit shifted. See ImageSymtype for info
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.External),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x4EC, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".debug$T"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 4),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x4FE, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{68,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x510, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".chks64"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 5),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x522, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{40,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyField(name: "String Table Size", 4)
                )
            #endregion
            );
        }

        [TestMethod]
        public void OBJ_LTCG_Minimal()
        {
            TestObj(
                "NewObj", true,
            #region Header
                v => v.VerifyHeader(size: 300,
                    c => c.VerifyStruct(name: "ANON_OBJECT_HEADER", offset: 0, size: 32,
                        c1 => c1.VerifyField(name: "Sig1", value: IMAGE_FILE_MACHINE.UNKNOWN),
                        c1 => c1.VerifyField(name: "Sig2", value: (short) -1),
                        c1 => c1.VerifyField(name: "Version", value: (short) 1),
                        c1 => c1.VerifyField(name: "Machine", value: IMAGE_FILE_MACHINE.I386),
                        c1 => c1.VerifyFieldIgnoreValue(name: "TimeDateStamp"),
                        c1 => c1.VerifyField(name: "ClassID", value: new Guid("0cb3fe38-d9a5-4dab-ac9b-d6b6222653c2")), //It's written in c2!UtcCOMWriteCilObjHeader and queried in c2!DllGetObjHandler
                        c1 => c1.VerifyField(name: "SizeOfData", value: 2608)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_FILE_HEADER", offset: 0x20, size: 20,
                        c1 => c1.VerifyField("Machine", (IMAGE_FILE_MACHINE) 0xC13),
                        c1 => c1.VerifyField("NumberOfSections", (short) 7),
                        c1 => c1.VerifyFieldIgnoreValue("TimeDateStamp"),
                        c1 => c1.VerifyField("PointerToSymbolTable", 0x8FA),
                        c1 => c1.VerifyField("NumberOfSymbols", 17),
                        c1 => c1.VerifyField("SizeOfOptionalHeader", (short) 0),
                        c1 => c1.VerifyField("Characteristics", (ImageFile) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x34, size: 40,
                        c1 => c1.VerifyField("Name", ".drectve"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 127),
                        c1 => c1.VerifyField("PointerToRawData", 0x12C),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.LNK_INFO | IMAGE_SCN.LNK_REMOVE | IMAGE_SCN.ALIGN_1BYTES)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x5C, size: 40,
                        c1 => c1.VerifyField("Name", ".cil$fg"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 0xF8),
                        c1 => c1.VerifyField("PointerToRawData", 0x1AB),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x84, size: 40,
                        c1 => c1.VerifyField("Name", ".cil$gl"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 0x493),
                        c1 => c1.VerifyField("PointerToRawData", 0x2A3),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0xAC, size: 40,
                        c1 => c1.VerifyField("Name", ".cil$in"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 1),
                        c1 => c1.VerifyField("PointerToRawData", 0x736),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0xD4, size: 40,
                        c1 => c1.VerifyField("Name", ".cil$ex"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 223),
                        c1 => c1.VerifyField("PointerToRawData", 0x737),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0xFC, size: 40,
                        c1 => c1.VerifyField("Name", ".cil$sy"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 30),
                        c1 => c1.VerifyField("PointerToRawData", 0X816),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_READ)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x124, size: 40,
                        c1 => c1.VerifyField("Name", ".cil$db"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 198),
                        c1 => c1.VerifyField("PointerToRawData", 0X834),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.CNT_INITIALIZED_DATA | IMAGE_SCN.ALIGN_1BYTES | IMAGE_SCN.MEM_READ)
                    )
                ),
            #endregion
                v => v.VerifySection(name: ".drectve", offset: 0x14C, size: 127,
                    c => c.VerifyValue(offset: 0x14C, "   -compiler:\"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\VC\\Tools\\MSVC\\14.29.30133\\bin\\HostX86\\x86\\c2.dll\" ")
                ),
                v => v.VerifySectionIgnoreChildren(name: ".cil$fg", offset: 0x1CB, size: 248),
                v => v.VerifySectionIgnoreChildren(name: ".cil$gl", offset: 0x2C3, size: 1171),
                v => v.VerifySectionIgnoreChildren(name: ".cil$in", offset: 0x756, size: 1),
                v => v.VerifySectionIgnoreChildren(name: ".cil$ex", offset: 0x757, size: 223),
                v => v.VerifySectionIgnoreChildren(name: ".cil$sy", offset: 0x836, size: 30),
                v => v.VerifySectionIgnoreChildren(name: ".cil$db", offset: 0x854, size: 198),
            #region Coff Symbol Table
                v => v.VerifyStruct(name: "Coff Symbol Table", offset: 0x91A, size: 310,
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x91A, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@comp.id"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x10575BC),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) -1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x92C, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@feat.00"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x80010091),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) -1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x93E, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@vol.md"),
                        c1 => c1.VerifyField(name: "Value", (uint) 2),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) -1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x950, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".drectve"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 1),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x962, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{127,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x974, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$fg"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 2),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x986, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{248,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x998, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$gl"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 3),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x9AA, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{147,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x9BC, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$in"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 4),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x9CE, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x9E0, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$ex"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 5),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x9F2, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{223,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0xA04, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$sy"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 6),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0xA16, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{30,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0xA28, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$db"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (short) 7),
                        c1 => c1.VerifyField(name: "Type", ImageSymType.Null),
                        c1 => c1.VerifyField(name: "StorageClass", ImageSymClass.Static),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0xA3A, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{198,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyField(name: "String Table Size", 4)
                )
                #endregion
            );
        }

        [TestMethod]
        public void OBJ_C13_SymType_Strings()
        {
            //C13 strings should be UTF8
            using var objFile = OBJFile.FromFile(Sample.OBJ_C13);

            var symbolTable = objFile.SectionData.OfType<OBJSymbolsTable>().First();

            Assert.AreEqual(CV_SIGNATURE.C13, symbolTable.Signature);

            var subSection = symbolTable.C13SubSections.First();
            Assert.AreEqual(DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS, subSection.Type);

            var symbols = (SymType[]) subSection.Data;

            Assert.AreEqual("C:\\TestApp\\TestApp.obj", symbols[0].ToString());
        }

        [TestMethod]
        public void OBJ_C13_TypType_Strings()
        {
            //C13 strings should be UTF8 on TypType as well
            using var objFile = OBJFile.FromFile(Sample.OBJ_C13);

            var typeTable = objFile.SectionData.OfType<OBJTypesTable>().First();

            Assert.AreEqual(CV_SIGNATURE.C13, typeTable.Signature);

            var types = typeTable.Types;

            Assert.AreEqual("C:\\TestApp\\vc140.pdb", types[0].ToString());
        }

        [TestMethod]
        public void OBJ_C7_SymType_Strings()
        {
            //C7 strings should be ST
            using var objFile = OBJFile.FromFile(Sample.OBJ_C11);

            var symbolTable = objFile.SectionData.OfType<OBJSymbolsTable>().First();

            Assert.AreEqual(CV_SIGNATURE.C11, symbolTable.Signature);

            var symbols = symbolTable.C7Symbols;

            Assert.AreEqual("Debug/main.obj", symbols[0].ToString());
        }

        [TestMethod]
        public void OBJ_C7_TypType_Strings()
        {
            //C7 strings should be ST on TypType as well
            using var objFile = OBJFile.FromFile(Sample.OBJ_C11);

            var typeTable = objFile.SectionData.OfType<OBJTypesTable>().First();

            Assert.AreEqual(CV_SIGNATURE.C11, typeTable.Signature);

            var types = typeTable.Types;

            Assert.AreEqual("c:\\program files (x86)\\devstudio\\myprojects\\testapp\\debug\\vc50.pdb", types[0].ToString());
        }

        private void TestObj(
            string testName,
            bool ltcg,
            params Action<IView>[] verify)
        {
            var str = @"
int main(int a)
{
    return 0;
}";
            var msvc = new MSVC(testName, str, ltcg);
            var objFile = msvc.Compile();

            using var obj = OBJFile.FromFile(objFile);

            _ = obj.SectionData;

            var views = obj.GetView().Children;

            Assert.AreEqual(views.Length, verify.Length);

            for (var i = 0; i < verify.Length; i++)
                verify[i](views[i]);
        }
    }
}
#endif
