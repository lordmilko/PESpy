using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageAuxSymbol : IValue, IViewable
    {
#if PEFAST
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
            public Span<short> Dimension => chunk.PeekSpan<short>(0, 4);

            private readonly MemoryChunk chunk;

            internal ArrayData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }

        public readonly struct CrcData
        {
            public int crc => chunk.PeekInt32(0);

            public Span<byte> rgbReserved => chunk.PeekSpan<byte>(4, 14);

            private readonly MemoryChunk chunk;

            internal CrcData(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }
#endif

#if PEFAST
        //IMAGE_AUX_SYMBOL has a number of unioned fields; don't know how to detect which one is in use
        public Span<byte> Bytes => chunk.PeekSpan<byte>(0, StructSize);

        public int Offset => chunk.AbsoluteOffset;
#else
        public byte[] Bytes { get; }

        public int Offset { get; }
#endif

        internal const int StructSize = 18;

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageAuxSymbol(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageAuxSymbol(IFileReader reader)
        {
            Offset = (int) reader.Position;

            //IMAGE_AUX_SYMBOL has a number of unioned fields; don't know how to detect which one is in use
            Bytes = reader.ReadBytes(18);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_AUX_SYMBOL), this, ViewKind.ImageAuxSymbol);

#if PEFAST
            throw new NotImplementedException();
#else
            s.WriteField("Bytes", Bytes);
#endif
        }
    }
}
