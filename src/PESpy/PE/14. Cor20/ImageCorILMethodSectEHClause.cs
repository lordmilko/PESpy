using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("Flags = {Flags}, TryOffset = {TryOffset}, TryLength = {TryLength}, HandlerOffset = {HandlerOffset}, ClassToken = {ClassToken}, FilterOffset = {FilterOffset}")]
    public readonly struct ImageCorILMethodSectEHClause : IValue, IViewable
    {
        private const int FatFlagsOffset = 0;
        private const int FatTryOffsetOffset = 4;
        private const int FatTryLengthOffset = 8;
        private const int FatHandlerOffsetOffset = 12;
        private const int FatHandlerLengthOffset = 16;
        private const int FatFilterOffsetOffset = 20;

        private const int TinyFlagsOffset = 0;
        private const int TinyTryOffsetOffset = 2;
        private const int TinyTryLengthOffset = 4;
        private const int TinyHandlerOffsetOffset = 5;
        private const int TinyHandlerLengthOffset = 7;
        private const int TinyFilterOffsetOffset = 8;

        public CorExceptionFlag Flags { get; }

        public int TryOffset { get; }

        public int TryLength { get; }

        public int HandlerOffset { get; }

        public int HandlerLength { get; }

        public mdToken ClassToken { get; }

        public int FilterOffset { get; }

        public int Offset { get; }

        internal const int FatSize =
            sizeof(int) + //Flags
            sizeof(int) + //TryOffset
            sizeof(int) + //TryLength
            sizeof(int) + //HandlerOffset
            sizeof(int) + //HandlerLength
            sizeof(int); //FilterOffset

        internal const int TinySize =
            sizeof(short) + //Flags
            sizeof(short) + //TryOffset
            sizeof(byte) + //TryLength
            sizeof(short) + //HandlerOffset
            sizeof(byte) + //HandlerLength
            sizeof(int); //FilterOffset

        internal int StructSize => isFat ? FatSize : TinySize;

        private readonly bool isFat;

        internal ImageCorILMethodSectEHClause(in MemoryChunk chunk, bool isFat)
        {
            Offset = chunk.AbsoluteOffset;
            this.isFat = isFat;

            int filterOffsetOffset;

            if (isFat)
            {
                Flags = (CorExceptionFlag) chunk.PeekUInt32(FatFlagsOffset);
                TryOffset = chunk.PeekInt32(FatTryOffsetOffset);
                TryLength = chunk.PeekInt32(FatTryLengthOffset);
                HandlerOffset = chunk.PeekInt32(FatHandlerOffsetOffset);
                HandlerLength = chunk.PeekInt32(FatHandlerLengthOffset);

                filterOffsetOffset = FatFilterOffsetOffset;
            }
            else
            {
                Flags = (CorExceptionFlag) chunk.PeekUInt16(TinyFlagsOffset);
                TryOffset = chunk.PeekUInt16(TinyTryOffsetOffset);
                TryLength = chunk.PeekByte(TinyTryLengthOffset);
                HandlerOffset = chunk.PeekUInt16(TinyHandlerOffsetOffset);
                HandlerLength = chunk.PeekByte(TinyHandlerLengthOffset);

                filterOffsetOffset = TinyFilterOffsetOffset;
            }

            //There are several other flags that can be set; extract out the actual kind of handler it is
            var kind = Flags & (CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT);

            ClassToken = default;
            FilterOffset = default;

            //Read the value at offset 8 (small) / 20 (fat)
            switch (kind)
            {
                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT:
                    ClassToken = chunk.PeekUInt32(filterOffsetOffset);
                    break;

                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER:
                    FilterOffset = chunk.PeekInt32(filterOffsetOffset);
                    break;

                default:
                    //Value points to junk
                    ClassToken = chunk.PeekUInt32(filterOffsetOffset);
                    break;
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer)
        {
            return writer.NewStruct(
                isFat ? Strings.IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT : Strings.IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL,
                this,
                ViewKind.ImageCorILMethodSectEHClause,
                StructSize
            );
        }

        int IViewable.NumChildren => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    if (isFat)
                        structWriter.WriteField(nameof(Flags), FatFlagsOffset, Flags, sizeof(int));
                    else
                        structWriter.WriteField(nameof(Flags), TinyFlagsOffset, Flags, sizeof(short));

                    break;

                case 1:
                    if (isFat)
                        structWriter.WriteField(nameof(TryOffset), FatTryOffsetOffset, TryOffset);
                    else
                        structWriter.WriteField(nameof(TryOffset), TinyTryOffsetOffset, (short) TryOffset);

                    break;

                case 2:
                    if (isFat)
                        structWriter.WriteField(nameof(TryLength), FatTryLengthOffset, TryLength);
                    else
                        structWriter.WriteField(nameof(TryLength), TinyTryLengthOffset, (byte) TryLength);

                    break;

                case 3:
                    if (isFat)
                        structWriter.WriteField(nameof(HandlerOffset), FatHandlerOffsetOffset, HandlerOffset);
                    else
                        structWriter.WriteField(nameof(HandlerOffset), TinyHandlerOffsetOffset, (short) HandlerOffset);

                    break;

                case 4:
                    if (isFat)
                        structWriter.WriteField(nameof(HandlerLength), FatHandlerLengthOffset, HandlerLength);
                    else
                        structWriter.WriteField(nameof(HandlerLength), TinyHandlerLengthOffset, (byte) HandlerLength);

                    break;

                case 5:
                    var kind = Flags & (CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT);

                    switch (kind)
                    {
                        case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT:
                        default:
                            structWriter.WriteField(nameof(ClassToken), FatFilterOffsetOffset, ClassToken);
                            break;

                        case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER:
                            structWriter.WriteField(nameof(FilterOffset), TinyFilterOffsetOffset, ClassToken);
                            break;
                    }

                    break;
            }
        }
    }
}
