using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PESpy
{
    internal struct RefCounted<T> where T : class, IDisposable
    {
        private T? _value;
        private bool _disposing;
        private object _lock = new();
        private int _refCount;
        private ManualResetEventSlim _waitEvent = new ManualResetEventSlim(true);

        public bool IsEmpty => _value == null;

        public RefCounted()
        {
        }

        public T? Acquire()
        {
            lock (_lock)
            {
                if (_disposing)
                    return default;

                _refCount++;

                if (_refCount == 1)
                    _waitEvent.Reset();

                return _value;
            }
        }

        public void Release()
        {
            lock (_lock)
            {
                _refCount--;

                if (_refCount == 0)
                    _waitEvent.Set();
            }
        }

        public void Set(T value)
        {
            lock (_lock)
            {
                _disposing = false;
                _value = value;
            }
        }

        public bool TryDispose()
        {
            if (_value == null)
                return false;

            Dispose();
            return true;
        }

        //We need to make sure the parent frame doesn't touch the FileAccessor so that we
        //can GC it properly
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose()
        {
            lock (_lock)
                _disposing = true;

            _waitEvent.Wait();
            _value?.Dispose();
            _value = default;
        }
    }
}
