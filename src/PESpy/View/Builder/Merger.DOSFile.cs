namespace PESpy.View.Builder
{
    internal ref partial struct Merger
    {
        internal IView[] MergeDOS()
        {
            //todo: not sure if theres actually any sections we need to take into consideration

            var results = new ValueList<IView>();

            var dosFile = (DOSFile) file;

            try
            {
                var dosHeader = dosFile.DosHeader;

                var sizeOfHeaders = dosFile.SizeOfHeaders;
                var startOfOverlay = dosFile.StartOfOverlay;

                var headerMetadata = new HeaderView(0, sizeOfHeaders, BuildSection(0, sizeOfHeaders, v => v, v => v), viewWriter);
                results.Add(headerMetadata);

                var data = BuildSection(sizeOfHeaders, startOfOverlay, v => v, v => v);
                results.Add(new SectionView(sizeOfHeaders, "Code", data, viewWriter, startOfOverlay - sizeOfHeaders));

                var length = (int) byteViewProvider.FileOrSectionLength;

                if (startOfOverlay < length)
                {
                    var codeViewData = dosFile.CodeViewData;

                    if (codeViewData != null)
                    {
                        switch (codeViewData.Signature)
                        {
                            case CodeViewSig.DNRB:
                                var dnrb = (DNRBData) codeViewData;

                                CreateOMFRegion((int) dnrb.Offset, dnrb.Length, dnrb.Signature);
                                break;

                            case CodeViewSig.NB00:
                            case CodeViewSig.NB01:
                            case CodeViewSig.NB02:
                                var nb02 = (NB02Data) codeViewData;

                                CreateOMFRegion((int) nb02.Offset, nb02.LfoBase, nb02.Signature);
                                break;

                            default:
                                var nb05 = (NB05Data) codeViewData;

                                CreateOMFRegion((int) nb05.Offset, nb05.LfoBase, nb05.Signature);
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
