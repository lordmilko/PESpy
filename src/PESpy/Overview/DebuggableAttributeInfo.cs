using static System.Diagnostics.DebuggableAttribute;

namespace PESpy
{
    public static partial class FileOverview
    {
        public class DebuggableAttributeInfo
        {
            public string Configuration => IsDebug ? "Debug" : "Release";

            public DebuggingModes DebuggingFlags { get; set; }

            public bool IsDebug => IsJITOptimizerDisabled;

            //JIT Tracking is apparently always enabled in .NET 2.0+, so we will consider
            //ourselves to be a debug build if optimizations are disabled
            public bool IsJITTrackingEnabled => (DebuggingFlags & DebuggingModes.Default) != 0;

            public bool IsJITOptimizerDisabled => (DebuggingFlags & DebuggingModes.DisableOptimizations) != 0;

            internal DebuggableAttributeInfo(bool isJITTrackingEnabled, bool isJITOptimizerDisabled)
            {
                var debuggingFlags = DebuggingModes.None;

                if (isJITTrackingEnabled)
                    debuggingFlags |= DebuggingModes.Default;

                if (isJITOptimizerDisabled)
                    debuggingFlags |= DebuggingModes.DisableOptimizations;

                DebuggingFlags = debuggingFlags;
            }

            internal DebuggableAttributeInfo(DebuggingModes debuggingFlags)
            {
                DebuggingFlags = debuggingFlags;
            }

            public override string ToString() => $"{Configuration} ({DebuggingFlags})";
        }
    }
}
