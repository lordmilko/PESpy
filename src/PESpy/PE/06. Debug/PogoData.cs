using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //Name is made up
    public enum PogoSignatureKind : uint
    {
        Zero = 0,
        LCTG = 0x4C544347, //LCTG
        PGI = 0x50474900, //PGI\0
        PGO = 0x50474F00, //PGO\0
        PGU = 0x50475500, //PGU\0
        SPGO = 0x5350474f, //SPGO
    }

    /// <summary>
    /// Represents the data contained in the POGO debug directory.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public struct PogoData : IValue, IViewable
    {
        private const int SignatureOffset = 0;

        public PogoSignatureKind Signature => (PogoSignatureKind) chunk.PeekUInt32(SignatureOffset);

        private PogoItem[]? entries;

        public PogoItem[] Entries
        {
            get
            {
                if (entries == null)
                {
                    var end = sizeOfData - 4;

                    var read = 4;

                    using var results = new PooledList<PogoItem>();

                    while (read < end)
                    {
                        var item = new PogoItem(chunk.Slice(read));
                        read += item.StructSize;

                        //Each entry should be aligned to 4 bytes
                        read = (read + 3) & ~3;

                        results.Add(item);
                    }

                    entries = results.ToArray();
                }

                return entries;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int sizeOfData;

        internal PogoData(in MemoryChunk chunk, int sizeOfData)
        {
            this.chunk = chunk;
            this.sizeOfData = sizeOfData;
            entries = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PogoData, this, ViewKind.PogoData, sizeOfData);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField(nameof(Signature), Signature, sizeof(int));

            //Each value must be 4-byte aligned
            var entries = Entries;

            foreach (var item in entries)
            {
                s.WriteInline(item);

                s.Align(4);
            }

            structWriter.EagerFields = s.ToArray();
        }
    }
}
