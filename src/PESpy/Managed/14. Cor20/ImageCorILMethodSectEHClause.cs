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

        internal ImageCorILMethodSectEHClause(IFileReader reader, bool isFat)
        {
            Offset = (int) reader.Position;

            if (isFat)
            {
                Flags = (CorExceptionFlag) reader.ReadInt32();
                TryOffset = reader.ReadInt32();
                TryLength = reader.ReadInt32();
                HandlerOffset = reader.ReadInt32();
                HandlerLength = reader.ReadInt32();
            }
            else
            {
                Flags = (CorExceptionFlag) reader.ReadUInt16();
                TryOffset = reader.ReadUInt16();
                TryLength = reader.ReadByte();
                HandlerOffset = reader.ReadUInt16();
                HandlerLength = reader.ReadByte();
            }

            //There are several other flags that can be set; extract out the actual kind of handler it is
            var kind = Flags & (CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY | CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT);

            ClassToken = default;
            FilterOffset = default;

            //Read the value at offset 8 (small) / 20 (fat)
            switch (kind)
            {
                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FAULT:
                    ClassToken = reader.ReadUInt32();
                    break;

                case CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FILTER:
                    FilterOffset = reader.ReadInt32();
                    break;

                default:
                    //Value points to junk
                    ClassToken = reader.ReadUInt32();
                    break;
            }
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            var isFat = writer.CurrentTag == ViewTag.FatEH;

            using var s = writer.CreateStruct(
                isFat ? nameof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) : nameof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL),
                this,
                ViewKind.ImageCorILMethodSectEHClause
            );

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
        }
    }
}
