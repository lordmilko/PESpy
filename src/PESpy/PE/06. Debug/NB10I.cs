using System;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="Native.NB10I"/> structure.
    /// </summary>
    public class NB10I : ICodeViewPDB, IViewable //It's going to be boxed
    {
#if PEFAST
        public CodeViewSig Signature => (CodeViewSig) chunk.PeekUInt32(0);
#else
        public CodeViewSig Signature { get; }
#endif

#if PEFAST
        public int dwOffset => chunk.PeekInt32(4);
#else
        public int dwOffset { get; }
#endif

#if PEFAST
        public int PdbSignature => chunk.PeekInt32(8);
#else
        public int PdbSignature { get; }
#endif

#if PEFAST
        public int Age => chunk.PeekInt32(12);
#else
        public int Age { get; }
#endif

#if PEFAST
        public AnsiString Path => chunk.PeekAnsiNullTerminatedString(16);
#else
        public string Path { get; } //It's szPdb[MAX_PATH] but I don't think it's actually going to occupy 260 bytes if not needed
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int FixedStructSize =
            sizeof(int) + //Signature
            sizeof(int) + //Offset
            sizeof(int) + //PdbSignature
            sizeof(int);  //Age

#if PEFAST
        private readonly MemoryChunk chunk;

        internal NB10I(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal NB10I(IFileReader reader, CodeViewSig signature)
        {
            //Signature has already been read
            Offset = (RawOffset) reader.Position - 4;

            Signature = signature;

            if (Signature != CodeViewSig.NB10)
                throw new BadImageFormatException("Unexpected CodeView data signature value.");

            reader.FillBuffer(FixedStructSize - sizeof(int));

            dwOffset = reader.ReadInt32();
            PdbSignature = reader.ReadInt32();
            Age = reader.ReadInt32();
            Path = reader.ReadAnsiNullTerminatedString();
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(NB10I), this, ViewKind.NB10I);

            s.WriteField("dwSig", Signature, sizeof(uint));
            s.WriteField("dwOffset", dwOffset);
            s.WriteField("sig", PdbSignature);
            s.WriteField("age", Age);
            s.WriteAnsiNullTerminatedField("szPdb", Path);
        }

        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
