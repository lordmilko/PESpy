using System.Collections.Generic;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the POGO debug directory.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct PogoData : IValue, IViewable
    {
        //todo: -1 and 0 signatures?
        public const int ZeroSignature = 0;
        public const int LCTGSignature = 0x4C544347; //LCTG
        public const int PGISignature = 0x50474900; //PGI\0
        public const int PGOSignature = 0x50474F00; //PGO\0
        public const int PGUSignature = 0x50475500; //PGU\0
        public const int SPGOSignature = 0x5350474f; //SPGO

        public int Signature { get; }

        public PogoItem[] Entries { get; }

        public RawOffset Offset { get; }

        internal PogoData(IFileReader reader, int signature, int sizeOfData)
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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PogoData), this, ViewKind.PogoData);

            s.WriteField(nameof(Signature), Signature);
            s.WriteInline(Entries);
        }
    }
}
