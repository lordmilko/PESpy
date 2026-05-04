using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.LIB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy.OBJ
{
    //Name is made up
    [DebuggerDisplay("{Signature} Types")]
    public class OBJTypesTable : IValue, IViewable
    {
        private const int SignatureOffset = 0;

        public CV_SIGNATURE Signature => (CV_SIGNATURE) chunk.PeekUInt32(SignatureOffset);

        private TypTypeList? types;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public unsafe TypTypeList List
        {
            get
            {
                if (types == null)
                {
                    var block = chunk.block;

                    //C7 and C11 use ST strings
                    var sig = Signature;
                    var isLengthPrefixed = sig == CV_SIGNATURE.C7 || sig == CV_SIGNATURE.C11;

                    ICodeViewAccessor codeViewAccessor = null;

                    if (block is GlobalMemoryBlock b)
                    {
                        codeViewAccessor = new OBJFileCodeViewAccessor((OBJFile) b.File, isLengthPrefixed);
                    }
                    else
                    {
                        var s = (GlobalSubMemoryBlock) block;
                        codeViewAccessor = new LongImportLibraryMemberSymbolAccessor((LongImportLibraryMember) s.Owner, isLengthPrefixed);
                    }

                    SymbolMemoryTracker.RegisterCVSymbolMemory(chunk, codeViewAccessor, null);
                    types = new TypTypeList(chunk.Pointer + 4, Length - 4);
                }

                return types;
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private int[] indexToOffsetMap;

        /// <summary>
        /// Gets the number of bytes contained in the table.
        /// </summary>
        public int Length { get; }

        internal OBJTypesTable(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            Length = length;

#if STRESS_TEST
            _ = Types;
#endif
        }

        internal unsafe TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            //Make sure the symbol accessor is registered
            _ = List;

            if (indexToOffsetMap == null)
            {
                var p = chunk.Pointer + 4;
                var l = Length;

                var i = 0;
                var off = 0;

                using var results = new PooledList<int>();

                while (off < l)
                {
                    var t = (TYPTYPE*) (p + off);

                    results.Add(off);

                    i++;
                    off += t->len + 2;
                }

                indexToOffsetMap = results.ToArray();
            }

            return types.GetTypeFromOffset(indexToOffsetMap[typeIndex - 0x1000]);
        }

        public unsafe void CopyTo(Span<byte> destination)
        {
            new Span<byte>(chunk.Pointer, Length).CopyTo(destination);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //If the signature can be garbage, we need to not write the signature and not do offset + 4 below
            Debug.Assert(Signature is CV_SIGNATURE.C7 or CV_SIGNATURE.C11 or CV_SIGNATURE.C13);

            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);
            writer.WriteGlobal(Offset + 4, List);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
