using System;
using System.Buffers;
using System.Collections.Generic;
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(PESpy.Strings.NameTable, this, ViewKind.NameTable, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

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

            return s.ToArray();
        }
    }
}
