using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    //The way the CLR handles this is that in CMiniMdSchema::LoadFrom it loads the metadata header
    //into itself, and then does bit shifting on TBL_* enum values to see which items are present.
    //This is no good; I want an enum that clearly shows which fields are present

    public readonly struct CompressedModelHeader : IValue, IViewable
    {
        private const int Reserved1Offset = 0;
        private const int MajorVersionOffset = 4;
        private const int MinorVersionOffset = 5;
        private const int HeapSizesOffset = 6;
        private const int Reserved2Offset = 7;
        private const int ValidOffset = 8;
        private const int SortedOffset = 16;

        public int Reserved1 => chunk.PeekInt32(Reserved1Offset);

        public byte MajorVersion => chunk.PeekByte(MajorVersionOffset);

        public byte MinorVersion => chunk.PeekByte(MinorVersionOffset);

        public HeapSizes HeapSizes => (HeapSizes) chunk.PeekByte(HeapSizesOffset);

        public byte Reserved2 => chunk.PeekByte(Reserved2Offset);

        public TableMask Valid => (TableMask) chunk.PeekUInt64(ValidOffset);

        public TableMask Sorted => (TableMask) chunk.PeekUInt64(SortedOffset);

        /// <summary>
        /// Gets the row counts as listed in the header. Note that this will only contain as many valid entries as there are <see cref="Valid"/>
        /// bits. In order to get the number of rows in each table, an array of 64 integers must be constructed, and the bits contained in <see cref="Valid"/>
        /// iterated over to assign each row count to the table that owns it.
        /// </summary>
        public int[] RowCounts { get; init; }

        public int Offset => chunk.AbsoluteOffset;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MetadataHeader, this, ViewKind.MetadataHeader, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ViewWriter viewWriter)
        {
            switch (index)
            {
                case 0:
                    viewWriter.WriteField(nameof(Reserved1), Reserved1);
                    break;

                case 1:
                    viewWriter.WriteField(nameof(MajorVersion), MajorVersion);
                    break;

                case 2:
                    viewWriter.WriteField(nameof(MinorVersion), MinorVersion);
                    break;

                case 3:
                    viewWriter.WriteField(nameof(HeapSizes), HeapSizes, sizeof(byte));
                    break;

                case 4:
                    viewWriter.WriteField(nameof(Reserved2), Reserved2);
                    break;

                case 5:
                    viewWriter.WriteField(nameof(Valid), Valid, sizeof(long));
                    break;

                case 6:
                    viewWriter.WriteField(nameof(Sorted), Sorted, sizeof(long));
                    break;

                case 7:
                    viewWriter.WriteField(nameof(RowCounts), RowCounts);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
