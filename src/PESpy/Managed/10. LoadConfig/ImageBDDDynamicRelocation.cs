using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBDDDynamicRelocation : IValue, IViewable
    {
        /// <summary>
        /// Index of FALSE edge in BDD array
        /// </summary>
        public short Left { get; }

        /// <summary>
        /// Index of TRUE edge in BDD array
        /// </summary>
        public short Right { get; }

        /// <summary>
        /// Either FeatureNumber or Index into RVAs array
        /// </summary>
        public int Value { get; }

        public const int StructSize =
            sizeof(short) + //Left
            sizeof(short) + //Right
            sizeof(int);    //Value

        public int Offset { get; }

        internal ImageBDDDynamicRelocation(IFileReader reader)
        {
            Offset = (int) reader.Position;

            Left = reader.ReadInt16();
            Right = reader.ReadInt16();
            Value = reader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_BDD_DYNAMIC_RELOCATION), this, ViewKind.ImageBDDDynamicRelocation);

            s.WriteField(nameof(Left), Left);
            s.WriteField(nameof(Right), Right);
            s.WriteField(nameof(Value), Value);
        }
    }
}
