using System;
using ClrDebug;

namespace PESpy.Native
{
    //ClrEngineMetrics
    public struct CLR_ENGINE_METRICS
    {
        public int cbSize;
        public CorDebugInterfaceVersion dwDbiVersion;
        public IntPtr phContinueStartupEvent;
    }
}
