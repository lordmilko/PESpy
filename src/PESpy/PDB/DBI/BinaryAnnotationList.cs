using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.PDB
{
    internal class BinaryAnnotationListDebugView
    {
        private BinaryAnnotationList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public BinaryAnnotation[] Items => list.ToArray();

        public BinaryAnnotationListDebugView(BinaryAnnotationList list)
        {
            this.list = list;
        }
    }

    //Type encapsulates the native byte blob
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(BinaryAnnotationListDebugView))]
    public unsafe struct BinaryAnnotationList : IEnumerable<BinaryAnnotation>
    {
        private readonly byte* pStart;
        private readonly int length;

        private int? count;

        public int Count
        {
            get
            {
                if (this.count == null)
                {
                    var start = pStart;
                    var end = start + length;

                    var count = 0;

                    while (start < end)
                    {
                        //If we encounter trailing padding, we're at the end
                        if (!BinaryAnnotation.TryDecode(ref start, out _))
                            break;

                        count++;
                    }

                    this.count = count;
                }

                return count.Value;
            }
        }

        internal BinaryAnnotationList(byte* pStart, int length)
        {
            this.pStart = pStart;
            this.length = length;
        }

        public Enumerator GetEnumerator() => new Enumerator(pStart, length);

        IEnumerator<BinaryAnnotation> IEnumerable<BinaryAnnotation>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<BinaryAnnotation>
        {
            private byte* pStart;
            private readonly byte* pEnd;

            internal Enumerator(byte* pStart, int length)
            {
                this.pStart = pStart;
                pEnd = pStart + length;
            }

            public bool MoveNext()
            {
                if (pStart < pEnd)
                {
                    if (BinaryAnnotation.TryDecode(ref pStart, out var binaryAnnotation))
                    {
                        Current = binaryAnnotation;
                        return true;
                    }
                    else
                        pStart = pEnd; //We encountered trailing padding. We're at the end
                }

                Debug.Assert(pStart == pEnd);
                Current = default;
                return false;
            }

            public BinaryAnnotation Current { get; private set; }

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
