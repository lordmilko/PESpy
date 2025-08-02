using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="Native.NB10I"/> structure.
    /// </summary>
    public class NB10I : ICodeViewPDB, IViewable //It's going to be boxed
    {
        public CodeViewSig Signature => (CodeViewSig) chunk.PeekUInt32(0);

        public int dwOffset => chunk.PeekInt32(4);

        public uint PdbSignature => chunk.PeekUInt32(8);

        public int Age => chunk.PeekInt32(12);

        public AnsiString Path => chunk.PeekAnsiNullTerminatedString(16);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("dwSig", Signature, sizeof(uint));
            s.WriteField("dwOffset", dwOffset);
            s.WriteField("sig", PdbSignature);
            s.WriteField("age", Age);
            s.WriteAnsiNullTerminatedField("szPdb", Path);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
