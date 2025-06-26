using System.Collections.Generic;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
#if PEFAST
        public PogoSignatureKind Signature => (PogoSignatureKind) chunk.PeekUInt32(0);
#else
        public PogoSignatureKind Signature { get; }
#endif

#if PEFAST
        private PogoItem[]? entries;

        public PogoItem[] Entries
        {
            get
            {
                if (entries == null)
                {
                    var end = sizeOfData - 4;

                    var read = 4;

                    var results = new List<PogoItem>();

                    while (read < end)
                    {
                        var item = new PogoItem(chunk.Slice(read));
                        read += PogoItem.FixedStructSize + item.Name.Length + 1;

                        //Each entry should be aligned to 4 bytes
                        read = (read + 3) & ~3;

                        results.Add(item);
                    }

                    entries = results.ToArray();
                }

                return entries;
            }
        }

#else
        public PogoItem[] Entries { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;
        private readonly int sizeOfData;

        internal PogoData(in MemoryChunk chunk, int sizeOfData)
        {
            this.chunk = chunk;
            this.sizeOfData = sizeOfData;
            entries = default;
        }
#else
        internal PogoData(IFileReader reader, PogoSignatureKind signature, int sizeOfData)
        {
            //Signature has already been read
            Offset = (RawOffset) reader.Position - 4;
            Signature = signature;

            var end = (int) Offset + sizeOfData;

            var entries = new List<PogoItem>();

            while (reader.Position < end)
                entries.Add(new PogoItem(reader));

            Entries = entries.ToArray();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            throw new System.NotImplementedException();
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(PogoData), this, ViewKind.PogoData, sizeOfData);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Signature), Signature, sizeof(int));
            s.WriteInline(Entries);

            return s.ToArray();
        }
    }
}
