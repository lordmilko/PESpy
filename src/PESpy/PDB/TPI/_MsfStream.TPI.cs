using System.Collections.Generic;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class TPI
        {
            private HDR hdr;
            public ref readonly HDR Hdr => ref hdr;

            public TypType[] Types { get; }

            internal unsafe TPI(in MemoryChunk chunk)
            {
                hdr = new HDR(chunk);

                var ptr = chunk.Pointer + HDR.StructSize;

                var end = ptr + hdr.cbGprec;

                var results = new List<TypType>();

                while (ptr < end)
                {
                    TypType typType = (TYPTYPE*) ptr;

#if DEBUG
                    //Force resolve the symbol to its actual type so that we can trigger any asserts for un-implemented properties
                    TypTypeProxy.GetValue(typType);
#endif

                    results.Add(typType);

                    ptr += typType.len + 2;
                }

                Types = results.ToArray();
            }
        }
    }
}
