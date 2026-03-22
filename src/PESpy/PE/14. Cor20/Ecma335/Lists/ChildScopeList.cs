using System;
using System.Collections;
using System.Collections.Generic;

namespace PESpy.Ecma335
{
    public readonly struct ChildScopeList : IEnumerable<LocalScopeRow>
    {
        private readonly LocalScopeTable _localScopeTable;
        private readonly int _parentEndOffset;
        private readonly LocalScopeIndex _parentScope;
        private readonly MethodDefIndex _parentMethodIndex;

        internal ChildScopeList(LocalScopeIndex parentScope, LocalScopeTable localScopeTable)
        {
            _localScopeTable = localScopeTable;

            _parentEndOffset = localScopeTable.GetEndOffset(parentScope);
            _parentMethodIndex = localScopeTable.GetMethod(parentScope);
            _parentScope = parentScope;
        }

        public Enumerator GetEnumerator() => new Enumerator(_localScopeTable, _parentEndOffset, _parentScope, _parentMethodIndex);

        IEnumerator<LocalScopeRow> IEnumerable<LocalScopeRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<LocalScopeRow>
        {
            private int _currentRowId;
            private readonly int _parentEndOffset;
            private readonly int _parentRowId;
            private readonly LocalScopeTable _localScopeTable;
            private readonly MethodDefIndex _parentMethodIndex;

            internal Enumerator(
                LocalScopeTable localScopeTable,
                int parentEndOffset,
                LocalScopeIndex parentScope,
                MethodDefIndex parentMethodIndex)
            {
                _localScopeTable = localScopeTable;
                _parentEndOffset = parentEndOffset;
                _parentRowId = parentScope.RowId;
                _parentMethodIndex = parentMethodIndex;

                _currentRowId = 0;
            }

            public bool MoveNext()
            {
                int currentRowId = _currentRowId;
                if (currentRowId == CompressedModelHeap.EnumEnded)
                {
                    return false;
                }

                int currentEndOffset;
                int nextRowId;
                if (currentRowId == 0)
                {
                    currentEndOffset = -1;
                    nextRowId = _parentRowId + 1;
                }
                else
                {
                    currentEndOffset = _localScopeTable.GetEndOffset((LocalScopeIndex) currentRowId);
                    nextRowId = currentRowId + 1;
                }

                int rowCount = _localScopeTable.Count;

                while (true)
                {
                    if (nextRowId > rowCount || _parentMethodIndex != _localScopeTable.GetMethod((LocalScopeIndex) nextRowId))
                    {
                        _currentRowId = CompressedModelHeap.EnumEnded;
                        return false;
                    }

                    int nextEndOffset = _localScopeTable.GetEndOffset((LocalScopeIndex) nextRowId);

                    // If the end of the next scope is lesser than or equal the current end
                    // then it's nested into the current scope and thus not a child of
                    // the current scope parent.
                    if (nextEndOffset > currentEndOffset)
                    {
                        // If the end of the next scope is greater than the parent end,
                        // then we ran out of the children.
                        if (nextEndOffset > _parentEndOffset)
                        {
                            _currentRowId = CompressedModelHeap.EnumEnded;
                            return false;
                        }

                        _currentRowId = nextRowId;
                        return true;
                    }

                    nextRowId++;
                }
            }

            public LocalScopeRow Current => _localScopeTable[(LocalScopeIndex) _currentRowId];

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
