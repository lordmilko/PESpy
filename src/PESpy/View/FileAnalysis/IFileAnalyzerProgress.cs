namespace PESpy.View
{
    public enum FileAnalyzerProgressPhase
    {
        DiscoverGlobals = 1,
        LocateSymbols,
        DiscoverExceptionData,
        DiscoverCodeRoots,
        ProcessSymbols,
        WorkDisasmQueue,
        CollectStrings,
        ExpandUnknownData,
        MarkPadding,
        MarkLargeAreas,

        Max
    }

    public interface IFileAnalyzerProgress : ILocatorProgress
    {
        void NotifyPhase(FileAnalyzerProgressPhase phase);

        void PhaseComplete(FileAnalyzerProgressPhase phase, long elapsed);
    }
}
