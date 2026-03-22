using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy
{
    internal partial class LIBFileBuilder
    {
        [DebuggerDisplay("Count = {Count}")]
        public class ImportLibraryList : IEnumerable<ImportLibraryMemberBuilder>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private List<ImportLibraryMemberBuilder> _importLibraries;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private LIBFileBuilder _libFileBuilder;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public int Count => _importLibraries.Count;

            internal ImportLibraryList(List<ImportLibraryMemberBuilder> importLibraries, LIBFileBuilder libFileBuilder)
            {
                _importLibraries = importLibraries;
                _libFileBuilder = libFileBuilder;
            }

            public ImportLibraryMemberBuilder this[int index] => _importLibraries[index];

            public void Add(ImportLibraryMemberBuilder importLibrary)
            {
                if (importLibrary.Names.Count == 0)
                    throw new NotImplementedException(); //Need to have at least 1 name

                throw new NotImplementedException();
            }

            public void Remove(ImportLibraryMemberBuilder importLibrary)
            {
                _importLibraries.Remove(importLibrary);

                foreach (var name in importLibrary.Names)
                    _libFileBuilder._nameToLibraryMap.Remove(name);
            }

            public List<ImportLibraryMemberBuilder>.Enumerator GetEnumerator() => _importLibraries.GetEnumerator();

            IEnumerator<ImportLibraryMemberBuilder> IEnumerable<ImportLibraryMemberBuilder>.GetEnumerator() => _importLibraries.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _importLibraries.GetEnumerator();
        }
    }
}
