using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class DBI : IValue, IViewable
        {
            private NewDBIHdr dbiHdr;
            public ref readonly NewDBIHdr DbiHdr => ref dbiHdr;

            public Modi60[]? Modules { get; }

            public SectionContribsV60? SectionContribs { get; }
            
            public OMFSegMap? SectionMap { get; }

            public FileInfo? FileInfo { get; }

            public NMT? NameTableEC { get; } //MODI has an ECInfo, so we don't call this ECInfo too to avoid confusion

            public DbgDataHdr? DbgHdr { get; }

            public int Offset { get; }

            internal DBI(in MemoryChunk chunk)
            {
                Offset = chunk.AbsoluteOffset;

                dbiHdr = new NewDBIHdr(chunk);

                var dataChunk = chunk.Slice(NewDBIHdr.StructSize);

                //Module Substream
                if (dbiHdr.cbGpModi > 0)
                {
                    var remaining = dbiHdr.cbGpModi;

                    var modules = new List<Modi60>();

                    var moduleChunk = dataChunk;

                    while (remaining > 0)
                    {
                        var module = new Modi60(moduleChunk, out var read);
                        modules.Add(module);
                        remaining -= read;
                        Debug.Assert(remaining >= 0);
                        moduleChunk = moduleChunk.Slice(read);
                    }

                    Modules = modules.ToArray();

                    //Just in case there's any junk at the end of the module stream we didn't read,
                    //we want to evenly move on from the end of the module substream, so we slice
                    //based on dataChunk, not moduleCHunk
                    dataChunk = dataChunk.Slice(dbiHdr.cbGpModi);
                }

                //Section Contributions Substream
                if (dbiHdr.cbSC > 0)
                {
                    var version = (DBISCImpv) dataChunk.PeekUInt32(0);

                    if (version == DBISCImpv.DBISCImpvV60)
                    {
                        var size = dbiHdr.cbSC - 4; //Skip over the version field

                        SectionContribs = new SectionContribsV60(dataChunk.Slice(4), version, size);
                    }
                    else
                    {
                        throw new NotImplementedException($"Don't know how to handle {nameof(DBISCImpv)} '{version}'");
                    }

                    dataChunk = dataChunk.Slice(dbiHdr.cbSC);
                }

                //Section Map
                if (dbiHdr.cbSecMap > 0)
                {
                    SectionMap = new OMFSegMap(dataChunk);
                    dataChunk = dataChunk.Slice(dbiHdr.cbSecMap);
                }

                //File Info
                if (dbiHdr.cbFileInfo > 0)
                {
                    FileInfo = new FileInfo(dataChunk);
                    dataChunk = dataChunk.Slice(dbiHdr.cbFileInfo);
                }

                //Type Server Map
                if (dbiHdr.cbTSMap > 0)
                {
                    /* I believe this contains a collection of TSM structs. The only place
                     * TSM items are added seems to be in DBI1::AddTypeServer, which is #if 0'd
                     * out in microsoft-pdb. The method is not present in VS2015+. Furthermore,
                     * Mod1::GetTmts says that support for creating type servers was removed
                     * "a long time ago". It would be good to be able to show the data that
                     * existed here none-the-less, but for now we will just skip it */
                    dataChunk = dataChunk.Slice(dbiHdr.cbTSMap);
                }

                //Edit and Continue Info
                if (dbiHdr.cbECInfo > 0)
                {
                    NameTableEC = new NMT(dataChunk);
                    dataChunk = dataChunk.Slice(dbiHdr.cbECInfo);
                }

                //Optional Debug Header
                if (dbiHdr.cbDbgHdr > 0)
                {
                    DbgHdr = new DbgDataHdr(dataChunk, dbiHdr.cbDbgHdr);
                }
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                writer.WriteGlobal(DbiHdr);
                writer.WriteGlobal(Modules);
                writer.WriteGlobal(SectionContribs);
                writer.WriteGlobal(SectionMap);
                writer.WriteGlobal(FileInfo);
                writer.WriteGlobal(NameTableEC);
                writer.WriteGlobal(DbgHdr);
            }
        }
    }
}
