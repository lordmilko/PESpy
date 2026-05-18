using System;
#if NET
using System.Numerics;
#endif
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#standalone-debugging-metadata
    public class PdbHeap : IViewableValue
    {
        private const int IdOffset = 0;
        private const int EntryPointOffset = 20;
        private const int ReferencedTypeSystemTablesOffset = 24;
        private const int TypeSystemTableRowsOffset = 32;

        public NativeSpan<byte> Id => chunk.PeekNativeSpan<byte>(IdOffset, 20);

        public mdMethodDef EntryPoint { get; } //For some reason the Visual Studio debugger is getting upset trying to evaluate this from our chunk, so we eagerly evaluate

        public ulong ReferencedTypeSystemTables => chunk.PeekUInt64(ReferencedTypeSystemTablesOffset);

        ////This is the _compressed_ row counts!
        public NativeSpan<int> TypeSystemTableRows { get; }

        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        public int Size { get; }

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            20 + //Id
            sizeof(uint) + //EntryPoint
            sizeof(ulong); //ReferencedTypeSystemTables

        private readonly MemoryChunk chunk;

        internal PdbHeap(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            Size = size;
            EntryPoint = chunk.PeekUInt32(EntryPointOffset); //For some reason the Visual Studio debugger is getting upset trying to evaluate this from our chunk, so we eagerly evaluate

            var numRows = 0;

#if NET
            numRows = BitOperations.PopCount(ReferencedTypeSystemTables);
#else
            var value = ReferencedTypeSystemTables;

            while (value != 0)
            {
                numRows++;
                value &= value - 1;
            }
#endif


            TypeSystemTableRows = chunk.PeekNativeSpan<int>(TypeSystemTableRowsOffset, numRows);
        }

        public void GetRowCounts(Span<int> span)
        {
            //See MetadataSizes.cs for information on what these sizes are for

            var rows = TypeSystemTableRows;

            ulong bit = 1;

            var read = FixedStructSize;

            var referencedTypeSystemTables = ReferencedTypeSystemTables;

            for (var i = 0; i < rows.Length; i++)
            {
                if ((referencedTypeSystemTables & bit) != 0)
                {
                    var value = chunk.PeekInt32(read);
                    read += sizeof(int);

                    span[i] = value;
                }

                bit <<= 1;
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.PdbHeap, FixedStructSize + (TypeSystemTableRows.Length * sizeof(int)));

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Id), IdOffset, Id);
                    break;

                case 1:
                    structWriter.WriteField(nameof(EntryPoint), EntryPointOffset, EntryPoint);
                    break;

                case 2:
                    structWriter.WriteField(nameof(ReferencedTypeSystemTables), ReferencedTypeSystemTablesOffset, ReferencedTypeSystemTables);
                    break;

                case 3:
                    structWriter.WriteField(nameof(TypeSystemTableRows), TypeSystemTableRowsOffset, TypeSystemTableRows);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
