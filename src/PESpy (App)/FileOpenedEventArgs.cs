namespace PESpy
{
    public enum FileOpenedEventKind
    {
        //Context is an IFile
        OpenFile = 1,

        //Context is a FileAccessor
        OpenAccessor,

        AnalysisComplete
    }

    public class FileOpenedEventArgs
    {
        public FileOpenedEventKind EventKind { get; }

        public IFile? File { get; }

        public FileOpenedEventArgs(FileOpenedEventKind eventKind, IFile? file)
        {
            EventKind = eventKind;
            File = file;
        }
    }
}
