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

                var headerMetadata = new HeaderView(0, sizeOfHeaders, BuildSection(0, sizeOfHeaders, v => v, v => v), viewWriter);
                results.Add(headerMetadata);

                var data = BuildSection(sizeOfHeaders, startOfOverlay, v => v, v => v);
                results.Add(new SectionView(startOfOverlay, "Code", data, viewWriter, startOfOverlay - sizeOfHeaders));

                var length = byteViewProvider.FileOrSectionLength;

                if (startOfOverlay < length)
                {
                    var codeViewData = dosFile.CodeViewData;

                    if (codeViewData != null)
                    {
                        switch (codeViewData.Signature)
                        {
                            case CodeViewSig.DNRB:
                                var dnrb = (DNRBData) codeViewData;

                                CreateOMFRegion(dnrb.Offset, dnrb.Length, dnrb.Signature);
                                break;

                            default:
                                var nb02 = (NB02Data) codeViewData;

                                CreateOMFRegion(nb02.Offset, nb02.LfoBase, nb02.Signature);
                                break;
                        }
                    }

                    ProcessOverlay(startOfOverlay, length, ref this, ref results);
                }

                return results.ToArray();
            }
            finally
            {
                results.Dispose();
            }
        }
    }
}
