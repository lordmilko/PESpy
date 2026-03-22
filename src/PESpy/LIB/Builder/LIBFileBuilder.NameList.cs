using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy
{
    internal partial class LIBFileBuilder
    {
        [DebuggerDisplay("Count = {Count}")]
        public class NameList : IEnumerable<string>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private List<string> _names = new List<string>();

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private ImportLibraryMemberBuilder _importLibraryBuilder;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private LIBFileBuilder _libFileBuilder;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public int Count => _names.Count;

            internal NameList(ImportLibraryMemberBuilder importLibraryBuilder, LIBFileBuilder libFileBuilder)
            {
                _importLibraryBuilder = importLibraryBuilder;
                _libFileBuilder = libFileBuilder;
            }

            public string this[int index] => _names[index];

            public void Add(string name)
            {
                if (_importLibraryBuilder is LongImportLibraryMemberBuilder b && b.IsBad)
                    throw new NotImplementedException(); //Not allowed to modify

                AddInternal(name);

                _importLibraryBuilder.FirstLinkerIndex.Add(uint.MaxValue);
                _libFileBuilder._nameToFirstLinkerOffsetIndexMap[name] = uint.MaxValue;

                if (!_libFileBuilder._nameToSecondLinkerOffsetIndexMap.ContainsKey(name))
                    _libFileBuilder._nameToSecondLinkerOffsetIndexMap[name] = uint.MaxValue;
            }

            internal void AddInternal(string name)
            {
                if (_names.Contains(name))
                    throw new NotImplementedException();

                if (_libFileBuilder._nameToLibraryMap.ContainsKey(name))
                    throw new NotImplementedException();

                _libFileBuilder._nameToLibraryMap[name] = _importLibraryBuilder;
                _names.Add(name);
            }

            public void Remove(string name)
            {
                if (_importLibraryBuilder is LongImportLibraryMemberBuilder b && b.IsBad)
                    throw new NotImplementedException(); //Not allowed to modify

                _names.Remove(name);
                _libFileBuilder._nameToLibraryMap.Remove(name);

                var offset = _libFileBuilder._nameToFirstLinkerOffsetIndexMap[name];
                _libFileBuilder._nameToFirstLinkerOffsetIndexMap.Remove(name);
                _importLibraryBuilder.FirstLinkerIndex.Remove(offset);

                _libFileBuilder._nameToSecondLinkerOffsetIndexMap.Remove(name);
            }

            public List<string>.Enumerator GetEnumerator() => _names.GetEnumerator();

            IEnumerator<string> IEnumerable<string>.GetEnumerator() => _names.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _names.GetEnumerator();
        }
    }
}
