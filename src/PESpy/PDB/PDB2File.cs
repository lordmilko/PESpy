using System.Diagnostics;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    class PDB2FileDebugView : PDBFileDebugView
    {
        private PDB2File pdbFile;

        public MsfHdr MsfHeader => pdbFile.MsfHeader;

        internal PDB2FileDebugView(PDB2File pdbFile) : base(pdbFile)
        {
            this.pdbFile = pdbFile;
        }
    }

    /// <summary>
    /// Represents a CodeView Program Database v2 (PDB) file.
    /// </summary>
    [DebuggerTypeProxy(typeof(PDB2FileDebugView))]
    public class PDB2File : PDBFile
    {
        private MsfHdr msfHeader;

        public ref readonly MsfHdr MsfHeader => ref msfHeader;

        protected internal override int PageSize => msfHeader.PageSize;

        protected internal override int ActiveFpmPageNo => (int) msfHeader.FpmPageNo;

        protected internal override int NumPages => msfHeader.NumPages;

        private PN[] _streamTablePageListArray;

        //Cache the array so we can lookup the same block we create below in PDBFileAccessor.GetMemoryChunkFromAddress
        internal PN[] StreamTablePageListArray
        {
            get
            {
                if (_streamTablePageListArray == null)
                {
                    var pageList = msfHeader.StreamTablePageList;

                    var arr = new PN[pageList.Length];

                    for (var i = 0; i < pageList.Length; i++)
                        arr[i] = pageList[i];

                    _streamTablePageListArray = arr;
                }

                return _streamTablePageListArray;
            }
        }

        internal PDB2File(string fileName, in MemoryMappedFileHolder mmf, string name = null) : base(fileName, mmf, PDBFileKind.V2, name)
        {
#if STRESS_TEST
            _ = PreviousStreamTable;
            _ = PDB;
            _ = TPI;
            _ = DBI;

            _ = NameMap;
#endif
        }

        internal override IStreamTable CreateStreamTable(in MemoryChunk chunk, int pageSize) => new MsfHdr.StreamTable(chunk, pageSize);

        protected override void ReadHeaders()
        {
            //Initialize the MSF

            var globalChunk = new MemoryChunk(globalBlock, 0);

            //The PDB begins with the MSF Header
            msfHeader = new MsfHdr(globalChunk);
            globalBlock.pageSize = msfHeader.PageSize;

            //PDBs have two Free Page Maps. In V2 PDBs, their locations are determined based on the page size

            var msfParms = MSFParms.FromPageSize(PageSize);

            //The first FPM is always page 1
            fpm0 = new FPM(1, msfHeader.PageSize, msfHeader.NumPages, globalBlock, msfParms);

            //The page of the second FPM depends on our page size
            var secondFPM = msfParms.Fpm1PageNo;

            fpm1 = new FPM(secondFPM, msfHeader.PageSize, msfHeader.NumPages, globalBlock, msfParms);

            /* In PDB v7, the MSF Header describes the location of a set of pages that can be read to find the location of the stream table
             * But in PDB v2, the MSF Header contains the set of pages of the stream table directly. More than that however, the actual
             * structure of the stream table is also different.
             *
             * In v7, you have the following layout
             *     int NumStreams
             *     int[] StreamSizes
             *     PN[][] StreamPages
             *
             * In v2, you instead have this layout instead
             *    int NumStreams
             *    SI_PERSIST[] Streams
             *    PN[][] StreamPages
             *
             * Each SI_PERSIST contains its ByteCount, which is what you get from int[] StreamSizes in v7
             */

            StreamTable = new MsfHdr.StreamTable(
                globalBlock.SlicePaged(StreamTablePageListArray, msfHeader.StreamTableSizeInfo.ByteCount), //Need to ToArray, because the 16-bit header gets upsized to 32-bit PN values, which must be stored in an array
                msfHeader.PageSize
            );
        }

        protected override void WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(MsfHeader);

            //We do not write the FPM; these raw bytes are automatically collected during merging
            //under the FPM0 and FPM1 regions

            WriteMsfStreamViews(writer);
        }
    }
}
