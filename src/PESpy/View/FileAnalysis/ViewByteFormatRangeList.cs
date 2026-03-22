using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NET9_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace PESpy.View
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
        public void Add(ViewByteFormatKind kind, int startOffset, int endOffset, int? targetAddress = null)
        {
            if (list.Count == 0)
                list.Add(new ViewByteFormatRange(kind, startOffset, endOffset, targetAddress));
            else
            {
#if NET9_0_OR_GREATER
                ref var lastItem = ref CollectionsMarshal.AsSpan(list)[list.Count - 1];
#else
                var lastItem = list[list.Count - 1];
#endif

                if (lastItem.Kind == kind && lastItem.EndOffset == startOffset)
                {
                    lastItem.EndOffset = endOffset; //Grow the last item to encompass this item

#if !NET9_0_OR_GREATER
                    list[list.Count - 1] = lastItem;
#endif
                }
                else
                {
                    //Add a new item
                    list.Add(new ViewByteFormatRange(kind, startOffset, endOffset, targetAddress));
                }
            }
        }

        public void AddToPrevious(int endOffset)
        {
#if NET9_0_OR_GREATER
                ref var lastItem = ref CollectionsMarshal.AsSpan(list)[list.Count - 1];
#else
            var lastItem = list[list.Count - 1];
#endif

            lastItem.EndOffset = endOffset;

#if !NET9_0_OR_GREATER
            list[list.Count - 1] = lastItem;
#endif
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
