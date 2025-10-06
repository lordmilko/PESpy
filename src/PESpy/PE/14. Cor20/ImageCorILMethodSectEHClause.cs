using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("Flags = {Flags}, TryOffset = {TryOffset}, TryLength = {TryLength}, HandlerOffset = {HandlerOffset}, ClassToken = {ClassToken}, FilterOffset = {FilterOffset}")]
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

        private const int FatSize =
            sizeof(int) + //Flags
            sizeof(int) + //TryOffset
            sizeof(int) + //TryLength
            sizeof(int) + //HandlerOffset
            sizeof(int) + //HandlerLength
            sizeof(int); //FilterOffset        

        private const int TinySize =
            sizeof(short) + //Flags
            sizeof(short) + //TryOffset
            sizeof(byte) + //TryLength
            sizeof(short) + //HandlerOffset
            sizeof(byte) + //HandlerLength
            sizeof(int); //FilterOffset

        internal int StructSize => isFat ? FatSize : TinySize;

        private readonly bool isFat;

        internal ImageCorILMethodSectEHClause(in MemoryChunk chunk, bool isFat, ref int read)
        {
            Offset = chunk.AbsoluteOffset + read;
            this.isFat = isFat;

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            if (isFat)
            {
                s.WriteField(nameof(Flags), Flags, sizeof(int));
                s.WriteField(nameof(TryOffset), TryOffset);
                s.WriteField(nameof(TryLength), TryLength);
                s.WriteField(nameof(HandlerOffset), HandlerOffset);
                s.WriteField(nameof(HandlerLength), HandlerLength);
            }
            else
            {
                s.WriteField(nameof(Flags), Flags, sizeof(short));
                s.WriteField(nameof(TryOffset), (short) TryOffset);
                s.WriteField(nameof(TryLength), (byte) TryLength);
                s.WriteField(nameof(HandlerOffset), (short) HandlerOffset);
                s.WriteField(nameof(HandlerLength), (byte) HandlerLength);
            }

            var kind = Flags & (CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT);

            switch (kind)
            {
                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT:
                default:
                    s.WriteField(nameof(ClassToken), ClassToken);
                    break;

                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER:
                    s.WriteField(nameof(FilterOffset), ClassToken);
                    break;
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
