using PESpy.View;

namespace PESpy
{
    public struct HandlerType : IValue, IViewable
    {
        private const int TypeOffset = 4;

        public int Adjectives => chunk.PeekInt32(0);

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

        public int CatchObj => chunk.PeekInt32(8);

        public int Handler => chunk.PeekInt32(12);

        public int Frame => chunk.PeekInt32(16);

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
            writer.WriteRVAField(Type, fieldOffset: TypeOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.HandlerType, this, ViewKind.HandlerType, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("adjectives", Adjectives);
            s.WriteRVAField("dispType", Type);
            s.WriteField("dispCatchObj", CatchObj);
            s.WriteField("dispOfHandler", Handler);
            s.WriteField("dispFrame", Frame);

            return s.ToArray();
        }
    }
}
