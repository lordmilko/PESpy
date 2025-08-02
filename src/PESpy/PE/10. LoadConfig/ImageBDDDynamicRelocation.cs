using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBDDDynamicRelocation : IValue, IViewable
    {
        /// <summary>
        /// Index of FALSE edge in BDD array
        /// </summary>
        public short Left => chunk.PeekInt16(0);

        /// <summary>
        /// Index of TRUE edge in BDD array
        /// </summary>
        public short Right => chunk.PeekInt16(2);

        /// <summary>
        /// Either FeatureNumber or Index into RVAs array
        /// </summary>
        public int Value => chunk.PeekInt32(4);

        public const int StructSize =
            sizeof(short) + //Left
            sizeof(short) + //Right
            sizeof(int);    //Value

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageBDDDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BDD_DYNAMIC_RELOCATION, this, ViewKind.ImageBDDDynamicRelocation, StructSize);

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
