using System;
using System.Drawing;
using PInvoke;

namespace PESpy
{
    public readonly struct NativeFont
    {
        public HFONT ToHfont() => throw new NotImplementedException();

        public NativeFont(NativeFont font, FontStyle fontStyle)
        {
            throw new NotImplementedException();
        }
    }
}
