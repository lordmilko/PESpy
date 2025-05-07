using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class GSI : IValue, IViewable
        {
            private readonly GSIHashHdr gsiHdr;

            public ref readonly GSIHashHdr GsiHdr => ref gsiHdr;

            public HRFile[] HashRecords { get; }

            public SymType[] Symbols { get; }

            public int Offset => chunk.AbsoluteOffset;

            internal readonly MemoryChunk chunk;
            
            internal unsafe GSI(in MemoryChunk chunk)
            {
                this.chunk = chunk;
                
                gsiHdr = new GSIHashHdr(chunk);

                if (GsiHdr.verSignature == GSIHashHdr.hdrSignature && GsiHdr.verHdr == GSIHashSCImpv.GSIHashSCImpvV70)
                {
                    var read = GSIHashHdr.StructSize;

                    //microsoft-pdb does does some dodgy stuff loading the HRFile records into an array of HR records,
                    //and then I think it fixes things up, since the layout of HR is not the same as HRFile

                    var numItems = GsiHdr.cbHr / HRFile.StructSize;

                    var hashRecords = new HRFile[numItems];
                    var symbols = new SymType[numItems];

                    var pdbFile = chunk.PDBFile();

                    if (!pdbFile.TryGetStreamChunk(pdbFile.DBI!.DbiHdr.snSymRecs, out var symbolChunk))
                        throw new InvalidOperationException("Couldn't retrieve section for DbiHdr.snSymRecs for global symbols");

                    var symbolsStart = symbolChunk.Pointer;

                    for (var i = 0; i < numItems; i++)
                    {
                        var hrFile = new HRFile(chunk.Slice(read));
                        hashRecords[i] = hrFile;
                        read += HRFile.StructSize;

                        //The target of the symbol can be retrieved by adding the HRFile.off - 1 to the snSymRecs stream
                        symbols[i] = (SYMTYPE*)(symbolsStart + hrFile.off - 1);
                    }

                    HashRecords = hashRecords;
                    Symbols = symbols;

                    if (gsiHdr.cbBuckets > 0)
                    {
                        //Reading the buckets is not yet implemented
                    }
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
