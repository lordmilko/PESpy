using PESpy.View;

namespace PESpy
{
    public struct HandlerType : IValue, IViewable
    {
        private const int TypeOffset = 4;

#if PEFAST
        public int Adjectives => chunk.PeekInt32(0);
#else
        public int Adjectives { get; }
#endif

#if PEFAST
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
#else
        public RVA<TypeDescriptor> Type { get; }
#endif

#if PEFAST
        public int CatchObj => chunk.PeekInt32(8);
#else
        public int CatchObj { get; }
#endif

#if PEFAST
        public int Handler => chunk.PeekInt32(12);
#else
        public int Handler { get; }
#endif

#if PEFAST
        public int Frame => chunk.PeekInt32(16);
#else
        public int Frame { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //Adjectives
            sizeof(int) + //Type
            sizeof(int) + //CatchObj
            sizeof(int) + //Handler
            sizeof(int); //Frame

#if PEFAST
        private readonly MemoryChunk chunk;

        internal HandlerType(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            type = default;
        }
#else
        internal HandlerType(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            Adjectives = reader.ReadInt32();
            var dispType = reader.ReadInt32();
            CatchObj = reader.ReadInt32();
            Handler = reader.ReadInt32();
            Frame = reader.ReadInt32();

            if (peFile.TryGetOffset(dispType, out var offset))
            {
                var oldPosition = reader.Position;

                reader.Seek(offset);

                Type = new RVA<TypeDescriptor>(dispType, offset, new TypeDescriptor(reader, peFile));

                reader.Seek(oldPosition);
            }
            else
                Type = new RVA<TypeDescriptor>(dispType);
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(Type, fieldOffset: TypeOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(PESpy.Native.HandlerType), this, ViewKind.HandlerType, StructSize);

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
