using PESpy.View;

namespace PESpy
{
    public class NB05Data : IValue, IViewable
    {
        public int LfoDir { get; }

        public CodeViewSig Sig { get; }

        public int LfoBase { get; }

        private readonly OMFDirHeader dirHeader;

        public ref readonly OMFDirHeader DirHeader => ref dirHeader;

        public OMFDirEntry[] DirEntries { get; }

        public object?[] TableData { get; }

        public int Offset { get; }

        internal NB05Data(int offset, CodeViewSig sig, int lfoBase, int lfoDir, in OMFDirHeader dirHeader, OMFDirEntry[] dirEntries, object?[] tableData)
        {
            Offset = offset;
            LfoDir = lfoDir;
            Sig = sig;
            LfoBase = lfoBase;
            this.dirHeader = dirHeader;
            DirEntries = dirEntries;
            TableData = tableData;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            //The merger will wrap this all up in a region

            writer.WriteGlobal(Offset, Sig, sizeof(int), ViewKind.Value);
            writer.WriteGlobalField(Offset + 4, "lfoDir", LfoDir, sizeof(int));

            //Data comes before the header

            //foreach (var item in TableData)
            //{
            //    if (item == null)
            //        continue;
            //
            //    if (item is IValue v)
            //        writer.WriteGlobal((IViewable) v);
            //    else
            //        throw new NotImplementedException();
            //}

            writer.WriteGlobal(DirHeader);
            writer.WriteGlobal(DirEntries);
        }
    }
}
