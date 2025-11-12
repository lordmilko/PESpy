using System;
using System.Diagnostics;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    internal class PDB1FileDebugView
    {
        private PDB1File pdbFile;

        public OHDR Hdr => pdbFile.Hdr;

        public C8Rec[] Records => pdbFile.Records;

        internal PDB1FileDebugView(PDB1File pdbFile)
        {
            this.pdbFile = pdbFile;
        }
    }

    /// <summary>
    /// Represents a CodeView Program Database v1 (PDB) file.<para/>
    /// Unlike PDB v2+, PDB v1 does not implement MSF and only contains type records.
    /// </summary>
    [DebuggerTypeProxy(typeof(PDB1FileDebugView))]
    public unsafe class PDB1File : PDBFile
    {
        private OHDR hdr;

        public ref readonly OHDR Hdr => ref hdr;

        private C8Rec[]? records;

        public C8Rec[] Records
        {
            get
            {
                if (records == null)
                {
                    //The rest of the file after the OHDR contains C8REC items

                    var globalChunk = new MemoryChunk(globalBlock, OHDR.StructSize);

                    var read = 0;

                    using var recs = new PooledList<C8Rec>();

                    //Next we appear to have a list of C8REC records. A C8REC consists of a USHORT hash and a BYTE[] buf.
                    //The buf is the TYPTYPE. Per NT 4 TPI1::fLoadOldPDB
                    while (read < hdr.cb)
                    {
                        C8Rec rec = (C8REC*) (globalChunk.Pointer + read);

                        recs.Add(rec);

                        read += rec.type.len + 4; //len + sizeof(hash) + sizeof(len)
                    }

                    records = recs.ToArray();
                }

                return records;
            }
        }

        protected internal override int PageSize => -1;

        protected internal override int ActiveFpmPageNo => -1;

        protected internal override int NumPages => -1;

        internal override IStreamTable CreateStreamTable(in MemoryChunk chunk, int pageSize) => throw new NotSupportedException();

        internal PDB1File(string fileName, in MemoryMappedFileHolder mmf) : base(fileName, mmf, PDBFileKind.V1)
        {
        }

        protected override void ReadHeaders()
        {
            //PDB v1 files do not use MSF; they merely contain type information

            var globalChunk = new MemoryChunk(globalBlock, 0);

            hdr = new OHDR(globalChunk);
        }

        protected override void WriteGlobals(ViewWriter writer)
        {
            throw new System.NotImplementedException();
        }
    }
}
