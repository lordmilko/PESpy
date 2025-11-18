using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PESpy
{
    [DebuggerDisplay("Count = {Count}")]
    public class ViewByteFormatRangeList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private List<ViewByteFormatRange> list;

        public int Count => list.Count;

        public int LastOffset
        {
            get
            {
                if (list.Count == 0)
                    return 0;

                var item = list[list.Count - 1];

                return item.EndOffset;
            }
        }

        public ViewByteFormatRangeList()
        {
            list = new List<ViewByteFormatRange>();
        }

        //This will intelligently decide whether we should add to previous
        public void Add(ViewByteFormatKind kind, int startOffset, int endOffset)
        {
            if (list.Count == 0)
                list.Add(new ViewByteFormatRange(kind, startOffset, endOffset));
            else
            {
                ref var lastItem = ref CollectionsMarshal.AsSpan(list)[list.Count - 1];

                if (lastItem.Kind == kind && lastItem.EndOffset == startOffset)
                    lastItem.EndOffset = endOffset; //Grow the last item to encompass this item
                else
                {
                    //Add a new item
                    list.Add(new ViewByteFormatRange(kind, startOffset, endOffset));
                }
            }
        }

        public void AddToPrevious(int endOffset)
        {
            ref var lastItem = ref CollectionsMarshal.AsSpan(list)[list.Count - 1];
            lastItem.EndOffset = endOffset;
        }

        public void Clear() => list.Clear();

        public ViewByteFormatRange[] ToArrayAndClear()
        {
            if (list.Count == 0)
                return Array.Empty<ViewByteFormatRange>();

            var results = list.ToArray();
            list.Clear();
            return results;
        }
    }
}
