namespace PESpy.View
{
    public enum FileAnalyzerProgressPhase
    {
        DiscoverGlobals,
        DiscoverCodeRoots,
        DiscoverSymbols,
        WorkDisasmQueue,
        CollectStrings,
        ExpandUnknownData,
        MarkPadding,

#if DEBUG
        ValidateNames,
        ValidateBodyReferences,
#endif

        Max
    }

    public interface IFileAnalyzerProgress : ILocatorProgress
    {
        void NotifyPhase(FileAnalyzerProgressPhase phase);

        void PhaseComplete(FileAnalyzerProgressPhase phase, long elapsed);
    }
}
