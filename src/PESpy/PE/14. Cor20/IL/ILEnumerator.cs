using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.IL
{
    internal class ILEnumeratorDebugView
    {
        private ILEnumerator _enumerator;

        //Similar to the issue below, we need to delay computing the items until the enumeration
        //is expanded to avoid having all of the items after the IL member in ImageCorILMethod
        //A/V'ing
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ILInstruction[] Items => _enumerator.ToArray();

        internal ILEnumeratorDebugView(ILEnumerator enumerator)
        {
            _enumerator = enumerator;
        }
    }

    //Note: while ideally we'd like to show a count, for some reason when we do this upsets
    //the Visual Studio debugger and it decides that all properties listed after the IL property
    //in ImageCorILMethod A/V. I'm not sure if this is perhaps related to our IEnumerable/IEnumerator
    //shenanigans we perform below; in any case, for now we don't show a count

    //[DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ILEnumeratorDebugView))]
    public struct ILEnumerator : IEnumerable<ILInstruction>, IEnumerator<ILInstruction>
    {
        private ILDecoder _decoder;

        //private int? _count;

        //public int Count
        //{
        //    get
        //    {
        //        if (_count == null)
        //        {
        //            var count = 0;

        //            var decoder = _decoder;
        //            decoder._byteReader.Offset = 0;

        //            while (decoder.TryDecode(out _))
        //                count++;

        //            _count = count;
        //        }

        //        return _count.Value;
        //    }
        //}

        public ILDecoder Decoder
        {
            get
            {
                var byteReader = _decoder._byteReader;
                byteReader.Offset = 0;

                return new ILDecoder(byteReader);
            }
        }

        internal ILEnumerator(ILDecoder decoder)
        {
            _decoder = decoder;
        }

        public ILEnumerator GetEnumerator()
        {
            var byteReader = _decoder._byteReader;
            byteReader.Offset = 0;
            return new ILEnumerator(new ILDecoder(byteReader));
        }

        IEnumerator<ILInstruction> IEnumerable<ILInstruction>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private ILInstruction _current;

        public ILInstruction Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext() => _decoder.TryDecode(out _current);

        public void Reset()
        {
            _decoder._byteReader.Offset = 0;
            _current = default;
        }

        public void Dispose()
        {

        }
    }
}
