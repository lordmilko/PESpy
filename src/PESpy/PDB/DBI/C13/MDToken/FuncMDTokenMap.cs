using System.Diagnostics;
using ClrDebug;

namespace PESpy.PDB
{
    //Type is made up
    public readonly struct FuncMDTokenMap
    {
        public int NumEntries { get; }

        public Entry[] Entries { get; }

        //Each entry contains a NativeSpan of the specific area of the method data
        //that pertains to it
        public NativeSpan<byte> MethodData { get; }

        internal FuncMDTokenMap(int numEntries, Entry[] entries, NativeSpan<byte> methodData)
        {
            NumEntries = numEntries;
            Entries = entries;
            MethodData = methodData;
        }

        [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, Token = {Token}")]
        public readonly struct Entry
        {
            public int RVA { get; }

            public uint Offset { get; }

            public mdMemberRef Token { get; }

            public int NumGenericParameters { get; }

            public bool HasMethodData => (Offset >> 31) != 0;

            //e.g. Given the bytes 11-C0-00-53-05-11-C0-00-53-05, you can extract out the types by calling
            //System.Reflection.Metadata.Ecma335.SignatureDecoder.DecodeType NumGenericParameters times.
            //In this instance, you get 2x 0x10014C1 [mdtTypeRef] entries
            public NativeSpan<byte> TypeSpecBlobs { get; }

            //The "offset" here is really just the RID with the high bit set
            internal Entry(int rva, uint offset, mdMemberRef token)
            {
                RVA = rva;
                Offset = offset;
                Token = token;
            }

            internal Entry(int rva, uint offset, mdMemberRef token, int numGenericParameters, NativeSpan<byte> typeSpecBlobs)
            {
                RVA = rva;
                Offset = offset;
                Token = token;
                NumGenericParameters = numGenericParameters;
                TypeSpecBlobs = typeSpecBlobs;
            }
        }
    }
}
