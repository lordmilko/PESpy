using System;
using System.Collections.Generic;
using PESpy.PDB;
using PESpy.View;

namespace PESpy.OBJ
{
    //Name is made up
    public class OBJSymbolsTable : IValue, IViewable
    {
        public CV_SIGNATURE Signature => (CV_SIGNATURE) chunk.PeekUInt32(0);

        public SymTypeList? c7Symbols;

        //C7 or C11
        public unsafe SymTypeList? C7Symbols
        {
            get
            {
                var sig = Signature;

                if (c7Symbols == null && sig is CV_SIGNATURE.C7 or CV_SIGNATURE.C11)
                {
                    //C7 and C11 use ST strings
                    SymbolMemoryTracker.RegisterCVSymbolMemory(sig, chunk);
                    c7Symbols = new SymTypeList(chunk.Pointer + 4, Length - 4);
                }

                return c7Symbols;
            }
        }

        private CvDebugSSubsectionHeader[]? c13SubSections;

        public CvDebugSSubsectionHeader[]? C13SubSections
        {
            get
            {
                if (c13SubSections == null && Signature == CV_SIGNATURE.C13)
                {
                    var totalOffset = 4;

                    var results = new List<CvDebugSSubsectionHeader>();

                    while (totalOffset < Length)
                    {
                        //The start of each record must be 32-bit aligned relative to the start
                        //of the section. i.e. if the section starts at 1, address 5 is aligned
                        if ((totalOffset & 3) != 0)
                        {
                            var diff = 4 - (totalOffset & 3);
                            totalOffset += diff;
                        }

                        //C13 uses UTF8 strings
                        var header = new CvDebugSSubsectionHeader(chunk.Slice(totalOffset));

                        results.Add(header);

                        totalOffset += header.Length + 8; //sizeof(type) = sizeof(cbLen)
                    }

                    c13SubSections = results.ToArray();
                }

                return c13SubSections;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        /// <summary>
        /// Gets the number of bytes contained in the table.
        /// </summary>
        public int Length { get; }

        internal OBJSymbolsTable(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            Length = length;

            _ = C7Symbols;
#if STRESS_TEST
            _ = C13SubSections;
#endif
        }

        public unsafe void CopyTo(Span<byte> destination)
        {
            new Span<byte>(chunk.Pointer, Length).CopyTo(destination);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);

            var c7 = C7Symbols;

            if (c7 != null)
                writer.WriteGlobal(Offset + 4, c7);
            else
                writer.WriteGlobal(C13SubSections);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
    }
}
