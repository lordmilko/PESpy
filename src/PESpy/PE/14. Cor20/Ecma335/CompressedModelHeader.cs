using System;
using System.Collections.Generic;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    //The way the CLR handles this is that in CMiniMdSchema::LoadFrom it loads the metadata header
    //into itself, and then does bit shifting on TBL_* enum values to see which items are present.
    //This is no good; I want an enum that clearly shows which fields are present

    public readonly struct CompressedModelHeader : IValue, IViewable
    {
#if PEFAST
        public int Reserved1 => chunk.PeekInt32(0);

        public byte MajorVersion => chunk.PeekByte(4);

        public byte MinorVersion => chunk.PeekByte(5);

        public HeapSizes HeapSizes => (HeapSizes) chunk.PeekByte(6);

        public byte Reserved2 => chunk.PeekByte(7);

        public TableMask Valid => (TableMask) chunk.PeekUInt64(8);

        public TableMask Sorted => (TableMask) chunk.PeekUInt64(16);
#else
        public int Reserved1 { get; init; }

        public byte MajorVersion { get; init; }

        public byte MinorVersion { get; init; }

        public HeapSizes HeapSizes { get; init; }

        public byte Reserved2 { get; init; }

        public TableMask Valid { get; init; }

        public TableMask Sorted { get; init; }
#endif

        /// <summary>
        /// Gets the row counts as listed in the header. Note that this will only contain as many valid entries as there are <see cref="Valid"/>
        /// bits. In order to get the number of rows in each table, an array of 64 integers must be constructed, and the bits contained in <see cref="Valid"/>
        /// iterated over to assign each row count to the table that owns it.
        /// </summary>
        public int[] RowCounts { get; init; }

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int FixedStructSize =
            sizeof(int) + //Reserved1
            sizeof(byte) + //MajorVersion
            sizeof(byte) + //MinorVersion
            sizeof(byte) + //HeapSizes
            sizeof(byte) + //Reserved2
            sizeof(long) + //Valid
            sizeof(long);  //Sorted

        internal int StructSize =>
            FixedStructSize + (RowCounts.Length * sizeof(int));

#if PEFAST
        private readonly MemoryChunk chunk;

        internal CompressedModelHeader(in MemoryChunk chunk, out int[] rowCounts)
        {
            this.chunk = chunk;

            //Valid is a bit vector that lists every single table that is valid in the module. As Valid is a 64-bit value, this implicitly means that the maximum
            //number of metadata tables a given PE can possibly have is 64

            rowCounts = new int[64];

            ulong bit = 1;

            using var compressedRowCounts = new PooledList<int>();

            RowCounts = default!;
            var valid = Valid;

            var read = FixedStructSize;

            for (var i = 0; i < rowCounts.Length; i++)
            {
                if (((ulong) valid & bit) != 0)
                {
                    var value = chunk.PeekInt32(read);
                    read += sizeof(int);

                    //There are 64 possible tables. If the table is not set here, by default it's count is 0
                    rowCounts[i] = value;
                    compressedRowCounts.Add(value);
                }

                bit <<= 1;
            }

            RowCounts = compressedRowCounts.ToArray();

#if DEBUG
            if ((HeapSizes & HeapSizes.EXTRA_DATA) != 0)
                throw new NotImplementedException("Don't know how to handle having extra data");
#endif
        }
#else
        internal CompressedModelHeader(IFileReader reader, out int[] rowCounts)
        {
            //ECMA-335 II.24.2.6

            //Reader is filled by parent

            Offset = (RawOffset) reader.Position;

            Reserved1 = reader.ReadInt32();
            MajorVersion = reader.ReadByte();
            MinorVersion = reader.ReadByte();
            HeapSizes = (HeapSizes) reader.ReadByte();
            Reserved2 = reader.ReadByte();
            Valid = (TableMask) reader.ReadUInt64();
            Sorted = (TableMask) reader.ReadUInt64();

            //Valid is a bit vector that lists every single table that is valid in the module. As Valid is a 64-bit value, this implicitly means that the maximum
            //number of metadata tables a given PE can possibly have is 64

            rowCounts = new int[64];

            ulong bit = 1;

            using var compressedRowCounts = new PooledList<int>();

            for (var i = 0; i < rowCounts.Length; i++)
            {
                if (((ulong) Valid & bit) != 0)
                {
                    var value = reader.ReadInt32();

                    //There are 64 possible tables. If the table is not set here, by default it's count is 0
                    rowCounts[i] = value;
                    compressedRowCounts.Add(value);
                }

                bit <<= 1;
            }

            RowCounts = compressedRowCounts.ToArray();

#if DEBUG
            if ((HeapSizes & HeapSizes.EXTRA_DATA) != 0)
                throw new NotImplementedException("Don't know how to handle having extra data");
#endif
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("Metadata Header", this, ViewKind.MetadataHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Reserved1), Reserved1);
            s.WriteField(nameof(MajorVersion), MajorVersion);
            s.WriteField(nameof(MinorVersion), MinorVersion);
            s.WriteField(nameof(HeapSizes), HeapSizes, sizeof(byte));
            s.WriteField(nameof(Reserved2), Reserved2);
            s.WriteField(nameof(Valid), Valid, sizeof(long));
            s.WriteField(nameof(Sorted), Sorted, sizeof(long));
            s.WriteField(nameof(RowCounts), RowCounts);

            return s.ToArray();
        }
    }
}
