using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct HandlerType : IValue, IViewable
    {
        private const int AdjectivesOffset = 0;
        internal const int TypeOffset = 4;
        private const int CatchObjOffset = 8;
        private const int HandlerOffset = 12;
        private const int FrameOffset = 16;

        public int Adjectives => chunk.PeekInt32(AdjectivesOffset);

        private RVA<TypeDescriptor> type;

        public RVA<TypeDescriptor> Type
        {
            get
            {
                if (type.ListedOffset == 0)
                {
                    var dispType = chunk.PeekInt32(TypeOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispType, out var valueChunk))
                        type = new RVA<TypeDescriptor>(dispType, dispType, new TypeDescriptor(valueChunk));
                    else
                        type = new RVA<TypeDescriptor>(dispType);
                }

                return type;
            }
        }

        public int CatchObj => chunk.PeekInt32(CatchObjOffset);

        public int Handler => chunk.PeekInt32(HandlerOffset);

        public int Frame => chunk.PeekInt32(FrameOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //Adjectives
            sizeof(int) + //Type
            sizeof(int) + //CatchObj
            sizeof(int) + //Handler
            sizeof(int); //Frame

        private readonly MemoryChunk chunk;

        internal HandlerType(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            type = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(Type, Offset, fieldOffset: TypeOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.HandlerType, this, ViewKind.HandlerType, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("adjectives", AdjectivesOffset, Adjectives);
                    break;

                case 1:
                    structWriter.WriteRVAField("dispType", TypeOffset, Type);
                    break;

                case 2:
                    structWriter.WriteField("dispCatchObj", CatchObjOffset, CatchObj);
                    break;

                case 3:
                    structWriter.WriteField("dispOfHandler", HandlerOffset, Handler);
                    break;

                case 4:
                    structWriter.WriteField("dispFrame", FrameOffset, Frame);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
