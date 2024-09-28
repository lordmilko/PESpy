using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBDDInfo : IValue, IViewable
    {
        /// <summary>
        /// Decides the semantics of serialized BDD
        /// </summary>
        public int Version { get; }

        public int BDDSize { get; }

        public ImageBDDDynamicRelocation[] BDDNodes { get; }

        public int Offset { get; }

        internal ImageBDDInfo(ref FileReader reader)
        {
            Offset = (int) reader.Position;

            Version = reader.ReadInt32();
            BDDSize = reader.ReadInt32();

            var nodes = new ImageBDDDynamicRelocation[BDDSize / ImageBDDDynamicRelocation.StructSize];

            for (var i = 0; i < nodes.Length; i++)
                nodes[i] = new ImageBDDDynamicRelocation(ref reader);

            BDDNodes = nodes;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_BDD_INFO), this, ViewKind.ImageBDDInfo);

            s.WriteField(nameof(Version), Version);
            s.WriteField(nameof(BDDSize), BDDSize);
            s.WriteInline(BDDNodes);
        }
    }
}
