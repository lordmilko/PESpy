using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PESpy.View;

namespace PESpy.PDB
{
    //Name table. Different from NMTNI which is name table of name indices
    public class NMT : IValue, IViewable //Class because we may not have EC Info
    {
        public VHdr vhdr => new VHdr(chunk);

        public int NameBufferSize => chunk.PeekInt32(VHdr.StructSize);

        //Names are physically located after NameBufferSize and before NumOffsets

        public int NumOffsets => chunk.PeekInt32(VHdr.StructSize + sizeof(int) + NameBufferSize);

        public NativeSpan<int> Offsets => chunk.PeekNativeSpan<int>(VHdr.StructSize + sizeof(int) + NameBufferSize + sizeof(int), NumOffsets);

        //Not sure exactly what NumStrings is; an empty DBI has 1 offset, a null string
        //I feel like maybe it's "cni"
        public int NumStrings => chunk.PeekInt32(VHdr.StructSize + sizeof(int) + NameBufferSize + sizeof(int) + (NumOffsets * sizeof(int)));

        private RawValue<AnsiString>[]? strings;

        public RawValue<AnsiString>[] Strings
        {
            get
            {
                if (strings == null)
                {
                    var offsets = Offsets;

                    var strs = new RawValue<AnsiString>[offsets.Length];

                    for (var i = 0; i < offsets.Length; i++)
                    {
                        var off = VHdr.StructSize + sizeof(int) + offsets[i];
                        strs[i] = new RawValue<AnsiString>(off + chunk.AbsoluteOffset, chunk.PeekAnsiNullTerminatedString(off));
                    }

                    strings = strs;
                }

                return strings;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            VHdr.StructSize + //vhdr
            sizeof(int) + //NameBufferSize
            sizeof(int) + //NumOffsets
            sizeof(int); //NumStrings

        internal int StructSize =>
            FixedStructSize +
            NameBufferSize +
            (NumOffsets * sizeof(int));

        private readonly MemoryChunk chunk;

        //It's not the same as the StreamNameTable NMTNI; NMTNI has a Map with a list of present/deleted words. We don't have that here
        internal NMT(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public AnsiString GetStringFromNI(NI ni) => chunk.PeekAnsiNullTerminatedString(VHdr.StructSize + sizeof(int) + ni);

        public unsafe NI Hash(string str)
        {
            //We don't need to worry about the \0, it's not hashed

            using var builder = new Utf8StringBuilder(str);

            fixed (byte* p = builder.AsSpan())
                return Hash(new FixedUtf8String(p, builder.Length));
        }

        public NI Hash(FixedUtf8String str)
        {
            //This isn't 100% the same as NMT

            //From nmt::reload, if the vhdr does not have known "allowed" values, the whole thing apparently needs to be rehashed
            var v = vhdr;

            if (v.ulHdr != VHdr.Hdr.verHdr || v.ulVer > VHdr.Ver.verLongHash) //the check is against verCur which is verLongHash
                throw new NotImplementedException("Converting the NMT hash format is not implemented");

            var n = NumOffsets;

            var i = (int) (hashSz(str, v.ulVer) % n);

            var offsets = Offsets;
            var @base = VHdr.StructSize + sizeof(int);

            while (true)
            {
                var ni = offsets[i];

                if (ni == 0)
                    return 0;

                var result = (FixedUtf8String) chunk.PeekUtf8NullTerminatedString(@base + ni);

                //Check if this string actually matches. I think this relates to collisions?
                if (result == str)
                {
                    return ni;
                }

                i = (i + 1 < n) ? (i + 1) : 0;
            }
        }

        private unsafe uint hashSz(FixedUtf8String str, VHdr.Ver ver)
        {
            switch (ver)
            {
                case VHdr.Ver.verLongHash:
                    return Hasher.lhashPbCb(str.Value, str.Length, uint.MaxValue);

                case VHdr.Ver.verLongHashV2:
                    return HasherV2.lhashPbCb(str.Value, str.Length, uint.MaxValue);

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(VHdr.Ver)} '{ver}'");
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.NameTable, StructSize);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //We need to sort the strings and check we only write each one once, so we need
            //to eager load

            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteInline(vhdr);
            s.WriteField("Name Buffer Size", NameBufferSize);

            //The same string could be pointed to by multiple offset items

            var seenAddresses = new HashSet<int>();

            foreach (var str in Strings.OrderBy(v => v.Offset))
            {
                if (seenAddresses.Add(str.Offset))
                    s.WriteInlineAnsiNullTerminated(str);
            }

            s.WriteField("Num Offsets", NumOffsets);
            s.WriteField("Offsets", Offsets);
            s.WriteField("Num Strings", NumStrings);

            structWriter.EagerFields = s.ToArray();
        }
    }
}
