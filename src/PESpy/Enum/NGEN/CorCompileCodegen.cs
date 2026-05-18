namespace PESpy
{
    public enum CorCompileCodegen
    {
        CORCOMPILE_CODEGEN_DEBUGGING = 0x0001,   // suports debugging (unoptimized code with symbol info)

        CORCOMPILE_CODEGEN_PROFILING = 0x0004,   // supports profiling
        CORCOMPILE_CODEGEN_PROF_INSTRUMENTING = 0x0008   // code is instrumented to collect profile count info
    }
}
