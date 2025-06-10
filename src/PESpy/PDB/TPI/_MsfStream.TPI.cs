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

            private MsfStream.TpiHash? tpiHash;

            public MsfStream.TpiHash? TpiHash
            {
                get
                {
                    if (tpiHash == null && Hdr is HDR h)
                    {
                        var info = h.tpihash;

                        var pdbFile = chunk.PDBFile();

                        if (pdbFile.TryGetStreamChunk(info.sn, out var hashChunk))
                        {
                            tpiHash = new MsfStream.TpiHash(hashChunk, info, h.vers);
                        }
                    }

                    return tpiHash;
                }
            }

            public int Offset => chunk.AbsoluteOffset;

            private readonly MemoryChunk chunk;

            internal unsafe TPI(in MemoryChunk chunk)
            {
                this.chunk = chunk;

                var impv = (TPIImpv) chunk.PeekUInt32(0);

                var headerSize = 0;

                //We are 16-bit (generally) if we are <= impvv41. This also affects
                //the TPI TI to Off map
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
