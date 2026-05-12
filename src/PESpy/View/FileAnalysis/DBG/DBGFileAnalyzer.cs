namespace PESpy.View
{
    internal class DBGFileAnalyzer : FileAnalyzer
    {
        private readonly DBGFile _dbgFile;

        internal DBGFileAnalyzer(
            DBGFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _dbgFile = fileAccessor.DBGFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var dbgFile = (DBGFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(dbgFile),
                dbgFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteData();

        protected override void MarkRegions()
        {
            CreateOMFRegion(_dbgFile.DebugTable);
        }
    }
}
