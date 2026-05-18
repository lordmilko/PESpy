namespace PESpy
{
    public enum CorCompileILRegion
    {
        CORCOMPILE_ILREGION_INLINEABLE,     // Public inlineable methods
        CORCOMPILE_ILREGION_WARM,           // Other inlineable methods and methods that failed to NGen
        CORCOMPILE_ILREGION_GENERICS,       // Generic methods (may be needed to compile non-NGened instantiations)
        CORCOMPILE_ILREGION_COLD,           // Everything else (should be touched in rare scenarios like reflection or profiling only)
        CORCOMPILE_ILREGION_COUNT,
    }
}
