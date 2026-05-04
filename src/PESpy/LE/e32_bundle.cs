using System;

namespace PESpy.LE
{
    public struct e32_bundle
    {
        /// <summary>
        /// Number of entries in this bundle
        /// </summary>
        public byte b32_cnt => chunk.PeekByte(0);

        /// <summary>
        /// Bundle type
        /// </summary>
        public E32BundleType b32_type => (E32BundleType) chunk.PeekByte(1);

        /// <summary>
        /// Object number
        /// </summary>
        public short b32_obj => chunk.PeekInt16(2);

        private e32_entry[]? entries;

        public e32_entry[] Entries
        {
            get
            {
                if (entries == null)
                {
                    var entriesChunk = chunk.Slice(4);

                    var results = new e32_entry[b32_cnt];

                    //All entries within a given bundle are the same size
                    switch (b32_type)
                    {
                        case E32BundleType.EMPTY:
                            throw new NotImplementedException();

                        case E32BundleType.ENTRY16:
                            for (var i = 0; i < results.Length; i++)
                                results[i] = e32_entry.Entry16(entriesChunk.Slice(i * e32_entry.Entry16Size));

                            break;

                        case E32BundleType.GATE16:
                            for (var i = 0; i < results.Length; i++)
                                results[i] = e32_entry.Gate16(entriesChunk.Slice(i * e32_entry.Gate16Size));

                            break;

                        case E32BundleType.ENTRY32:
                            for (var i = 0; i < results.Length; i++)
                                results[i] = e32_entry.Entry32(entriesChunk.Slice(i * e32_entry.Entry32Size));

                            break;

                        case E32BundleType.ENTRYFWD:
                            for (var i = 0; i < results.Length; i++)
                                results[i] = e32_entry.EntryFwd(entriesChunk.Slice(i * e32_entry.EntryFwdSize));

                            break;

                        case E32BundleType.TYPEINFO:
                            /*     Parameter Typing Information Present.
                             * 
                             *     This bit  signifies that  additional information is contained in the linear 
                             *     EXE module  and will  be used  in  the future for parameter type checking.
                             * 
                             * I don't know what this means
                             */
                            throw new NotImplementedException();

                        default:
                            throw new NotImplementedException();
                    }

                    entries = results;
                }

                return entries;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize
        {
            get
            {
                var size =
                    sizeof(byte) + //b32_cnt
                    sizeof(byte) + //b32_type
                    sizeof(short); //b32_obj

                var entrySize = b32_type switch
                {
                    E32BundleType.ENTRY16 => e32_entry.Entry16Size,
                    E32BundleType.GATE16 => e32_entry.Gate16Size,
                    E32BundleType.ENTRY32 => e32_entry.Entry32Size,
                    E32BundleType.ENTRYFWD => e32_entry.EntryFwdSize
                };

                size += (b32_cnt * entrySize);

                return size;
            }
        }

        private readonly MemoryChunk chunk;

        internal e32_bundle(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            _ = Entries;
        }
    }
}
