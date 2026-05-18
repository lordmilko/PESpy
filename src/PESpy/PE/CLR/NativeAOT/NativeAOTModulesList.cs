using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

//Specifying the namespace here seems redundant given the class name also starts with "NativeAOT"
namespace PESpy
{
    internal class NativeAOTModulesListDebugView
    {
        private NativeAOTModulesList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public VA<NativeAOT.ReadyToRunHeader>[] Items => list.ToArray();

        internal NativeAOTModulesListDebugView(NativeAOTModulesList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeAOTModulesListDebugView))]
    public class NativeAOTModulesList : IEnumerable<VA<NativeAOT.ReadyToRunHeader>>, IViewableValue
    {
        public int Count => numModules;

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int numModules;

        private VA<NativeAOT.ReadyToRunHeader>[]? _headers;

        /* I've observed that __modules_a may be 0, and then the actual RVAs
         * are inside it and __modules_z. __modules_z also seems to be 0, but it's
         * not included in our length. StartupCodeHelpers.CreateTypeManagers
         * says that the null pointers are a side effect of how the linker merges
         * the sections */
        public VA<NativeAOT.ReadyToRunHeader> this[int index]
        {
            get
            {
                if (_headers == null)
                    _headers = new VA<NativeAOT.ReadyToRunHeader>[numModules];

                ref var item = ref _headers[index];

                if (item.ListedAddress == 0)
                {
                    var ptr = (long) chunk.PeekPointer(index * chunk.PointerSize);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(ptr, out var valueChunk))
                        item = new VA<NativeAOT.ReadyToRunHeader>(ptr, valueChunk.AbsoluteOffset, new NativeAOT.ReadyToRunHeader(valueChunk));
                    else
                        item = new VA<NativeAOT.ReadyToRunHeader>(ptr);
                }

                return item;
            }
        }

        public long ModulesZ => (long) chunk.PeekPointer(numModules * chunk.PointerSize);

        internal NativeAOTModulesList(in MemoryChunk chunk, int numModules)
        {
            this.chunk = chunk;
            this.numModules = numModules;
        }

        public Enumerator GetEnumerator()
        {
            if (_headers == null)
                _headers = new VA<NativeAOT.ReadyToRunHeader>[numModules]; //We're going to need this

            return new Enumerator(chunk, _headers);
        }

        IEnumerator<VA<NativeAOT.ReadyToRunHeader>> IEnumerable<VA<NativeAOT.ReadyToRunHeader>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var ptrSize = chunk.PointerSize;

            var read = 0;

            foreach (var value in this)
            {
                writer.WriteGlobal(Offset + read, value.ListedAddress, ptrSize, read == 0 ? ViewKind.NativeAOTModulesA : ViewKind.NativeAOTModuleAddress);

                //Write the ReadyToRunHeader
                writer.WriteVAPointerField(value, Offset, read);
                read += ptrSize;
            }

            writer.WriteGlobal(Offset + read, ModulesZ, ptrSize, ViewKind.NativeAOTModulesZ);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        public struct Enumerator : IEnumerator<VA<NativeAOT.ReadyToRunHeader>>
        {
            private PEFile _peFile;
            private long _imageBase;
            private int _index;
            private VA<NativeAOT.ReadyToRunHeader>[] _headers;
            private readonly MemoryChunk chunk;

            internal Enumerator(in MemoryChunk chunk, VA<NativeAOT.ReadyToRunHeader>[] headers)
            {
                this.chunk = chunk;
                _peFile = chunk.PEFile();
                _imageBase = _peFile.OptionalHeader.ImageBase;
                _headers = headers;
            }

            public bool MoveNext()
            {
                if (_index < _headers.Length)
                {
                    ref var item = ref _headers[_index];

                    if (item.ListedAddress == 0)
                    {
                        var ptr = (long) chunk.PeekPointer(_index * chunk.PointerSize);

                        if (ptr != 0)
                        {
                            var rva = (int) (ptr - _imageBase);

                            if (_peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                                item = new VA<NativeAOT.ReadyToRunHeader>(ptr, rva, new NativeAOT.ReadyToRunHeader(valueChunk));
                            else
                                item = new VA<NativeAOT.ReadyToRunHeader>(ptr);
                        }
                    }

                    _index++;

                    Current = item;
                    return true;
                }

                return false;
            }

            public VA<NativeAOT.ReadyToRunHeader> Current { get; private set; }

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
