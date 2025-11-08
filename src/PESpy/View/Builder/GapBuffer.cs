using System;
using System.Collections.Generic;

namespace PESpy
{
    internal class GapBuffer<T>
    {
        private const int defaultCapacity = 4;

        private int gapStart;
        private int gapEnd;
        private T[] buffer;

        public int Count => Capacity - GapSize;

        public int GapSize => gapEnd - gapStart;

        public int Capacity
        {
            get => buffer.Length;
            set
            {
                if (value == buffer.Length)
                    return;

                if (value < Count) //Capacity cannot be decreased beyond the number of elements we have
                    throw new ArgumentOutOfRangeException();

                //If we don't have any elements, the capacity could be reset to minimum
                if (value > 0)
                {
                    var oldLength = buffer.Length;

                    var shift = value - oldLength;

                    var isGapAtEnd = oldLength == gapEnd;

                    //Resize the buffer, and then shift everything after the gap to the end, thereby expanding the size of the gap
                    Array.Resize(ref buffer, value);

                    //If the gap is already at the end, we don't need to shift any content down to "grow" the gap
                    if (!isGapAtEnd)
                    {
                        var newLength = buffer.Length;

                        var lengthAfterGapEnd = oldLength - gapEnd;

                        //I confirmed that Array.Copy does work right to left, so we aren't at risk of corrupting our data as we shift it down
                        Array.Copy(buffer, sourceIndex: gapEnd, buffer, newLength - lengthAfterGapEnd, lengthAfterGapEnd);
                    }

                    //After the copy, the gap will essentially contain "junk" (the old data that has now been shifted down).
                    //This is OK; we know that the data contained in the gap is not valid. When the gap moves, or some data
                    //is entered into the gap, this junk will be overwritten.
                    gapEnd += shift;
                }
                else
                {
                    //Caller wants the capacity to be 0; we're not going to do that. Reset back to the default state
                    //we have when constructing a new object
                    buffer = new T[defaultCapacity];
                    gapStart = 0;
                    gapEnd = defaultCapacity;
                }
            }
        }

        public GapBuffer()
        {
            //By default, everything is a gap, so the gap extends from 0 - defaultCapacity
            buffer = new T[defaultCapacity];
            gapEnd = defaultCapacity;
        }

        public GapBuffer(int capacity)
        {
            buffer = new T[capacity];
            gapEnd = capacity;
        }

        public GapBuffer(List<T> list)
        {
            buffer = list.ToArray();
            gapEnd = list.Count;
            gapStart = gapEnd;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException();

                //If the index is on the left side of the gap, it's all fine. If it's beyond the start of the gap
                //however, we need to skip over the size of the gap to get at the actual content located after the gap
                if (index >= gapStart)
                    index += GapSize;

                return buffer[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException();

                if (index >= gapStart)
                    index += GapSize;

                buffer[index] = value;
            }
        }

        //Moves the gap to the end of the buffer, and inserts text to the beginning of the gap
        public void Append(ReadOnlySpan<T> value) => InsertRange(Count, value);

        //Moves the gap to the beginning of the buffer, and inserts text at the end of the gap.
        //Useful when dynamically loading content while scrolling up
        public void Prepend(ReadOnlySpan<T> value)
        {
            /* Suppose we have the following buffer
             *
             *     b 0 0 0
             *
             * And we want to prepend "a" such that we end up with
             *
             *     0 0 a b
             *
             * by default, if you simply do "Insert" you will end up with
             *
             *     a 0 0 b
             *
             * because it's assumed that you want to do subsequent inserts _after_ "a". To work around
             * this, we will first pretend that we want to insert a value at the very beginning of
             * the buffer, thereby moving the gap. However, we will then change the index of the position
             * that we want to write to to actually be the area at the _end_ of the gap
             */

            //Move the gap to the start and ensure it's big enough
            PositionGap(0);

            var length = value.Length;
            EnsureGapCapacity(length);

            //Now insert the value at the _end_ of the gap, rather than at the beginning
            var index = gapEnd - length;
            value.CopyTo(buffer.AsSpan(index, length));
            gapEnd -= length;
        }

