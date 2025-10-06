namespace PESpy.View.Builder
{
    internal ref partial struct Merger
    {
        internal IView[] MergeDOS()
        {
            //todo: not sure if theres actually any sections we need to take into consideration

            var results = new PooledList<IView>();

            var dosFile = (DOSFile) file;

            try
            {
                var dosHeader = dosFile.DosHeader;

                var sizeOfHeaders = dosFile.SizeOfHeaders;
                var startOfOverlay = dosFile.StartOfOverlay;

                var headerMetadata = new HeaderView(sizeOfHeaders, BuildSection(0, sizeOfHeaders, v => v, v => v));
                results.Add(headerMetadata);

                var data = BuildSection(sizeOfHeaders, startOfOverlay, v => v, v => v);
                results.Add(new SectionView(startOfOverlay, "Code", data, startOfOverlay - sizeOfHeaders));

                var length = (int) extension.GetInputLength();

                if (startOfOverlay < length)
                    ProcessOverlay(startOfOverlay, length, ref this, ref results);

                return results.ToArray();
            }
            finally
            {
                results.Dispose();
            }
        }
    }
}
