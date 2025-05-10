using System.Collections.Generic;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class TPI : IValue, IViewable
        {
            public IHDR Hdr { get; } //We have to use interfaces because there's just way too much variability as to when different structures may appear

            public TypTypeList Types { get; }

            public int Offset => chunk.AbsoluteOffset;

            private readonly MemoryChunk chunk;

            internal unsafe TPI(in MemoryChunk chunk)
            {
                this.chunk = chunk;

                var impv = (TPIImpv) chunk.PeekUInt32(0);

                var headerSize = 0;

                if (impv <= TPIImpv.impv41)
                {
                    Hdr = new HDR_16t(chunk);
                    headerSize += HDR_16t.StructSize;
                }
                else
                {
                    Hdr = new HDR(chunk);
                    headerSize += HDR.StructSize;
                }

                var ptr = chunk.Pointer + headerSize;

                SymbolMemoryTracker.RegisterPDBSymbolMemory(chunk);

                Types = new TypTypeList(ptr, Hdr.cbGprec);
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                writer.WriteGlobal(Hdr);

                writer.WritePagedGlobal(chunk.RelativeOffset + Hdr.StructSize, (PagedMemoryBlock) chunk.block, Types);
            }
        }
    }
}
