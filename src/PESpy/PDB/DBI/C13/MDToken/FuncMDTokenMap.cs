using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    //Type is made up
    public readonly struct FuncMDTokenMap : IViewableValue
    {
        public int NumEntries { get; }

        public Entry[] Entries { get; }

        //Each entry contains a NativeSpan of the specific area of the method data
        //that pertains to it
        public NativeSpan<byte> MethodData { get; }

        public int Offset { get; }

        internal int StructSize => sizeof(int) + (Entries.Length * Entry.StructSize) + MethodData.Length;

        internal FuncMDTokenMap(int offset, int numEntries, Entry[] entries, NativeSpan<byte> methodData)
        {
            Offset = offset;
            NumEntries = numEntries;
            Entries = entries;
            MethodData = methodData;
        }

        [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, Token = {Token}")]
        public readonly struct Entry : IViewableValue
        {
            private const int RVAOffset = 0;
            private const int OffsetOffset = 4;

            public int RVA { get; }

            public uint Offset { get; }

            public mdMemberRef Token { get; }

            public int NumGenericParameters { get; }

            public bool HasMethodData => (Offset >> 31) != 0;

            //e.g. Given the bytes 11-C0-00-53-05-11-C0-00-53-05, you can extract out the types by calling
            //System.Reflection.Metadata.Ecma335.SignatureDecoder.DecodeType NumGenericParameters times.
            //In this instance, you get 2x 0x10014C1 [mdtTypeRef] entries
            public NativeSpan<byte> TypeSpecBlobs { get; }

            private readonly int structOffset;
            int IValue.Offset => structOffset;

            internal const int StructSize =
                sizeof(int) +
                sizeof(int);

            //The "offset" here is really just the RID with the high bit set
            internal Entry(int structOffset, int rva, uint offset, mdMemberRef token)
            {
                this.structOffset = structOffset;
                RVA = rva;
                Offset = offset;
                Token = token;
            }

            internal Entry(int structOffset, int rva, uint offset, mdMemberRef token, int numGenericParameters, NativeSpan<byte> typeSpecBlobs)
            {
                this.structOffset = structOffset;
                RVA = rva;
                Offset = offset;
                Token = token;
                NumGenericParameters = numGenericParameters;
                TypeSpecBlobs = typeSpecBlobs;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.Entry, this, ViewKind.FuncMDTokenMap_Entry, StructSize);

            int IViewable.NumChildren() => HasMethodData ? 2 : 3;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField(nameof(RVA), RVAOffset, RVA);
                        break;

                    case 1:
                        if (HasMethodData)
                        {
                            //Offset is just an offset
                            structWriter.WriteField(nameof(Offset), OffsetOffset, Offset);
                        }
                        else
                        {
                            //Offset is a bitmask
                            structWriter.WriteBitField("SigInOffset", OffsetOffset, true, sizeof(int), 1);
                        }
                        break;

                    case 2:
                        if (HasMethodData)
                            throw new IndexOutOfRangeException();
                        else
                            structWriter.WriteBitField("RID", OffsetOffset, Token.Rid, sizeof(int), 31);

                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FuncMDTokenMap, this, ViewKind.FuncMDTokenMap, StructSize);

        int IViewable.NumChildren() => 2 + Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index == 0)
            {
                structWriter.WriteField(nameof(NumEntries), 0, NumEntries);
                return;
            }

            var i = index - 1;

            if (i < Entries.Length)
            {
                structWriter.WriteInline(Entries[i]);
            }
            else if (i == Entries.Length)
            {
                structWriter.WriteInline(4 + (Entries.Length * Entry.StructSize), MethodData);
            }
            else
                throw new IndexOutOfRangeException();
        }
    }
}
