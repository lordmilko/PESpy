using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //Name is made up
    public readonly struct DNRBModule : IValue, IViewable
    {
        public NativeSpan<byte> Unknown => chunk.PeekNativeSpan<byte>(0, 30);

        public FixedAnsiString Name
        {
            get
            {
                //After the 30 bytes at the front is the name
                var length = chunk.PeekByte(FixedStructSize);
                return chunk.PeekAnsiFixedLength(FixedStructSize + 1, length);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        public int StructSize => FixedStructSize + Name.Length + 1;

        internal const int FixedStructSize = 30; //Unknown

        private readonly MemoryChunk chunk;

        internal DNRBModule(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.DNRBModule, this, ViewKind.DNRBModule, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("Unknown", Unknown);
            s.WriteLengthPrefixedAnsiField("Name", Name);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
