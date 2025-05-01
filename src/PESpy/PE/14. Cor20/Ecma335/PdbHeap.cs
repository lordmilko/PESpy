using ClrDebug;

namespace PESpy
{
    public class PdbHeap
    {
        public byte[] Id { get; }

        public mdToken EntryPoint { get; }

        public ulong ReferencedTypeSystemTables { get; }

        public int[] TypeSystemTableRows { get; }

        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset { get; }

        internal PdbHeap(IFileReader reader, int size)
        {
            Offset = (int) reader.Position;
            Size = size;

            Id = reader.ReadBytes(20);
            EntryPoint = reader.ReadInt32();
            ReferencedTypeSystemTables = reader.ReadUInt64();

            var rows = new int[64];

            ulong bit = 1;

            for (var i = 0; i < rows.Length; i++)
            {
                if ((ReferencedTypeSystemTables & bit) != 0)
                    rows[i] = reader.ReadInt32();

                bit <<= 1;
            }

            TypeSystemTableRows = rows;
        }
    }
}
