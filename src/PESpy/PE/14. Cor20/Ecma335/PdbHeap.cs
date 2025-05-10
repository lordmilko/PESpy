using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public class PdbHeap
    {
        public NativeSpan<byte> Id => chunk.PeekNativeSpan<byte>(0, 20);

        public mdMethodDef EntryPoint { get; } //For some reason the Visual Studio debugger is getting upset trying to evaluate this from our chunk, so we eagerly evaluate

        public ulong ReferencedTypeSystemTables => chunk.PeekUInt64(24);

        public int[] TypeSystemTableRows { get; }

        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        public int Size { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            20 + //Id
            sizeof(uint) + //EntryPoint
            sizeof(ulong); //ReferencedTypeSystemTables

        private readonly MemoryChunk chunk;

        internal PdbHeap(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            Size = size;
            EntryPoint = chunk.PeekUInt32(20); //For some reason the Visual Studio debugger is getting upset trying to evaluate this from our chunk, so we eagerly evaluate

            var referencedTypeSystemTables = ReferencedTypeSystemTables;

            var rows = new int[64];

            ulong bit = 1;

            var read = FixedStructSize;

            for (var i = 0; i < rows.Length; i++)
            {
                if ((referencedTypeSystemTables & bit) != 0)
                {
                    var value = chunk.PeekInt32(read);
                    read += sizeof(int);

                    rows[i] = value;
                }

                bit <<= 1;
            }

            TypeSystemTableRows = rows;
        }
    }
}
