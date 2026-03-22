using System;
using PInvoke;

namespace PESpy
{
    //Don't inherit from SafeHandleZeroOrMinusOneIsInvalid; that will probably bring in a bunch of stuff we don't need
    internal unsafe class SafeWinHttpHandle : IDisposable
    {
        public SafeWinHttpHandle Parent { get; set; }

        private void* _hInternet;

        public static implicit operator SafeWinHttpHandle(void* value) => new SafeWinHttpHandle { _hInternet = value };

        public static implicit operator void*(SafeWinHttpHandle value) => value._hInternet;

        public void Dispose()
        {
            if (Parent != default)
            {
                Parent.Dispose();
                Parent = null;
            }

            if (_hInternet != default)
            {
                WinHttp.WinHttpCloseHandle(_hInternet);
                _hInternet = default;
            }
        }
    }
}
