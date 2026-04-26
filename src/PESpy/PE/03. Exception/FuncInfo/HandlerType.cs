using System;
using PESpy.View;

namespace PESpy
{
    public struct HandlerType : IValue, IViewable
    {
        private const int adjectivesOffset = 0;
        internal const int dispTypeOffset = 4;
        private const int dispCatchObjOffset = 8;
        private const int dispOfHandlerOffset = 12;
        private const int dispFrameOffset = 16;

        public HT adjectives => (HT) chunk.PeekUInt32(adjectivesOffset);

        private RVA<TypeDescriptor> _dispType;

        public RVA<TypeDescriptor> dispType
        {
            get
            {
                if (_dispType.ListedOffset == 0)
                {
                    var dispType = chunk.PeekInt32(dispTypeOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispType, out var valueChunk))
                        _dispType = new RVA<TypeDescriptor>(dispType, dispType, new TypeDescriptor(valueChunk));
                    else
                        _dispType = new RVA<TypeDescriptor>(dispType);
                }

                return _dispType;
            }
        }

        public int dispCatchObj => chunk.PeekInt32(dispCatchObjOffset);

        public int dispOfHandler => chunk.PeekInt32(dispOfHandlerOffset);

        public int dispFrame => chunk.PeekInt32(dispFrameOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //adjectives
            sizeof(int) + //dispType
            sizeof(int) + //dispCatchObj
            sizeof(int) + //dispOfHandler
            sizeof(int); //dispFrame

        private readonly MemoryChunk chunk;

        internal HandlerType(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            _dispType = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteUniqueRVAField(dispType, Offset, fieldOffset: dispTypeOffset);
            writer.WriteUniqueRVAXRef(Offset, dispOfHandlerOffset, dispOfHandler);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.HandlerType, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(adjectives), adjectivesOffset, adjectives, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteRVAField(nameof(dispType), dispTypeOffset, dispType);
                    break;

                case 2:
                    structWriter.WriteField(nameof(dispCatchObj), dispCatchObjOffset, dispCatchObj);
                    break;

                case 3:
                    structWriter.WriteField(nameof(dispOfHandler), dispOfHandlerOffset, dispOfHandler);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dispFrame), dispFrameOffset, dispFrame);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
