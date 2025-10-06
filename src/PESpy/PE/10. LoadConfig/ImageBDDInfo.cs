using System.Diagnostics;
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

        internal int StructSize =>
            sizeof(int) + //Version
            sizeof(int) + //BDDSize
            BDDSize; //BDDNodes

        internal ImageBDDInfo(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            //Eagerly load; I presume all the info you want is in the BDD Nodes
            Version = chunk.PeekInt32(0);
            BDDSize = chunk.PeekInt32(4);

            var nodes = new ImageBDDDynamicRelocation[BDDSize / ImageBDDDynamicRelocation.StructSize];

            for (var i = 0; i < nodes.Length; i++)
                nodes[i] = new ImageBDDDynamicRelocation(chunk.Slice(8 + (i * ImageBDDDynamicRelocation.StructSize)));

            BDDNodes = nodes;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BDD_INFO, this, ViewKind.ImageBDDInfo, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Version), Version);
            s.WriteField(nameof(BDDSize), BDDSize);
            s.WriteInline(BDDNodes);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
