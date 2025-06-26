using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBDDDynamicRelocation : IValue, IViewable
    {
        /// <summary>
        /// Index of FALSE edge in BDD array
        /// </summary>
#if PEFAST
        public short Left => chunk.PeekInt16(0);
#else
        public short Left { get; }
#endif

        /// <summary>
        /// Index of TRUE edge in BDD array
        /// </summary>
#if PEFAST
        public short Right => chunk.PeekInt16(2);
#else
        public short Right { get; }
#endif

        /// <summary>
        /// Either FeatureNumber or Index into RVAs array
        /// </summary>
#if PEFAST
        public int Value => chunk.PeekInt32(4);
#else
        public int Value { get; }
#endif

        public const int StructSize =
            sizeof(short) + //Left
            sizeof(short) + //Right
            sizeof(int);    //Value

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageBDDDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageBDDDynamicRelocation(IFileReader reader)
        {
            Offset = (int) reader.Position;

            Left = reader.ReadInt16();
            Right = reader.ReadInt16();
            Value = reader.ReadInt32();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(IMAGE_BDD_DYNAMIC_RELOCATION), this, ViewKind.ImageBDDDynamicRelocation, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Left), Left);
            s.WriteField(nameof(Right), Right);
            s.WriteField(nameof(Value), Value);

            return s.ToArray();
        }
    }
}
