using PESpy.PDB;
using PESpy.View;

namespace PESpy.OBJ
{
    //Name is made up
    class OBJTypesTable : IValue, IViewable
    {
        public CV_SIGNATURE Signature => (CV_SIGNATURE) chunk.PeekUInt32(0);

        private TypTypeList? types;

        public unsafe TypTypeList Types
        {
            get
            {
                if (types == null)
                {
                    SymbolMemoryTracker.RegisterCVSymbolMemory(Signature, chunk);
                    types = new TypTypeList(chunk.Pointer + 4, length - 4);
                }

                return types;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private int length;

        internal OBJTypesTable(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;

#if STRESS_TEST
            _ = Types;
#endif
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.Value);
            writer.WriteGlobal(Offset + 4, Types);
        }
    }
}
