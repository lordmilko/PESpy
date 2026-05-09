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
                        c1 => c1.VerifyField("Machine", IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386),
                        c1 => c1.VerifyField("NumberOfSections", (ushort) 5),
                        c1 => c1.VerifyFieldIgnoreValue("TimeDateStamp"),
                        c1 => c1.VerifyField("PointerToSymbolTable", 982),
                        c1 => c1.VerifyField("NumberOfSymbols", 14),
                        c1 => c1.VerifyField("SizeOfOptionalHeader", (short) 0),
                        c1 => c1.VerifyField("Characteristics", (IMAGE_FILE) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SECTION_HEADER", offset: 0x14, size: 40,
                        c1 => c1.VerifyField("Name", ".drectve"),
                        c1 => c1.VerifyField("VirtualSize", 0),
                        c1 => c1.VerifyField("VirtualAddress", 0),
                        c1 => c1.VerifyField("SizeOfRawData", 61),
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
                        c1 => c1.VerifyField("SizeOfRawData", 536),
                        c1 => c1.VerifyField("PointerToRawData", 281),
                        c1 => c1.VerifyField("PointerToRelocations", 817),
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
                        c1 => c1.VerifyField("PointerToRawData", 887),
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
                        c1 => c1.VerifyField("SizeOfRawData", 52),
                        c1 => c1.VerifyField("PointerToRawData", 890),
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
                        c1 => c1.VerifyField("PointerToRawData", 942),
                        c1 => c1.VerifyField("PointerToRelocations", 0),
                        c1 => c1.VerifyField("PointerToLineNumbers", 0),
                        c1 => c1.VerifyField("NumberOfRelocations", (short) 0),
                        c1 => c1.VerifyField("NumberOfLineNumbers", (short) 0),
                        c1 => c1.VerifyField("Characteristics", IMAGE_SCN.LNK_INFO | IMAGE_SCN.LNK_REMOVE)
                    )
                ),
            #endregion
                v => v.VerifySection(name: ".drectve", offset: 0xDC, size: 61,
                    c => c.VerifyValue(offset: 0xDC, "   /DEFAULTLIB:\"MSVCRT\" /DEFAULTLIB:\"OLDNAMES\" /EXPORT:_main ")
                ),
            #region .debug$S
                v => v.VerifySection(name: ".debug$S", offset: 281, size: 536,
                    c => c.VerifyValue(offset: 0x119, value: CV_SIGNATURE.C13),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x11D, size: 157, //DEBUG_S_SYMBOLS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS),
                        c1 => c1.VerifyField("cbLen", 149),
                        c1 => c1.VerifySymType(offset: 0x125, size: 31, SYM_ENUM_e.S_OBJNAME),
                        c1 => c1.VerifySymType(offset: 0x144, size: 60, SYM_ENUM_e.S_COMPILE3),
                        c1 => c1.VerifySymType(offset: 0x180, size: 20, SYM_ENUM_e.S_UNAMESPACE),
                        c1 => c1.VerifySymType(offset: 0x194, size: 22, SYM_ENUM_e.S_UNAMESPACE),
                        c1 => c1.VerifySymType(offset: 0x1AA, size: 8, SYM_ENUM_e.S_UNAMESPACE),
                        c1 => c1.VerifySymType(offset: 0x1B2, size: 8, SYM_ENUM_e.S_UNAMESPACE)
                    ),
                    c => c.VerifyByteBlob(offset: 0x1BA, new byte[3]),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x1BD, size: 44, //DEBUG_S_FRAMEDATA
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA),
                        c1 => c1.VerifyField("cbLen", 36),
                        c1 => c1.VerifyStruct(name: "RVA + FrameData", offset: 0x1C5, size: 40,
                            c2 => c2.VerifyField(name: "RVA", value: 0),
                            c2 => c2.VerifyStruct(name: "FRAMEDATA", offset: 0x1C9, size: 32,
                                c3 => c3.VerifyField(name: "ulRvaStart", value: 0),
                                c3 => c3.VerifyField(name: "cbBlock", value: 3),
                                c3 => c3.VerifyField(name: "cbLocals", value: 0),
                                c3 => c3.VerifyField(name: "cbParams", value: 4),
                                c3 => c3.VerifyField(name: "cbStkMax", value: 0),
                                c3 => c3.VerifyField(name: "frameFunc", value: 24),
                                c3 => c3.VerifyField(name: "cbProlog", value: (short) 0),
                                c3 => c3.VerifyField(name: "cbSavedRegs", value: (short) 0),
                                c3 => c3.VerifyBitField(name: "fHasSEH", value: (byte) 0, bits: 1),
                                c3 => c3.VerifyBitField(name: "fHasEH", value: (byte) 0, bits: 1),
                                c3 => c3.VerifyBitField(name: "fIsFunctionStart", value: (byte) 1, bits: 1),
                                c3 => c3.VerifyBitField(name: "reserved", value: 0, bits: 29)
                            )
                        )
                    ),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x1E9, size: 142, //DEBUG_S_SYMBOLS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS),
                        c1 => c1.VerifyField("cbLen", 134),
                        c1 => c1.VerifySymType(offset: 0x1F1, size: 44, SYM_ENUM_e.S_GPROC32_ID),
                        c1 => c1.VerifySymType(offset: 0x21D, size: 12, SYM_ENUM_e.S_LOCAL),
                        c1 => c1.VerifySymType(offset: 0x229, size: 20, SYM_ENUM_e.S_DEFRANGE_REGISTER_REL),
                        c1 => c1.VerifySymType(offset: 0x23D, size: 8, SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE),
                        c1 => c1.VerifySymType(offset: 0x245, size: 30, SYM_ENUM_e.S_FRAMEPROC),
                        c1 => c1.VerifySymType(offset: 0x263, size: 16, SYM_ENUM_e.S_REGREL32),
                        c1 => c1.VerifySymType(offset: 0x273, size: 4, SYM_ENUM_e.S_PROC_ID_END)
                    ),
                    c => c.VerifyByteBlob(offset: 0x277, new byte[2]),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x279, size: 56, //DEBUG_S_LINES
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES),
                        c1 => c1.VerifyField("cbLen", 48),
                        c1 => c1.VerifyStruct(name: "CV_DebugSLinesHeader_t", offset: 0x281, size: 48,
                            c2 => c2.VerifyField(name: "offCon", 0),
                            c2 => c2.VerifyField(name: "segCon", (short) 0),
                            c2 => c2.VerifyField(name: "flags", (CV_LINES) 0),
                            c2 => c2.VerifyField(name: "cbCon", 3),
                            c2 => c2.VerifyStruct(name: "CV_DebugSLinesFileBlockHeader_t", offset: 0x28D, size: 36,
                                c3 => c3.VerifyField(name: "nLines", 3),
                                c3 => c3.VerifyField(name: "cbBlock", 36),
                                c3 => c3.VerifyStruct(name: "CV_Line_t", offset: 0x299, size: 8,
                                    c4 => c4.VerifyField(name: "offset", value: 0),
                                    c4 => c4.VerifyBitField(name: "linenumStart", value: 2, bits: 24),
                                    c4 => c4.VerifyBitField(name: "deltaLineEnd", value: 0, bits: 7),
                                    c4 => c4.VerifyBitField(name: "fStatement", value: (byte) 1, bits: 1)
                                ),
                                c3 => c3.VerifyStruct(name: "CV_Line_t", offset: 0x2A1, size: 8,
                                    c4 => c4.VerifyField(name: "offset", value: 0),
                                    c4 => c4.VerifyBitField(name: "linenumStart", value: 3, bits: 24),
                                    c4 => c4.VerifyBitField(name: "deltaLineEnd", value: 0, bits: 7),
                                    c4 => c4.VerifyBitField(name: "fStatement", value: (byte) 1, bits: 1)
                                ),
                                c3 => c3.VerifyStruct(name: "CV_Line_t", offset: 0x2A9, size: 8,
                                    c4 => c4.VerifyField(name: "offset", value: 2),
                                    c4 => c4.VerifyBitField(name: "linenumStart", value: 4, bits: 24),
                                    c4 => c4.VerifyBitField(name: "deltaLineEnd", value: 0, bits: 7),
                                    c4 => c4.VerifyBitField(name: "fStatement", value: (byte) 1, bits: 1)
                                )
                            )
                        )
                    ),

                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x2B1, size: 32, //DEBUG_S_FILECHKSMS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS),
                        c1 => c1.VerifyField("cbLen", 24),
                        c1 => c1.VerifyStruct(name: "CV_FileCheckSum", offset: 0x2B9, size: 22,
                            c2 => c2.VerifyField(name: "name", 1),
                            c2 => c2.VerifyField(name: "len", (byte) 16),
                            c2 => c2.VerifyField(name: "type", CV_SourceChksum_t.CHKSUM_TYPE_MD5),
                            c2 => c2.VerifyField(name: "hash", new byte[] { 0x41, 0x11, 0xff, 0x38, 0x4f, 0x65, 0x34, 0x5d, 0x9f, 0xcd, 0xe7, 0xaa, 0xa1, 0xd2, 0x7e, 0xa8 })
                        )
                    ),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x2D1, size: 77, //DEBUG_S_STRINGTABLE
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE),
                        c1 => c1.VerifyField("cbLen", 69),
                        c1 => c1.VerifyValue(offset: 0x2D9, value: string.Empty),
                        c1 => c1.VerifyValueIgnoreValue(offset: 0x2DA), //The path to the obj file
                        c1 => c1.VerifyValue(offset: 0x2F1, value: "$T0 .raSearch = $eip $T0 ^ = $esp $T0 4 + = ")
                    ),
                    c => c.VerifyByteBlob(offset: 0x31E, new byte[3]),
                    c => c.VerifyStruct(name: "CV_DebugSSubsectionHeader_t", offset: 0x321, size: 16, //DEBUG_S_SYMBOLS
                        c1 => c1.VerifyField("type", DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS),
                        c1 => c1.VerifyField("cbLen", 8),
                        c1 => c1.VerifySymType(offset: 0x329, size: 8, SYM_ENUM_e.S_BUILDINFO)
                    )
                ),
            #endregion
            #region Relocations
                v => v.VerifyLogicalRegion(name: "Relocations", offset: 0x331, size: 70,
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x331, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0xAC),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", IMAGE_REL_I386.IMAGE_REL_I386_DIR32NB)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x33B, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0xF8),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", IMAGE_REL_I386.IMAGE_REL_I386_SECREL)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x345, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0xFC),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", IMAGE_REL_I386.IMAGE_REL_I386_SECTION)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x34F, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x11C),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", IMAGE_REL_I386.IMAGE_REL_I386_SECREL)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x359, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x120),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", IMAGE_REL_I386.IMAGE_REL_I386_SECTION)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x363, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x168),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", IMAGE_REL_I386.IMAGE_REL_I386_SECREL)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_RELOCATION", offset: 0x36D, size: 10,
                        c1 => c1.VerifyField(name: "VirtualAddress", 0x16C),
                        c1 => c1.VerifyField(name: "SymbolTableIndex", 9),
                        c1 => c1.VerifyField(name: "Type", IMAGE_REL_I386.IMAGE_REL_I386_SECTION)
                    )
                ),
            #endregion
                v => v.VerifySection(name: ".text$mn", offset: 0x377, size: 3,
                    c => c.VerifyByteBlob(offset: 0x377, new byte[] {0x33, 0xC0, 0xC3})
                ),
                v => v.VerifySection(name: ".debug$T", offset: 0x37A, size: 52,
                    c => c.VerifyValue(offset: 0x37A, value: CV_SIGNATURE.C13),
                    c => c.VerifyTypType(offset: 0x37E, size: 48, type: LEAF_ENUM_e.LF_TYPESERVER2)
                ),
                v => v.VerifySection(name: ".chks64", offset: 0x3AE, size: 40,
                    c => c.VerifyByteBlob(offset: 0x3AE, value: new byte[] {
                        0x22, 0x8f, 0x00, 0x40, 0x54, 0x5a, 0x85, 0x76, 0xcb, 0x49,
                        0x7b, 0xa5, 0x45, 0x81, 0x9d, 0x54, 0x4b, 0xc7, 0x24, 0xf3,
                        0xb1, 0xd0, 0xca, 0xf4, 0xdf, 0x1f, 0x75, 0x68, 0x7f, 0xfb,
                        0x56, 0x4d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                    })
                ),
            #region Coff Symbol Table
                v => v.VerifyStruct(name: "Coff Symbol Table", offset: 0x3D6, size: 256,
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x3D6, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@comp.id"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x10575BC),
                        c1 => c1.VerifyField(name: "SectionNumber", ushort.MaxValue),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x3E8, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@feat.00"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x80010091),
                        c1 => c1.VerifyField(name: "SectionNumber", ushort.MaxValue),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x3FA, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@vol.md"),
                        c1 => c1.VerifyField(name: "Value", (uint) 2),
                        c1 => c1.VerifyField(name: "SectionNumber", ushort.MaxValue),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x40C, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".drectve"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 1),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x41E, size: 18,
                            c2 => c2.VerifyField("Bytes", value: new byte[]{0x3d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4b, 0x22, 0x07, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x430, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".debug$S"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 2),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x442, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{ 0x18, 0x02, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x454, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".text$mn"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 3),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x466, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{ 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8d, 0x1f, 0xba, 0xef, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x478, size: 18,
                        c1 => c1.VerifyField(name: "Name", "_main"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 3),
                        c1 => c1.VerifyField(name: "Type", (IMAGE_SYM_TYPE) 0x20), //IMAGE_SYM_DTYPE_FUNCTION bit shifted. See ImageSymtype for info
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_EXTERNAL),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x48A, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".debug$T"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 4),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x49C, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{ 0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x4AE, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".chks64"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 5),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x4C0, size: 18,
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
                v => v.VerifyHeader(size: 332,
                    c => c.VerifyStruct(name: "ANON_OBJECT_HEADER", offset: 0, size: 32,
                        c1 => c1.VerifyField(name: "Sig1", value: IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN),
                        c1 => c1.VerifyField(name: "Sig2", value: (short) -1),
                        c1 => c1.VerifyField(name: "Version", value: (short) 1),
                        c1 => c1.VerifyField(name: "Machine", value: IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386),
                        c1 => c1.VerifyFieldIgnoreValue(name: "TimeDateStamp"),
                        c1 => c1.VerifyField(name: "ClassID", value: new Guid("0cb3fe38-d9a5-4dab-ac9b-d6b6222653c2")), //It's written in c2!UtcCOMWriteCilObjHeader and queried in c2!DllGetObjHandler
                        c1 => c1.VerifyField(name: "SizeOfData", value: 2411)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_FILE_HEADER", offset: 0x20, size: 20,
                        c1 => c1.VerifyField("Machine", (IMAGE_FILE_MACHINE) 0xC13),
                        c1 => c1.VerifyField("NumberOfSections", (ushort) 7),
                        c1 => c1.VerifyFieldIgnoreValue("TimeDateStamp"),
                        c1 => c1.VerifyField("PointerToSymbolTable", 2101),
                        c1 => c1.VerifyField("NumberOfSymbols", 17),
                        c1 => c1.VerifyField("SizeOfOptionalHeader", (short) 0),
                        c1 => c1.VerifyField("Characteristics", (IMAGE_FILE) 0)
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
                        c1 => c1.VerifyField("SizeOfRawData", 146),
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
                        c1 => c1.VerifyField("SizeOfRawData", 1120),
                        c1 => c1.VerifyField("PointerToRawData", 573),
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
                        c1 => c1.VerifyField("PointerToRawData", 1693),
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
                        c1 => c1.VerifyField("PointerToRawData", 1694),
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
                        c1 => c1.VerifyField("PointerToRawData", 1917),
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
                        c1 => c1.VerifyField("SizeOfRawData", 154),
                        c1 => c1.VerifyField("PointerToRawData", 1947),
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
                v => v.VerifySectionIgnoreChildren(name: ".cil$fg", offset: 459, size: 146),
                v => v.VerifySectionIgnoreChildren(name: ".cil$gl", offset: 605, size: 1120),
                v => v.VerifySectionIgnoreChildren(name: ".cil$in", offset: 1725, size: 1),
                v => v.VerifySectionIgnoreChildren(name: ".cil$ex", offset: 1726, size: 223),
                v => v.VerifySectionIgnoreChildren(name: ".cil$sy", offset: 1949, size: 30),
                v => v.VerifySectionIgnoreChildren(name: ".cil$db", offset: 1979, size: 154),
            #region Coff Symbol Table
                v => v.VerifyStruct(name: "Coff Symbol Table", offset: 0x855, size: 310,
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x855, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@comp.id"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x10575BC),
                        c1 => c1.VerifyField(name: "SectionNumber", ushort.MaxValue),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x867, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@feat.00"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0x80010091),
                        c1 => c1.VerifyField(name: "SectionNumber", ushort.MaxValue),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x879, size: 18,
                        c1 => c1.VerifyField(name: "Name", "@vol.md"),
                        c1 => c1.VerifyField(name: "Value", (uint) 2),
                        c1 => c1.VerifyField(name: "SectionNumber", ushort.MaxValue),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 0)
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x88B, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".drectve"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 1),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x89D, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{127,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x8AF, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$fg"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 2),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x8C1, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{146,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x8D3, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$gl"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 3),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x8E5, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{96,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x8F7, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$in"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 4),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x909, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x91B, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$ex"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 5),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x92D, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{223,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x93F, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$sy"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 6),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x951, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{30,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
                        )
                    ),
                    c => c.VerifyStruct(name: "IMAGE_SYMBOL", offset: 0x963, size: 36,
                        c1 => c1.VerifyField(name: "Name", ".cil$db"),
                        c1 => c1.VerifyField(name: "Value", (uint) 0),
                        c1 => c1.VerifyField(name: "SectionNumber", (ushort) 7),
                        c1 => c1.VerifyField(name: "Type", IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL),
                        c1 => c1.VerifyField(name: "StorageClass", IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                        c1 => c1.VerifyField(name: "NumberOfAuxSymbols", (byte) 1),
                        c1 => c1.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0x975, size: 18,
                            c2 => c2.VerifyField(name: "Bytes", value: new byte[]{154,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0})
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

            var symbols = (SymTypeList) subSection.Data;

            Assert.AreEqual("C:\\TestApp\\TestApp.obj", symbols[0].ToString());
        }

        [TestMethod]
        public void OBJ_C13_TypType_Strings()
        {
            //C13 strings should be UTF8 on TypType as well
            using var objFile = OBJFile.FromFile(Sample.OBJ_C13);

            var typeTable = objFile.SectionData.OfType<OBJTypesTable>().First();

            Assert.AreEqual(CV_SIGNATURE.C13, typeTable.Signature);

            var types = typeTable.List;

            Assert.AreEqual("C:\\TestApp\\vc140.pdb", types.First().ToString());
        }

        [TestMethod]
        public void OBJ_C7_SymType_Strings()
        {
            //C7 strings should be ST
            using var objFile = OBJFile.FromFile(Sample.OBJ_C11);

            var symbolTable = objFile.SectionData.OfType<OBJSymbolsTable>().First();

            Assert.AreEqual(CV_SIGNATURE.C11, symbolTable.Signature);

            var symbols = symbolTable.C7Symbols;

            Assert.AreEqual("Debug/main.obj", symbols.First().ToString());
        }

        [TestMethod]
        public void OBJ_C7_TypType_Strings()
        {
            //C7 strings should be ST on TypType as well
            using var objFile = OBJFile.FromFile(Sample.OBJ_C11);

            var typeTable = objFile.SectionData.OfType<OBJTypesTable>().First();

            Assert.AreEqual(CV_SIGNATURE.C11, typeTable.Signature);

            var types = typeTable.List;

            Assert.AreEqual("c:\\program files (x86)\\devstudio\\myprojects\\testapp\\debug\\vc50.pdb", types.First().ToString());
        }

        private void TestObj(
            string testName,
            bool ltcg,
            params Action<IView>[] verify)
        {
            var objFile = ltcg ? Sample.VS22_LTCG_OBJ : Sample.VS22_OBJ;

            using var obj = OBJFile.FromFile(objFile);

            _ = obj.SectionData;

            var views = obj.GetView().Children;

            Assert.AreEqual(views.Count, verify.Length);

            for (var i = 0; i < verify.Length; i++)
                verify[i](views[i]);
        }
    }
}
