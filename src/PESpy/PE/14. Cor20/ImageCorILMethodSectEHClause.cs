using ClrDebug;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageCorILMethodSectEHClause : IValue, IViewable
    {
        public CorExceptionFlag Flags { get; }

        public int TryOffset { get; }

        public int TryLength { get; }

        public int HandlerOffset { get; }

        public int HandlerLength { get; }

        public mdToken ClassToken { get; }

        public int FilterOffset { get; }

        public int Offset { get; }

        internal ImageCorILMethodSectEHClause(in MemoryChunk chunk, bool isFat, ref int read)
        {
            Offset = chunk.AbsoluteOffset;

            if (isFat)
            {
                Flags = (CorExceptionFlag) chunk.PeekUInt32(read);
                TryOffset = chunk.PeekInt32(read + 4);
                TryLength = chunk.PeekInt32(read + 8);
                HandlerOffset = chunk.PeekInt32(read + 12);
                HandlerLength = chunk.PeekInt32(read + 16);

                read += 20;
            }
            else
            {
                Flags = (CorExceptionFlag) chunk.PeekUInt16(read);
                TryOffset = chunk.PeekUInt16(read + 2);
                TryLength = chunk.PeekByte(read + 4);
                HandlerOffset = chunk.PeekUInt16(read + 5);
                HandlerLength = chunk.PeekByte(read + 7);

                read += 8;
            }

            //There are several other flags that can be set; extract out the actual kind of handler it is
            var kind = Flags & (CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT);

            ClassToken = default;
            FilterOffset = default;

            //Read the value at offset 8 (small) / 20 (fat)
            switch (kind)
            {
                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT:
                    ClassToken = chunk.PeekUInt32(read);
                    read += 4;
                    break;

                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER:
                    FilterOffset = chunk.PeekInt32(read);
                    read += 4;
                    break;

                default:
                    //Value points to junk
                    ClassToken = chunk.PeekUInt32(read);
                    read += 4;
                    break;
            }
        }
    }
}
