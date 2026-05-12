namespace PESpy.View
{
    internal class SYMFileAnalyzer : FileAnalyzer
    {
        private readonly SYMFile _symFile;

        public SYMFileAnalyzer(
            SYMFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _symFile = fileAccessor.SYMFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var symFile = (SYMFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(symFile),
                symFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteData();
    }
}
