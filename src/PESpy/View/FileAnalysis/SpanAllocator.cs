using System;
using System.Collections.Generic;

namespace PESpy.View
{
    /// <summary>
    /// Represents a custom allocator capable of storing multiple lists of values within a single buffer.<para/>
    /// This type can be used to massively reduce memory consumption by eliminating the overhead inherent in maintaining
    /// separate list and array objects for each entity that wishes to store a range of values.<para/>
    /// 
    /// e.g. <see cref="ViewByte"/> entities may wish to store all xrefs that are associated with them; rather than allocate a separate
    /// list for each byte with xrefs, the <see cref="SpanAllocator{T}"/> can reserve an area of memory for storing the precise number of
    /// elements that need to be stored with zero overhead (beyond the handle that the caller must then hold on to)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SpanAllocator<T>
    {
        private T[] _buffer;

        public SpanAllocator(int capacity)
        {
            _buffer = new T[capacity];
            _freeList.AddFirst(new SpanAllocatorHandle(0, capacity));
        }

        private LinkedList<SpanAllocatorHandle> _freeList = new LinkedList<SpanAllocatorHandle>();

        public SpanAllocatorHandle Alloc(Span<T> items)
        {
            var handle = Alloc(items.Length);
            items.CopyTo(_buffer.AsSpan(handle.Index, handle.Length));
            return handle;
        }

        public SpanAllocatorHandle Alloc(int length)
        {
            var freeList = _freeList;

            for (var node = freeList.First; node != null; node = node.Next)
            {
                var nodeValue = node.Value;

                if (length <= nodeValue.Length)
                {
                    //This node can satisfy the request
                    if (length == nodeValue.Length)
                    {
                        //Remove this node from the list
                        freeList.Remove(node);
                        
                        return nodeValue;
                    }
                    else
                    {
                        var result = new SpanAllocatorHandle(nodeValue.Index, length);
                        node.Value = new SpanAllocatorHandle(nodeValue.Index + length, nodeValue.Length - length);
                        return result;
                    }
                }
            }

            //We've run out of memory; we need to expand the size of the buffer
            var oldCapacity = _buffer.Length;
            var newCapacity = Math.Max(length, oldCapacity * 2);
            Array.Resize(ref _buffer, newCapacity);

            var extraCount = newCapacity - oldCapacity;

            var handle = new SpanAllocatorHandle(oldCapacity, length);

            if (extraCount > length)
            {
                //Carve out just the space for the newly added items, and add the rest to the free list
                Free(new SpanAllocatorHandle(oldCapacity + length, extraCount - length));
            }

            return handle;
        }

        public void Free(SpanAllocatorHandle handle)
        {
            if (handle.IsEmpty)
                return;

#if DEBUG
            _buffer.AsSpan(handle.Index, handle.Length).Clear();
#endif

            //Find the best place to insert this handle in the free list, and coalesce
            //if possible

            var freeList = _freeList;

            var current = freeList.First;
            LinkedListNode<SpanAllocatorHandle> previous = null;

            while (current != null && current.Value.Index < handle.Index)
            {
                previous = current;
                current = current.Next;
            }

            if (previous != null)
            {
                //Try merge with previous

                handle = new SpanAllocatorHandle(previous.Value.Index, previous.Value.Length + handle.Length);

                var nextPrevious = previous.Previous;
                freeList.Remove(previous);
                previous = nextPrevious;
            }

            if (current != null && handle.Index + handle.Length == current.Value.Index)
            {
                //Try merge with next

                handle = new SpanAllocatorHandle(handle.Index, handle.Length + current.Value.Length);
                freeList.Remove(current);
            }

            if (previous != null)
                freeList.AddAfter(previous, handle);
            else
                freeList.AddFirst(handle);
        }

        public SpanAllocatorHandle Realloc(SpanAllocatorHandle handle, Span<T> items)
        {
            if (handle.IsEmpty)
                return Alloc(items);
            else
            {
                //Try and find a free block directly after the current value

                var freeList = _freeList;

                var node = freeList.First;

                while (node != null)
                {
                    var nodeValue = node.Value;

                    if (handle.Index + handle.Length == nodeValue.Index)
                    {
                        //This block is directly after the handle. Can it satisfy our request?

                        if (nodeValue.Length >= items.Length)
                        {
                            var mergedHandle = new SpanAllocatorHandle(handle.Index, handle.Length + items.Length);

                            items.CopyTo(_buffer.AsSpan(nodeValue.Index, items.Length));

                            if (nodeValue.Length == items.Length)
                            {
                                //The entire block after us should be merged into the current block

                                freeList.Remove(node);
                            }
                            else
                            {
                                //Just carve out the space we need

                                node.Value = new SpanAllocatorHandle(nodeValue.Index + items.Length, nodeValue.Length - items.Length);
                            }
                        }
                        else
                        {
                            //The next block cannot satisfy the request
                            break;
                        }
                    }

                    node = node.Next;
                }

                //Failed to expand in place

                var newHandle = Alloc(handle.Length + items.Length);

                var newSpan = _buffer.AsSpan(newHandle.Index, newHandle.Length);

                //Copy the old items over
                _buffer.AsSpan(handle.Index, handle.Length).CopyTo(newSpan);

                //Copy the new items over
                items.CopyTo(newSpan.Slice(handle.Length));

                Free(handle);

                return newHandle;
            }
        }

        internal Span<T> GetSpan(SpanAllocatorHandle handle) => _buffer.AsSpan(handle.Index, handle.Length);

        public void Trim()
        {
            //Find the free node that goes to the end of the list; remove that node
            //from the free list and then resize the underlying buffer

            var freeList = _freeList;

            for (var node = freeList.First; node != null; node = node.Next)
            {
                var nodeValue = node.Value;

                if (nodeValue.Index + nodeValue.Length == _buffer.Length)
                {
                    Array.Resize(ref _buffer, _buffer.Length - nodeValue.Length);

                    freeList.Remove(node);
                }
            }
        }
    }
}
