using PESpy.View;

namespace PESpy
{
    public readonly struct HandlerType : IValue, IViewable
    {
        public int Adjectives { get; }

        public RVA<TypeDescriptor> Type { get; }

        public int CatchObj { get; }

        public int Handler { get; }

        public int Frame { get; }

        public int Offset { get; }

        internal HandlerType(ref FileReader reader, PEFile peFile)
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

                Type = new RVA<TypeDescriptor>(dispType, offset, new TypeDescriptor(ref reader, peFile));

                reader.Seek(oldPosition);
            }
            else
                Type = new RVA<TypeDescriptor>(dispType);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PESpy.Native.HandlerType), this, ViewKind.HandlerType);

            s.WriteField("adjectives", Adjectives);
            s.WriteRVAField("dispType", Type);
            s.WriteField("dispCatchObj", CatchObj);
            s.WriteField("dispOfHandler", Handler);
            s.WriteField("dispFrame", Frame);
        }
    }
}
