using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public unsafe struct FuncInfo4 : IViewableValue
    {
        public FuncInfoHeader header { get; }

        //I am skeptical this uses the same enum as BBT_UNIQUE_FUNCINFO
        public uint bbtFlags { get; }

        public RVA<UWMap4> dispUnwindMap { get; }

        public RVA<TryBlockMap4> dispTryBlockMap { get; }

        //The true field is dispIPtoStateMap, but sometimes it points to SepIPtoStateMap4.
        //I named dispToSegMap after a variable ehstate_export.h uses to read the sep map
        public RVA<IPtoStateMap4> dispIPtoStateMap { get; }
        public RVA<SepIPtoStateMap4> dispToSegMap { get; }

        public uint dispFrame { get; } //Not an RVA to anything

        public int Offset { get; }

        private readonly byte* _pBytes;

        internal int StructSize
        {
            get
            {
                var header = this.header;

                var pBytes = _pBytes +
                    sizeof(byte); //header

                if (header.BBT)
                    ReadUnsigned(ref pBytes);

                if (header.UnwindMap)
                    pBytes += sizeof(int);

                if (header.TryBlockMap)
                    pBytes += sizeof(int);

                pBytes += sizeof(int); //dispIPtoStateMap / dispToSegMap

                if (header.isCatch)
                    ReadUnsigned(ref pBytes);

                return (int) (pBytes - _pBytes);
            }
        }

        //For use by ViewProvider only
        internal FuncInfo4(in MemoryChunk chunk) : this(chunk, 0)
        {
        }

        internal FuncInfo4(in MemoryChunk chunk, int functionAddress)
        {
            Offset = chunk.AbsoluteOffset;
            _pBytes = chunk.Pointer;

            //Too complex to read lazily

            var pBytes = chunk.Pointer;

            header = new FuncInfoHeader(Offset, *pBytes);
            pBytes++;

            if (header.BBT)
                bbtFlags = ReadUnsigned(ref pBytes);
            else
                bbtFlags = default;

            var peFile = chunk.PEFile();

            MemoryChunk valueChunk;

            #region UnwindMap

            if (header.UnwindMap)
            {
                var dispUnwindMap = ReadInt(ref pBytes);

                if (peFile.TryGetValueChunkFromSection(dispUnwindMap, out valueChunk))
                {
                    var map = new UWMap4(valueChunk, functionAddress);

                    this.dispUnwindMap = new RVA<UWMap4>(
                        dispUnwindMap,
                        map.Offset,
                        map
                    );
                }
                else
                    this.dispUnwindMap = new RVA<UWMap4>(dispUnwindMap);
            }
            else
                dispUnwindMap = default;

            #endregion
            #region TryBlockMap

            if (header.TryBlockMap)
            {
                var dispTryBlockMap = ReadInt(ref pBytes);

                if (peFile.TryGetValueChunkFromSection(dispTryBlockMap, out valueChunk))
                {
                    var map = new TryBlockMap4(valueChunk, functionAddress);

                    this.dispTryBlockMap = new RVA<TryBlockMap4>(
                        dispTryBlockMap,
                        map.Offset,
                        map
                    );
                }
                else
                    this.dispTryBlockMap = new RVA<TryBlockMap4>(dispTryBlockMap);
            }
            else
                dispTryBlockMap = default;

            #endregion
            #region SegMap / IPtoStateMap

            //dispIPtoStateMap always exists, its just a question or what type of entity it points to
            if (header.isSeparated)
            {
                var dispToSegMap = ReadInt(ref pBytes);

                if (peFile.TryGetValueChunkFromSection(dispToSegMap, out valueChunk))
                {
                    var map = new SepIPtoStateMap4(valueChunk, functionAddress);

                    this.dispToSegMap = new RVA<SepIPtoStateMap4>(
                        dispToSegMap,
                        map.Offset,
                        map
                    );
                }
                else
                    this.dispToSegMap = new RVA<SepIPtoStateMap4>(dispToSegMap);
            }
            else
            {
                var dispIPtoStateMap = ReadInt(ref pBytes);

                if (peFile.TryGetValueChunkFromSection(dispIPtoStateMap, out valueChunk))
                {
                    var map = new IPtoStateMap4(valueChunk, functionAddress);

                    this.dispIPtoStateMap = new RVA<IPtoStateMap4>(
                        dispIPtoStateMap,
                        map.Offset,
                        map
                    );
                }
                else
                    this.dispIPtoStateMap = new RVA<IPtoStateMap4>(dispIPtoStateMap);
            }

            #endregion

            if (header.isCatch)
                dispFrame = ReadUnsigned(ref pBytes);
            else
                dispFrame = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            var pBytes = _pBytes;

            pBytes++;

            if (header.BBT)
                ReadUnsigned(ref pBytes); //bbtFlags

            if (header.UnwindMap)
            {
                writer.WriteUniqueRVAField(dispUnwindMap, offset, (int) (pBytes - _pBytes));
                ReadInt(ref pBytes); //dispUnwindMap
            }

            if (header.TryBlockMap)
            {
                writer.WriteUniqueRVAField(dispTryBlockMap, offset, (int) (pBytes - _pBytes));
                ReadInt(ref pBytes); //dispTryBlockMap
            }

            if (header.isSeparated)
                writer.WriteUniqueRVAField(dispToSegMap, offset, (int) (pBytes - _pBytes));
            else
                writer.WriteUniqueRVAField(dispIPtoStateMap, offset, (int) (pBytes - _pBytes));

            ReadInt(ref pBytes); //dispIPtoStateMap

            if (header.isCatch)
                ReadUnsigned(ref pBytes); //dispFrame
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FuncInfo4, this, ViewKind.FuncInfo4, StructSize);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            var pBytes = _pBytes;

            using var s = structWriter.CreateEagerWriter();

            s.WriteStructField(nameof(header), header, FuncInfoHeader.StructSize);

            pBytes++;

            if (header.BBT)
            {
                var before = pBytes;
                var bbtFlags = ReadUnsigned(ref pBytes);
                var length = (int) (pBytes - before);
                s.WriteField(nameof(bbtFlags), bbtFlags, length);
            }

            if (header.UnwindMap)
            {
                var dispUnwindMap = ReadInt(ref pBytes);
                s.WriteField(nameof(dispUnwindMap), dispUnwindMap);
            }

            if (header.TryBlockMap)
            {
                var dispTryBlockMap = ReadInt(ref pBytes);
                s.WriteField(nameof(dispTryBlockMap), dispTryBlockMap);
            }

            var dispIPtoStateMap = ReadInt(ref pBytes);
            s.WriteField(
                header.isSeparated
                    ? nameof(dispToSegMap)
                    : nameof(dispIPtoStateMap),
                dispIPtoStateMap
            );

            if (header.isCatch)
            {
                var before = pBytes;
                var dispFrame = ReadUnsigned(ref pBytes);
                var length = (int) (pBytes - before);
                s.WriteField(nameof(dispFrame), dispFrame, length);
            }

            structWriter.EagerFields = s.ToArray();
        }

        #region Decompression

        // Constants for decompression.
        private static sbyte[] s_negLengthTab =
        {
            -1,    // 0
            -2,    // 1
            -1,    // 2
            -3,    // 3

            -1,    // 4
            -2,    // 5
            -1,    // 6
            -4,    // 7

            -1,    // 8
            -2,    // 9
            -1,    // 10
            -3,    // 11

            -1,    // 12
            -2,    // 13
            -1,    // 14
            -5,    // 15
        };

        private static sbyte[] s_shiftTab =
        {
            32 - 7 * 1,    // 0
            32 - 7 * 2,    // 1
            32 - 7 * 1,    // 2
            32 - 7 * 3,    // 3

            32 - 7 * 1,    // 4
            32 - 7 * 2,    // 5
            32 - 7 * 1,    // 6
            32 - 7 * 4,    // 7

            32 - 7 * 1,    // 8
            32 - 7 * 2,    // 9
            32 - 7 * 1,    // 10
            32 - 7 * 3,    // 11

            32 - 7 * 1,    // 12
            32 - 7 * 2,    // 13
            32 - 7 * 1,    // 14
            0,             // 15
        };

        internal static uint ReadUnsigned(ref byte* pbEncoding)
        {
            uint lengthBits = (uint) (*pbEncoding & 0x0F);
            int negLength = s_negLengthTab[lengthBits];
            int shift = s_shiftTab[lengthBits];
            uint result = *(uint*) (pbEncoding - negLength - 4);

            result >>= shift;
            pbEncoding -= negLength;

            return result;
        }

        internal static int GetLength(uint value)
        {
            // Lower 4 bits of the MSB determine the number of bytes to read:
            // XXX0: 1 byte
            // XX01: 2 bytes
            // X011: 3 bytes
            // 0111: 4 bytes
            // 1111: 5 bytes

            if (value < 128)
                return 1;
            else if (value < 128 * 128)
                return 2;
            else if (value < 128 * 128 * 128)
                return 3;
            else if (value < 128 * 128 * 128 * 128)
                return 4;
            else
                return 5;
        }

        internal static int ReadInt(ref byte* buffer)
        {
            int value = *(int*) buffer;
            buffer += sizeof(int);
            return value;
        }

        #endregion
    }
}
