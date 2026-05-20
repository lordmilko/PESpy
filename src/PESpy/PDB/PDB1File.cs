using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    internal class PDB1FileDebugView
    {
        private PDB1File pdbFile;

        public PDBFileKind PDBKind => pdbFile.PDBKind;

        public string Name => pdbFile.Name;

        public string FileName => pdbFile.FileName;

        public FileKind Kind => pdbFile.Kind;

        public long Length => pdbFile.Length;

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

                    using var recs = new ValueList<C8Rec>();

                    //Next we appear to have a list of C8REC records. A C8REC consists of a USHORT hash and a BYTE[] buf.
                    //The buf is the TYPTYPE. Per NT 4 TPI1::fLoadOldPDB
                    while (read < hdr.cb)
                    {
                        C8Rec rec = (C8REC*) (globalChunk.Pointer + read);

                        recs.Add(rec);

                        read += rec.StructSize;
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

        private TpiHashLookup tpiHashLookup;

        internal PDB1File(string fileName, in MemoryMappedFileHolder mmf, string name = null) : base(fileName, mmf, PDBFileKind.V1, name)
        {
        }

        protected override void ReadHeaders()
        {
            //PDB v1 files do not use MSF; they merely contain type information

            var globalChunk = new MemoryChunk(globalBlock, 0);

            hdr = new OHDR(globalChunk);
        }

        #region Type Lookup

        public override TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            if (!TryGetTypTypeFromIndex(typeIndex, out var typType))
                throw new InvalidOperationException($"Failed to resolve type index '{typeIndex}'");

            return typType;
        }

        public override TypType GetTypTypeFromIndex(CV_ItemId typeIndex) => throw new NotSupportedException();

        public override bool TryGetTypTypeFromIndex(CV_typ_t typeIndex, out TypType typType)
        {
            var i = typeIndex - Hdr.tiMin;

            var records = Records;

            if (i >= records.Length)
            {
                typType = default;
                return false;
            }

            typType = records[i].type;
            return true;
        }

        public override bool TryGetTypTypeFromIndex(CV_ItemId typeIndex, out TypType typType)
        {
            typType = default;
            return false;
        }

        public bool TryGetIndexFromTypType(TypType typType, out CV_typ_t typeIndex)
        {
            if (tpiHashLookup == null)
            {
                lock (this)
                {
                    if (tpiHashLookup == null)
                        InitializeHashLookup();
                }
            }

            var hash = Hasher.oldHashPbCb((TYPTYPE*) typType);

            var bucket = GetBucket(hash);

            var records = Records;
            var tiMin = Hdr.tiMin;

            foreach (var entry in bucket)
            {
                var candidate = records[entry - tiMin];

                //I would expect that implicitly the hash should be equal (since unlike with TpiHash,
                //there isn't really a concept of "hashPrecFull". There's only one hash, the hash of
                //the entire record
                if (candidate.hash == hash && candidate.type.AsSpan().SequenceEqual(typType.AsSpan()))
                {
                    typeIndex = entry;
                    return true;
                }
            }

            typeIndex = default;
            return false;
        }

        private void InitializeHashLookup()
        {
            var tiMin = Hdr.tiMin;
            var tiMac = Hdr.tiMac;

            var records = Records;

            //An array of lists
            var buckets = new Dictionary<uint, int>();

            //Count how many collisions in each bucket we have
            foreach (var record in records)
            {
                var hash = record.hash;

                if (!buckets.TryGetValue(hash, out var bucket))
                    buckets[hash] = 1;
                else
                    buckets[hash] = bucket + 1;
            }

            //Now write all the values in
            var tpiHashLookup = new TpiHashLookup(buckets, tiMac - tiMin);

            for (var i = 0; i < Records.Length; i++)
            {
                var record = Records[i];

                var hash = record.hash;

                var handle = tpiHashLookup[hash];

                var remaining = buckets[hash];
                var pos = handle.Length - remaining;

                tpiHashLookup.GetSpan(handle)[pos] = i + tiMin;

                buckets[hash] = remaining - 1;
            }

            this.tpiHashLookup = tpiHashLookup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<CV_typ_t> GetBucket(uint hash)
        {
            if (!tpiHashLookup.TryGetValue(hash, out var handle))
                return default;

            return tpiHashLookup.GetSpan(handle);
        }

        #endregion

        protected override unsafe void GetRawHeaderData(out byte* pointer, out int length)
        {
            pointer = mmf.Address;
            length = (int) mmf.Length;
        }

        internal override bool TryGetValueChunkFromPhysicalOffset(int offset, out MemoryChunk chunk)
        {
            if (offset < Length)
            {
                chunk = new MemoryChunk(globalBlock, offset);
                return true;
            }

            chunk = default;
            return false;
        }

        protected override void WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(hdr);

            writer.UnmanagedOffset = OHDR.StructSize;

            try
            {
                foreach (var record in Records)
                {
                    writer.WriteGlobal(record);
                    writer.UnmanagedOffset += record.StructSize;
                }
            }
            finally
            {
                writer.UnmanagedOffset = 0;
            }
        }
    }
}
