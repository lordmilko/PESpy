using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class PSGSI : GSI, IValue, IViewable
        {
            public PSGSIHDR PSGsiHdr { get; }
                                    
            internal PSGSI(in MemoryChunk chunk) : base(chunk.Slice(PSGSIHDR.StructSize))
            {                
                PSGsiHdr = new PSGSIHDR(chunk);
                
                //Following the PSGSIHDR we have the exact same hash info that is found in GSI. This is read
                //by the base GSI ctor

                //Following this is Addr Mao, and then Thunk Map
            }

            protected override void WriteView(ViewWriter writer)
            {
                writer.WriteGlobal(PSGsiHdr);

                base.WriteView(writer);
            }
        }
    }
}
