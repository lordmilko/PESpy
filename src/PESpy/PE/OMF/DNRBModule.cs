using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //Name is made up
    public readonly struct DNRBModule : IValue, IViewable
    {
        private const int UnknownOffset = 0;
        private const int NameOffset = FixedStructSize;

        public NativeSpan<byte> Unknown => chunk.PeekNativeSpan<byte>(UnknownOffset, 30);

        public FixedAnsiString Name
        {
            get
            {
                //After the 30 bytes at the front is the name
                var length = chunk.PeekByte(NameOffset);
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

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("Unknown", UnknownOffset, Unknown);
                    break;

                case 1:
                    structWriter.WriteLengthPrefixedAnsiField("Name", NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
