using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.Ecma335;
using PESpy.View;

namespace PESpy
{
    internal class ImageCorILMethodListDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ImageCorILMethod[] Items { get; }

        internal ImageCorILMethodListDebugView(ImageCorILMethodList list)
        {
            Items = list.ToArray();
        }
    }

    /// <summary>
    /// Provides access to <see cref="ImageCorILMethod"/> instances without allocating an array.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImageCorILMethodListDebugView))]
    public class ImageCorILMethodList : IEnumerable<ImageCorILMethod>, ILightweightList<ImageCorILMethodList.Enumerator, ImageCorILMethod>
    {
        private MethodDefTable _methodDefs;
        private PEFile _peFile;

        private int? _count;

        public int Count
        {
            get
            {
                if (_count == null)
                {
                    var enumerator = GetEnumerator();

                    var count = 0;

                    while (enumerator.MoveNext())
                        count++;

                    _count = count;
                }

                return _count.Value;
            }
        }

        internal ImageCorILMethodList(MethodDefTable methodDefs, PEFile peFile)
        {
            _methodDefs = methodDefs;
            _peFile = peFile;
        }

        public ImageCorILMethod this[int index]
        {
            get
            {
                var i = 0;

                var enumerator = GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if (i == index)
                        return enumerator.Current;

                    i++;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(_methodDefs, _peFile);

        IEnumerator<ImageCorILMethod> IEnumerable<ImageCorILMethod>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<ImageCorILMethod>
        {
            MethodDefTable.Enumerator _enumerator;
            private PEFile _peFile;

            internal Enumerator(MethodDefTable table, PEFile peFile)
            {
                _enumerator = table.GetEnumerator();
                _peFile = peFile;
            }

            public ImageCorILMethod Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    //Certain methods (such as interface methods) have an RVA of 0, and so do not
                    //have an IL method

                    if (!_peFile.TryGetILValueChunk(_enumerator.Current, out var valueChunk))
                        continue;

                    var ilMethod = new ImageCorILMethod(valueChunk, out var isValid);

                    if (!isValid)
                        continue;

                    Current = ilMethod;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
