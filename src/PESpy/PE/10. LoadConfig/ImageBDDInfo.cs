using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBDDInfo : IValue, IViewable
    {
        private const int VersionOffset = 0;
        private const int BDDSizeOffset = 4;
        private const int BDDNodesOffset = 8;

        /// <summary>
        /// Decides the semantics of serialized BDD
        /// </summary>
        public int Version { get; }

        public int BDDSize { get; }

        public ImageBDDDynamicRelocation[] BDDNodes { get; }

        public int Offset { get; }

        internal int StructSize =>
            sizeof(int) + //Version
            sizeof(int) + //BDDSize
            BDDSize; //BDDNodes

        internal ImageBDDInfo(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            //Eagerly load; I presume all the info you want is in the BDD Nodes
            Version = chunk.PeekInt32(VersionOffset);
            BDDSize = chunk.PeekInt32(BDDSizeOffset);

            var nodes = new ImageBDDDynamicRelocation[BDDSize / ImageBDDDynamicRelocation.StructSize];

            for (var i = 0; i < nodes.Length; i++)
                nodes[i] = new ImageBDDDynamicRelocation(chunk.Slice(BDDNodesOffset + (i * ImageBDDDynamicRelocation.StructSize)));

            BDDNodes = nodes;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageBDDInfo, StructSize);

        int IViewable.NumChildren() => 2 + BDDNodes.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Version), VersionOffset, Version);
                    break;

                case 1:
                    structWriter.WriteField(nameof(BDDSize), BDDSizeOffset, BDDSize);
                    break;

                default:
                    structWriter.WriteInline(BDDNodes[index - 2]);
                    break;
            }
        }
    }
}
