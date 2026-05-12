namespace PESpy.View
{
    internal class OMFDBGFileAnalyzer : FileAnalyzer
    {
        private readonly OMFDBGFile _omfDbgFile;

        internal OMFDBGFileAnalyzer(
            OMFDBGFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _omfDbgFile = fileAccessor.OMFDBGFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var omfDbgFile = (OMFDBGFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(omfDbgFile),
                omfDbgFile.CreateByteViewProvider(_fileAccessor),
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