        public void InsertRange(int index, ReadOnlySpan<T> value)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException();

            //Move the gap into position
            PositionGap(index);

            var length = value.Length;

            //Ensure that the gap is large enough to accomodate the new value
            EnsureGapCapacity(length);

            value.CopyTo(buffer.AsSpan(gapStart, length));
            gapStart += length;
        }

        public void InsertRange(int index, PooledList<T> value)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException();

            //Move the gap into position
            PositionGap(index);

            var length = value.Count;

            //Ensure that the gap is large enough to accomodate the new value
            EnsureGapCapacity(length);

            value.CopyTo(buffer, gapStart);
            gapStart += length;
        }

        public void Insert(int index, T value)
        {
            PositionGap(index);

            EnsureGapCapacity(1);

            buffer[index] = value;
            gapStart++;
        }

        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                //Moving the gap over the area we want to remove will cause the target area
                //to be moved to just after the gap. By then growing the gap into that area,
                //the area will have effectively been "removed"
                PositionGap(index);
                gapEnd += count;
            }
        }

        public ReadOnlySpan<T> GetText(int index, int length)
        {
            if (index < gapStart)
            {
                var end = index + length;

                if (end < gapStart)
                {
                    //The entire value is before the gap
                    return buffer.AsSpan(index, length);
                }

                //Part of the value is before the gap and part of the value is after it.
                //We need to synthesize a unified view of the value
                var spanBuffer = new T[length];
                var lengthBeforeGap = gapStart - index;
                Array.Copy(buffer, index, spanBuffer, 0, lengthBeforeGap);

                var lengthAfterGap = length - lengthBeforeGap;
                Array.Copy(buffer, gapEnd, spanBuffer, lengthBeforeGap, lengthAfterGap);
                return spanBuffer;
            }
            else
            {
                //The entire value is after the gap
                return buffer.AsSpan(gapEnd + index - gapStart, length);
            }
        }

        private void PositionGap(int index)
        {
            if (index == gapStart)
                return; //This is already the position of the gap

            if (GapSize == 0)
            {
                //The caller is going to need to resize the gap. Specify
                //where we want the gap to be and return
                gapStart = index;
                gapEnd = index;
                return;
            }

            if (index < gapStart)
            {
                //We need to move the gap to the left, which means we need to shift content to the right

                var shift = gapStart - index;

                //Copy the data from just before the beginning of the current gap to the end area of the current gap.
                //Once we update gapStart and gapEnd, this will have the effect of having "moved" this data to "after"
                //the gap, while also having "moved" the gap down a bit. The gap will contain junk from the data that was "moved",
                //but this is OK
                Array.Copy(buffer, sourceIndex: index, buffer, destinationIndex: gapEnd - shift, shift);

                gapStart -= shift;
                gapEnd -= shift;
            }
            else
            {
                //We need to move the gap to the right, which means we need to shift content to the left

                var shift = index - gapStart;

                Array.Copy(buffer, sourceIndex: gapEnd, buffer, destinationIndex: gapStart, shift);

                gapStart += shift;
                gapEnd += shift;
            }
        }

        private void EnsureGapCapacity(int length)
        {
            if (length > GapSize)
                Capacity = (Count + length) * 2;
        }

        public void Clear()
        {
            gapStart = 0;
            gapEnd = buffer.Length;
        }

        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                var chars = new char[Count];

                Array.Copy(buffer, chars, gapStart);
                Array.Copy(buffer, gapEnd, chars, gapStart, buffer.Length - gapEnd);

                return new string(chars).Replace("\r", "\\r").Replace("\n", "\\n"); //temp. maybe make this a debuggerdisplay?
            }

            return base.ToString();
        }
    }
}
