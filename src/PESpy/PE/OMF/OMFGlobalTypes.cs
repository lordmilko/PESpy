using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    [Source(SourceKind.cvexefmt_h)]
    [DebuggerDisplay("{flags.sig} Global Types ({cType.ToString(),nq})")]
    public class OMFGlobalTypes : IEnumerable<TypType>
    {
        public OMFTypeFlags flags => chunk.PeekUnmanaged<OMFTypeFlags>(0);

        public int cType => chunk.PeekInt32(4);

        public NativeSpan<int> typeOffset => chunk.PeekNativeSpan<int>(8, cType);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private TypType[] Items => this.ToArray();

        //https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
        //pdf page 81

        //Ostensibly you could just interpret the start of the TYPTYPE region as a TypTypeList, however the spec that each record
        //must start on a word boundary, and we don't enforce such alignment in TypTypeList
        public unsafe TypType this[int index] => (TYPTYPE*) (chunk.Pointer + typeInfoStart + typeOffset[index]);

        private readonly MemoryChunk chunk;
        private readonly int typeInfoStart;

        internal unsafe OMFGlobalTypes(in MemoryChunk chunk, int length, NB05SymbolAccessor codeViewAccessor)
        {
            this.chunk = chunk;

            //In NB07/NB08, each type offset is relative to the beginning of OMFGlobalTypes. In NB09 each type offset is relative to
            //the beginning of the area where the type names are listed. Despite what the PDF says, these do not appear to be type "names",
            //but rather just regular old TYPTYPE records
            switch (codeViewAccessor.CodeViewSig)
            {
                //The spec only calls upt NB07 and NB08, but what about NB05 and NB06?
                case CodeViewSig.NB05:
                case CodeViewSig.NB06:
                    //My NB05 sample didn't have OMFGlobalTypes, but I'm going to assume that it's the same as NB07/NB08
                    Debug.Assert(false);
                    goto case CodeViewSig.NB07;

                case CodeViewSig.NB07:
                case CodeViewSig.NB08:
                    typeInfoStart = 0;
                    break;

                default:
                    typeInfoStart = sizeof(int) + sizeof(int) + (cType * sizeof(int));
                    break;
            }

            SymbolMemoryTracker.RegisterCVSymbolMemory(chunk, codeViewAccessor, null);
        }

        public Enumerator GetEnumerator() => new Enumerator(typeInfoStart == 0 ? chunk : chunk.Slice(typeInfoStart), typeOffset);

        IEnumerator<TypType> IEnumerable<TypType>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<TypType>
        {
            private readonly MemoryChunk typeChunk;
            private readonly NativeSpan<int> offsets;
            private int index;

            internal Enumerator(in MemoryChunk typeChunk, NativeSpan<int> offsets)
            {
                this.typeChunk = typeChunk;
                this.offsets = offsets;
                index = 0;
                Current = default;
            }

            public unsafe bool MoveNext()
            {
                if (index < offsets.Length)
                {
                    Current = (TYPTYPE*) (typeChunk.Pointer + offsets[index]);
                    index++;
                    return true;
                }

                Current = default;
                return false;
            }

            public TypType Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
