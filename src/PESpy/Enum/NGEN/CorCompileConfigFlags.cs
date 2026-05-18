namespace PESpy
{
    /// <summary>
    /// Used for INativeImageInstallInfo::GetConfigMask()<para/>
    ///
    /// A bind will ask for the particular bits it needs set; if all bits are set, it is a match. Additional
    /// bits are ignored.
    /// </summary>
    public enum CorCompileConfigFlags
    {
        CORCOMPILE_CONFIG_DEBUG_NONE = 0x01, // Assembly has Optimized code
        CORCOMPILE_CONFIG_DEBUG = 0x02, // Assembly has non-Optimized debuggable code
        CORCOMPILE_CONFIG_DEBUG_DEFAULT = 0x08, // Additional flag set if this particular setting is the
        // one indicated by the assembly debug custom attribute.

        CORCOMPILE_CONFIG_PROFILING_NONE = 0x100, // Assembly code has profiling hooks
        CORCOMPILE_CONFIG_PROFILING = 0x200, // Assembly code has profiling hooks

        CORCOMPILE_CONFIG_INSTRUMENTATION_NONE = 0x1000, // Assembly code has no instrumentation
        CORCOMPILE_CONFIG_INSTRUMENTATION = 0x2000, // Assembly code has basic block instrumentation
    }
}
