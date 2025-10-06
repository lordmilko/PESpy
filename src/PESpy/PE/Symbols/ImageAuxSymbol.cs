using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /* IMAGE_AUX_SYMBOL is a very complicated union of symbol types. The key to understanding
     * which union is being used is to look at the type of IMAGE_SYMBOL that the IMAGE_AUX_SYMBOL is associated with
     * https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#auxiliary-symbol-records
     *
     * IMAGE_AUX_SYMBOLS_EX seems to be 20 bytes not 18, and contains additional unions that may exist. I don't know how you're meant to know
     * when IMAGE_AUX_SYMBOLS_EX is in use (being 20 bytes not 18)
     *
     * The following categories of AUX symbols exist
     *
     * | Type                 | Criteria |
     * |----------------------|----------|-------------------------
     * | Function Definition  | StorageClass EXTERNAL, Type Function, Section Number > 0              | Sym (TagIndex / TotalSize / PointerToLinenumber / PointerToNextFunction / Unused)
     * | .bf / .ef            | StorageClass FUNCTION. Name .bf or .ef. .lf does not have aux records | Sym (Unused, Linenumber, Unused, PointerToNextFunction (.bf only) / Unused
     * | Weak Externals       | StorageClass EXTERNAL, UNDEF section number, value of 0               | I think this is IMAGE_AUX_SYMBOLS_EX.Sym (MSDN says unused is 10 bytes but the header says 12?)
     * | Files                | StorageClass FILE                                                     | File
     * | Section Definitions  | StorageCLass STATIC, Symbol name names a section                      | Section
     * | COMDAT Sections      | I think we need a symbol that names a section, value of 0, Type Null, CLass Static, and section has IMAGE_SCN_LNK_COMDAT
     * | CLR Token Definition | Class IMAGE_SYM_CLASS_CLR_TOKEN                                       | TokenDef
     *
     * IMAGE_AUX_SYMBOL contains the following top level structures
     *
     * Sym
     * File
     * Section
     * TokenDef
     * CRC
     * */

    public enum AuxSymbolKind
    {
        Unknown = 0,
        Function,
        BFOrEF,
        WeakExternal,
        File,
        SectionDef,
        ComdatSection,
        CLRToken
    }

    public readonly struct ImageAuxSymbol : IValue, IViewable
    {
        #region Union

        public SymData Sym => new SymData(chunk);

        public FixedAnsiString File => chunk.PeekAnsiFixedLength(0, 18);

        public SectionData Section => new SectionData(chunk);

        public ImageAuxSymbolTokenDef TokenDef => new ImageAuxSymbolTokenDef(chunk);

        public CrcData CRC => new CrcData(chunk);

        #endregion

        //Name is made up
        public readonly struct SymData
        {
            /// <summary>
            /// struct, union, or enum tag index
            /// </summary>
            public int TagIndex => chunk.PeekInt32(0);

            public MiscData Misc => new MiscData(chunk.Slice(4)); //Occupies 4 bytes

            public FcnAryData FcnAry => new FcnAryData(chunk.Slice(8)); //Occupies 8 bytes

            /// <summary>
            /// tv index
            /// </summary>
            public short TvIndex => chunk.PeekInt16(12);

            private readonly MemoryChunk chunk;

            internal SymData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        //Name is made up
        public readonly struct SectionData
        {
            /// <summary>
            /// section length
            /// </summary>
            public int Length => chunk.PeekInt32(0);

            /// <summary>
            /// number of relocation entries
            /// </summary>
            public short NumberOfRelocations => chunk.PeekInt16(4);

            /// <summary>
            /// number of line numbers
            /// </summary>
            public short NumberOfLinenumbers => chunk.PeekInt16(6);

            /// <summary>
            /// checksum for communal
            /// </summary>
            public uint CheckSum => chunk.PeekUInt32(8);

            /// <summary>
            /// section number to associate with
            /// </summary>
            public short Number => chunk.PeekInt16(12);

            /// <summary>
            /// communal selection type
            /// </summary>
            public byte Selection => chunk.PeekByte(14);

            public byte bReserved => chunk.PeekByte(15);

            /// <summary>
            /// high bits of the section number
            /// </summary>
            public short HighNumber => chunk.PeekInt16(16);

            private readonly MemoryChunk chunk;

            internal SectionData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        public readonly struct MiscData
        {
            #region Union

            public LnSzData LnSz => new LnSzData(chunk); //4 bytes

            public int TotalSize => chunk.PeekInt32(0);

            #endregion

            private readonly MemoryChunk chunk;

            internal MiscData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        public readonly struct LnSzData
        {
            /// <summary>
            /// declaration line number
            /// </summary>
            public short Linenumber => chunk.PeekInt16(0);

            /// <summary>
            /// size of struct, union, or enum
            /// </summary>
            public short Size => chunk.PeekInt16(2);

            private readonly MemoryChunk chunk;

            internal LnSzData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        public readonly struct FcnAryData
        {
            #region Union

            /// <summary>
            /// if ISFCN, tag, or .bb
            /// </summary>
            public FunctionData Function => new FunctionData(chunk); //8 bytes

            /// <summary>
            /// if ISARY, up to 4 dimen.
            /// </summary>
            public ArrayData Array => new ArrayData(chunk);

            #endregion

            private readonly MemoryChunk chunk;

            internal FcnAryData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        public readonly struct FunctionData
        {
            public int PointerToLinenumber => chunk.PeekInt32(0);

            public int PointerToNextFunction => chunk.PeekInt32(4);

            private readonly MemoryChunk chunk;

            internal FunctionData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        public readonly struct ArrayData
        {
            public NativeSpan<short> Dimension => chunk.PeekNativeSpan<short>(0, 4);

            private readonly MemoryChunk chunk;

            internal ArrayData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        public readonly struct CrcData
        {
            public int crc => chunk.PeekInt32(0);

            public NativeSpan<byte> rgbReserved => chunk.PeekNativeSpan<byte>(4, 14);

            private readonly MemoryChunk chunk;

            internal CrcData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        //IMAGE_AUX_SYMBOL has a number of unioned fields. The data that is in effect depends on the data in the parent IMAGE_SYMBOL
        public NativeSpan<byte> Bytes => chunk.PeekNativeSpan<byte>(0, StructSize);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize = 18;

        public AuxSymbolKind Kind { get; }

        private readonly MemoryChunk chunk;

        internal ImageAuxSymbol(in MemoryChunk chunk, AuxSymbolKind kind = AuxSymbolKind.Unknown)
        {
            this.chunk = chunk;
            Kind = kind;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_AUX_SYMBOL, this, ViewKind.ImageAuxSymbol, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            //We don't currently calculate our Kind
            Debug.Assert(Kind == AuxSymbolKind.Unknown);
            s.WriteField("Bytes", Bytes);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
