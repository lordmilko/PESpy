using System;

namespace PESpy.Native
{
    //ClrEngineMetrics
    internal struct CLR_ENGINE_METRICS
    {
        public int cbSize;
        public int dwDbiVersion;
        public IntPtr phContinueStartupEvent;
    }
}
