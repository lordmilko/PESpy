using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class GSI : IValue, IViewable
        {
            public GSIHashHdr? GsiHdr { get; }

            public NativeSpan<HRFile> HashRecords { get; }

            public int[] Buckets { get; }

            public GlobalSymTypeList Symbols { get; }

            public int Offset => chunk.AbsoluteOffset;

            private int iphrHash;

            internal readonly MemoryChunk chunk;
            
            internal unsafe GSI(in MemoryChunk chunk, int length)
            {
                this.chunk = chunk;

                if (GsiHdr.verSignature == GSIHashHdr.hdrSignature && GsiHdr.verHdr == GSIHashSCImpv.GSIHashSCImpvV70)
                {
                    //microsoft-pdb does does some dodgy stuff loading the HRFile records into an array of HR records,
                    //and then I think it fixes things up, since the layout of HR is not the same as HRFile

                    var numItems = GsiHdr.cbHr / HRFile.StructSize;

                    var hashRecords = new HRFile[numItems];
                    var symbols = new SymType[numItems];

                    if (hashRecords.Length > 0)
                    {
                        var pdbFile = chunk.PDBFile();

                        if (!pdbFile.TryGetStreamChunk(pdbFile.DBI!.DbiHdr.snSymRecs, out var symbolChunk))
                            throw new InvalidOperationException("Couldn't retrieve section for DbiHdr.snSymRecs for global symbols");

                        SymbolMemoryTracker.RegisterPDBSymbolMemory(symbolChunk);

                        var symbolsStart = symbolChunk.Pointer;

                        Symbols = new GlobalSymTypeList(HashRecords, symbolsStart);

                        if (gsiHdr.cbBuckets > 0)
                        {
                            //Reading the buckets is not yet implemented
                        }
                    }
                    else
                        Symbols = null!;
                }
                else
                {
                    //I think there is no header? We need to make it a class then?

                    throw new NotImplementedException("Reading globals without a header is not implemented");
                }
            }

            void IViewable.WriteView(ViewWriter writer) => WriteView(writer);

            protected virtual void WriteView(ViewWriter writer)
            {
                writer.WriteGlobal(GsiHdr);
            }
        }
    }
}
