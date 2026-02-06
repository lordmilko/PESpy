using System;

namespace PESpy.UI
{
    [Flags]
    internal enum BoundsSpecified
    {
        None = 0,

        X = 1,
        Y = 2,
        Width = 4,
        Height = 8,
        Location = X | Y,
        Size = Width | Height,
        All = Location | Size
    }
}
