using PESpy.View;

namespace PESpy.PDB
{
    public interface IDBIHdr : IValue, IViewable
    {
        public SN snGSSyms { get; }
        public SN snPSSyms { get; }
        public SN snSymRecs { get; }
        public int cbGpModi { get; }
        public int cbSC { get; }
        public int cbSecMap { get; }
        public int cbFileInfo { get; }

        int StructSize { get; }
    }
}
