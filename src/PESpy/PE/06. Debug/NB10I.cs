using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="Native.NB10I"/> structure.
    /// </summary>
    public class NB10I : ICodeViewPDB, IViewable //It's going to be boxed
    {
        private const int SignatureOffset = 0;
        private const int dwOffsetOffset = 4;
        private const int PdbSignatureOffset = 8;
        private const int AgeOffset = 12;
        private const int PathOffset = 16;

        public CodeViewSig Signature => (CodeViewSig) chunk.PeekUInt32(SignatureOffset);

        public int dwOffset => chunk.PeekInt32(dwOffsetOffset);

        public uint PdbSignature => chunk.PeekUInt32(PdbSignatureOffset);

        public int Age => chunk.PeekInt32(AgeOffset);

        public AnsiString Path => chunk.PeekAnsiNullTerminatedString(PathOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //Signature
            sizeof(int) + //Offset
            sizeof(int) + //PdbSignature
            sizeof(int);  //Age

        internal int StructSize =>
            FixedStructSize +
            Path.Length + 1;

        private readonly MemoryChunk chunk;

        internal NB10I(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.NB10I, this, ViewKind.NB10I, StructSize);

        int IViewable.NumChildren => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("dwSig", SignatureOffset, Signature, sizeof(uint));
                    break;

                case 1:
                    structWriter.WriteField("dwOffset", dwOffsetOffset, dwOffset);
                    break;

                case 2:
                    structWriter.WriteField("sig", PdbSignatureOffset, PdbSignature);
                    break;

                case 3:
                    structWriter.WriteField("age", AgeOffset, Age);
                    break;

                case 4:
                    structWriter.WriteAnsiNullTerminatedField("szPdb", PathOffset, Path);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
