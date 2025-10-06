using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy
{
    internal class PooledListDebugView<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items { get; }

        public PooledListDebugView(PooledList<T> pooledList)
        {
            //We're having some issues in Merger wherein with over 500,000 items, the debug view
            //is very slow to copy the array in the debugger
            Items = pooledList.GetArrayUnsafe();
        }
    }

    [DebuggerTypeProxy(typeof(PooledListDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    
}
