namespace PESpy.View
{
    internal class NEFileAnalyzer : FileAnalyzer
    {
        private readonly NEFile _neFile;

        internal NEFileAnalyzer(
            NEFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _neFile = fileAccessor.NEFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var neFile = (NEFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(neFile),
                neFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteCode();

        protected override void DiscoverCodeRoots()
        {
            //Not yet implemented
        }
    }
}
