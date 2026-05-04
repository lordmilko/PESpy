using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt_h)]
    [DebuggerDisplay("{flags.sig} Global Types ({cType.ToString(),nq})")]
    public class OMFGlobalTypes : IEnumerable<TypType>, IValue, IViewable
    {
        private const int flagsOffset = 0;
        private const int cTypeOffset = 4;
        private const int typeOffsetOffset = 8;

        public OMFTypeFlags flags => chunk.PeekUnmanaged<OMFTypeFlags>(flagsOffset);

        public int cType => chunk.PeekInt32(cTypeOffset);

        public NativeSpan<int> typeOffset => chunk.PeekNativeSpan<int>(typeOffsetOffset, cType);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private TypType[] Items => this.ToArray();

        //https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
        //pdf page 81

        //Ostensibly you could just interpret the start of the TYPTYPE region as a TypTypeList, however the spec that each record
        //must start on a word boundary, and we don't enforce such alignment in TypTypeList
        public unsafe TypType this[int index] => (TYPTYPE*) (chunk.Pointer + typeInfoStart + typeOffset[index]);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int length;
        private readonly int typeInfoStart;

        internal unsafe OMFGlobalTypes(in MemoryChunk chunk, int length, NB05SymbolAccessor codeViewAccessor)
        {
            this.chunk = chunk;
            this.length = length;

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
                    //Offsets are relative to the start of the OMFGlobalTypes record
                    typeInfoStart = 0;
                    break;

                default:
                    //Offsets are relative to the area after the end of the OMFGlobalTypes record
                    typeInfoStart = typeOffsetOffset + (cType * sizeof(int));
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //OMFGlobalTypes contains an array of offsets to types, which means every offset is technically an xref

            var structOffset = Offset;

            var baseTypTypeOffset = structOffset + typeOffsetOffset + typeInfoStart;

            var fieldOffset = typeOffsetOffset;

            foreach (var offset in typeOffset)
            {
                writer.WriteOffsetXRef(structOffset, fieldOffset, baseTypTypeOffset + offset);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.OMFGlobalTypes, length);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteStructField(nameof(flags), flagsOffset, flags);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cType), cTypeOffset, cType);
                    break;

                case 2:
                    structWriter.WriteField(nameof(typeOffset), typeOffsetOffset, typeOffset);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
