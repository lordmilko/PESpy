using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using ClrDebug;
using ClrDebug.PDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.PDB;
using PESpy.View;
using DBI1 = ClrDebug.PDB.DBI1;
using PDB1 = ClrDebug.PDB.PDB1;
using SN = PESpy.PDB.SN;
using static PESpy.Tests.PEFileTests;

namespace PESpy.Tests
{
    [TestClass]
    public class PDBTests
    {
        #region MSF

        [TestMethod]
        public void PDB_Minimal()
        {
            //Create a minimal PDB using mspdbcore and confirm we can read it
            TestMsfView(
                null,
                v => v.VerifyLogicalRegion(name: "0 | Master Index",           offset: 0, size: 0x400,
                    c => c.VerifyStruct(name: "BIGMSF_HDR", offset: 0, size: 56,
                        c1 => c1.VerifyField(name: "szMagic", value: "Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0"),
                        c1 => c1.VerifyField(name: "cbPg", value: 1024),
                        c1 => c1.VerifyField(name: "pnFpm", value: (PN) 1),
                        c1 => c1.VerifyField(name: "pnMac", value: 11),
                        c1 => c1.VerifyStructField(name: "siSt", type: "SI_PERSIST", offset: 44, size: 8,
                            c2 => c2.VerifyField(name: "cb", value: 4),
                            c2 => c2.VerifyField(name: "mpspnpn", value: 0)
                        ),
                        c1 => c1.VerifyField(name: "mpspnpnSt", new[] { (PN) 4 })
                    ),
                    c => c.VerifyByteBlob(offset: 56, value: new byte[968])
                ),
                v => v.VerifyLogicalRegion(name: "1 | FPM 0 (1/1) (Active)",   offset: 0x400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x400, value: MakeFPM(0xE0, 0xFF))
                ),
                v => v.VerifyLogicalRegion(name: "2 | FPM 1 (1/1) (Inactive)", offset: 0x800, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x800, value: MakeFPM(0, 0))
                ),
                v => v.VerifyLogicalRegion(name: "3 | Stream Table (1/1)",     offset: 0xC00, size: 0x400,
                    c => c.VerifyStruct(name: "Stream Table", offset: 0xC00, size: 4,
                        c1 => c1.VerifyField(name: "NumStreams", value: 0)

                        //The other members are not present when NumStreams is 0
                    ),
                    c => c.VerifyByteBlob(offset: 0xC04, new byte[1020])
                ),
                v => v.VerifyLogicalRegion(name: "4 | Stream Table Page List (1/1)", offset: 0x1000, size: 0x400,
                    c => c.VerifyValue(offset: 0x1000, value: (PN) 3),
                    c => c.VerifyByteBlob(offset: 0x1004, new byte[1020])
                ),

                v => v.VerifyLogicalRegion(name: "5 | Free",                   offset: 0x1400, size: 0x400,
                    c => c.VerifyByteBlob(0x1400, new byte[1024])
                ),

                v => v.VerifyLogicalRegion(name: "6 | Free",                   offset: 0x1800, size: 0x400,
                    c => c.VerifyByteBlob(0x1800, new byte[1024])
                ),

                v => v.VerifyLogicalRegion(name: "7 | Free",                   offset: 0x1C00, size: 0x400,
                    c => c.VerifyByteBlob(0x1C00, new byte[1024])
                ),

                v => v.VerifyLogicalRegion(name: "8 | Free",                   offset: 0x2000, size: 0x400,
                    c => c.VerifyByteBlob(0x2000, new byte[1024])
                ),

                v => v.VerifyLogicalRegion(name: "9 | Free",                   offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(0x2400, new byte[1024])
                ),

                v => v.VerifyLogicalRegion(name: "10 | Free",                  offset: 0x2800, size: 0x400,
                    c => c.VerifyByteBlob(0x2800, new byte[1024])
                )
            );
        }

        [TestMethod]
        public void PDB_WithEmptyStream()
        {
            //Create a minimal PDB using mspdbcore and add a single user stream to it

            TestMsfView(
                msf =>
                {
                    //"Replace" is how you "add"
                    msf.ReplaceStream(SN.UserMin, IntPtr.Zero, 0);
                    msf.Commit();
                },
                v => v.VerifyLogicalRegion(name: "0 | Master Index", offset: 0, size: 0x400,
                    c => c.VerifyStruct(name: "BIGMSF_HDR", offset: 0, size: 56,
                        c1 => c1.VerifyField(name: "szMagic", value: "Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0"),
                        c1 => c1.VerifyField(name: "cbPg", value: 1024),
                        c1 => c1.VerifyField(name: "pnFpm", value: (PN) 2), //After our commit, FPM 2 is now active
                        c1 => c1.VerifyField(name: "pnMac", value: 11),
                        c1 => c1.VerifyStructField(name: "siSt", type: "SI_PERSIST", offset: 44, size: 8,
                            c2 => c2.VerifyField(name: "cb", value: 16), //After our commit, this changed from 4 -> 16
                            c2 => c2.VerifyField(name: "mpspnpn", value: 0)
                        ),
                        c1 => c1.VerifyField(name: "mpspnpnSt", (PN) 6) //After our commit, changed from 4 -> 6
                    ),
                    c => c.VerifyByteBlob(offset: 56, value: new byte[968])
                ),
                v => v.VerifyLogicalRegion(name: "1 | FPM 0 (1/1) (Inactive)", offset: 0x400, size: 0x400, //After our commit, FPM 1 is now inactive
                    c => c.VerifyByteBlob(offset: 0x400, value: MakeFPM(0xE0, 0xFF))
                ),
                v => v.VerifyLogicalRegion(name: "2 | FPM 1 (1/1) (Active)", offset: 0x800, size: 0x400, //After our commit, FPM 2 is now active
                    c => c.VerifyByteBlob(offset: 0x800, value: MakeFPM(0x98, 0xFF))
                ),

                //After our commit:
                //- the Stream Table           has moved from Page 3 -> 5
                //- the Stream Table Page List has moved from page 4 -> 6
                //- the previous Stream Table is now stored in Page 3
                //- page 4 is now free

                v => v.VerifyLogicalRegion(name: "3 | Fake Free / 'Previous Stream Table (0)' (1/1)", offset: 0xC00, size: 0x400,
                    c => c.VerifyStruct(name: "Stream Table", offset: 0xC00, size: 4,
                        c1 => c1.VerifyField(name: "NumStreams", value: 0)

                        //The other members are not present when NumStreams is 0
                    ),
                    c => c.VerifyByteBlob(offset: 0xC04, new byte[1020])
                ),

                v => v.VerifyLogicalRegionIgnoreChildren(name: "4 | Free", offset: 0x1000, size: 0x400),

                v => v.VerifyLogicalRegion(name: "5 | Stream Table (1/1)", offset: 0x1400, size: 0x400,
                    c => c.VerifyStruct(name: "Stream Table", offset: 0x1400, size: 16,
                        c1 => c1.VerifyField(name: "NumStreams", value: 2),
                        c1 => c1.VerifyField(name: "StreamSizes", new[] {4, 0}),
                        c1 => c1.VerifyField(name: "PageList (0)", new PN[] {3}) //Each PN[] is written as a PageList field
                    ),
                    c => c.VerifyByteBlob(offset: 0x1410, new byte[1008]) //Padding
                ),

                v => v.VerifyLogicalRegion(name: "6 | Stream Table Page List", offset: 0x1800, size: 0x400,
                    c => c.VerifyLogicalRegion(name: "SI Pages", offset: 0x1800, size: 4,
                        c1 => c1.VerifyValue(offset: 0x1800, value: (PN) 5)
                    ),
                    c => c.VerifyByteBlob(offset: 0x1804, new byte[1020])
                ),

                v => v.VerifyLogicalRegionIgnoreChildren(name: "7 | Free", offset: 0x1C00, size: 0x400),
                v => v.VerifyLogicalRegionIgnoreChildren(name: "8 | Free", offset: 0x2000, size: 0x400),
                v => v.VerifyLogicalRegionIgnoreChildren(name: "9 | Free", offset: 0x2400, size: 0x400),
                v => v.VerifyLogicalRegionIgnoreChildren(name: "10 | Free", offset: 0x2800, size: 0x400)
            );
        }

        [TestMethod]
        public unsafe void PDB_FPM_StressTest()
        {
            //The FPM allocates 8x as many pages as its supposed to. Stress test creating a large PDB and reading which pages are associated with the FPM

            void ConfigureMsf(MSF msf)
            {
                //Don't use snPDB; we force load streams in Debug mode for testing purposes,
                //and what we're essentially doing here is creating a stream full of garbage

                //StrmTbl's ctor dets the size of mpsnsi to 5 (which I guess would cover streams 0-4). GetFreeSn allocates the next available SN

                //A stream that doesn't exist should return cbNil (-1). We're just doing this check to demonstrate how interacting with MSF works
                Assert.AreEqual(-1, msf.GetCbStream(5));

                var sn = msf.GetFreeSn();
                Assert.AreEqual(5, sn);

                //The stream should now be in the stream table
                Assert.AreEqual(0, msf.GetCbStream(sn));

                //mspdbcore seems to have a strange algorithm for determining PDB growth. 8135 gives 8187 pages while 8136 gives 8195.
                //As soon as there's more than 8x pageSize pages (which is 8x pageSize x pageSize bytes) (i.e. 8192) there will be two FPM pages
                //1024 pages gives 1043.
                var cbBuf = 1024 * 1024;

                var pvBuf = Marshal.AllocHGlobal(cbBuf);

                for (var i = 0; i < cbBuf; i++)
                    *(byte*) (pvBuf + i) = 0xCD;

                //Add a bunch of junk to the stream. This should cause the FPM to add another page
                msf.AppendStream(sn, pvBuf, cbBuf);

                Marshal.FreeHGlobal(pvBuf);

                //Save the changes
                msf.Commit();
            }

            var actions = new Action<IView>[1043];

            for (var i = 0; i < actions.Length; i++)
                actions[i] = v => { };

            //We just want to verify the FPM
            actions[1] = v => v.VerifyLogicalRegionIgnoreChildren(name: "1 | FPM 0 (1/2) (Inactive)", offset: 0x400, size: 0x400);
            actions[2] = v => v.VerifyLogicalRegionIgnoreChildren(name: "2 | FPM 1 (1/2) (Active)", offset: 0x800, size: 0x400);

            actions[1025] = v => v.VerifyLogicalRegionIgnoreChildren(name: "1025 | FPM 0 (2/2) (Inactive)", offset: 0x100400, size: 0x400);
            actions[1026] = v => v.VerifyLogicalRegionIgnoreChildren(name: "1026 | FPM 1 (2/2) (Active)", offset: 0x100800, size: 0x400);

            TestMsfView(
                ConfigureMsf,
                actions,
                views =>
                {
                    var fpm0 = new[]
                    {
                        (ByteBlobView) ((LogicalRegionView) views[1]).Children[0],
                        (ByteBlobView) ((LogicalRegionView) views[1025]).Children[0]
                    };
                    var fpm1 = new[]
                    {
                        (ByteBlobView) ((LogicalRegionView) views[2]).Children[0],
                        (ByteBlobView) ((LogicalRegionView) views[1026]).Children[0]
                    };

                    //FPM 0 is not active, so it should still be E0 (i.e. only the first 5 pages were set in the first commit)
                    Assert.AreEqual(0xE0, fpm0[0].Bytes[0]);
                    Assert.IsTrue(fpm0[0].Bytes.Skip(1).All(v => v == 0xFF));
                    Assert.IsTrue(fpm0[1].Bytes.All(v => v == 0));

                    //FPM 1
                    Assert.AreEqual(0x18, fpm1[0].Bytes[0]);
                    Assert.IsTrue(fpm1[0].Bytes.Skip(1).Take(127).All(v => v == 0));
                    Assert.AreEqual(0xE0, fpm1[0].Bytes[129]);
                    Assert.IsTrue(fpm1[0].Bytes.Skip(130).All(v => v == 0xFF));

                    Assert.IsTrue(fpm1[1].Bytes.All(v => v == 0));
                }
            );
        }

        [TestMethod]
        public unsafe void PDB_StreamNumber_StressTest()
        {
            //Creating 75 streams with (1024 * 1024) byte pages should cause us to require 1204 bytes to store the 301
            //pages that the stream table itself spans, thus causing mpspnpnSt to contain more than one value

            void ConfigureMsf(MSF msf)
            {
                //For some reason, even though we're starting from Stream 5, data is still getting written into Stream 1,
                //so we need to explicitly create and zero it out
                msf.ReplaceStream(SN.PDB, IntPtr.Zero, 0);
                msf.ReplaceStream(SN.TPI, IntPtr.Zero, 0);
                msf.ReplaceStream(SN.DBI, IntPtr.Zero, 0);
                msf.ReplaceStream(SN.IPI, IntPtr.Zero, 0);

                var cbBuf = 1024 * 1024;

                var pvBuf = Marshal.AllocHGlobal(cbBuf);

                for (var i = 0; i < cbBuf; i++)
                    *(byte*) (pvBuf + i) = 0xCD;

                for (var i = 0; i < 75; i++)
                {
                    var sn = msf.GetFreeSn();

                    msf.AppendStream(sn, pvBuf, cbBuf);
                }

                Marshal.FreeHGlobal(pvBuf);

                msf.Commit();
            }

            TestMsfView(
                ConfigureMsf,
                WithIgnores(
                    v => v.VerifyLogicalRegion(name: "0 | Master Index", offset: 0, size: 1024,
                        c => c.VerifyStruct(name: "BIGMSF_HDR", offset: 0, size: 60,
                            c1 => c1.VerifyField(name: "szMagic", value: "Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0"),
                            c1 => c1.VerifyField(name: "cbPg", value: 1024),
                            c1 => c1.VerifyField(name: "pnFpm", value: (PN) 2),
                            c1 => c1.VerifyField(name: "pnMac", value: 77259),
                            c1 => c1.VerifyStructField(name: "siSt", type: "SI_PERSIST", offset: 44, size: 8,
                                c2 => c2.VerifyField(name: "cb", value: 307528),
                                c2 => c2.VerifyField(name: "mpspnpn", value: 0)
                            ),
                            c1 => c1.VerifyField(name: "mpspnpnSt", new[] { (PN) 77256, (PN) 77257 })
                        ),
                        c => c.VerifyByteBlob(offset: 0x3C, value: new byte[964])
                    ),
                    after: 77258
                )
            );
        }

        private void TestMsfView(Action<MSF> configureMsf, params Action<IView>[] verify) =>
            TestMsfView(configureMsf, verify, null);

        private void TestMsfView(
            Action<MSF> configureMsf,
            Action<IView>[] verify,
            Action<IView[]> verifyExtra)
        {
            using var tempFile = TempFile.New();

            using (var vsPdb = VsPDB.CreateMSF(tempFile))
            {
                configureMsf?.Invoke(vsPdb.MSF);
            }

            TestViewInternal(tempFile, verify, verifyExtra);
        }

        private static byte[] MakeFPM(byte first, byte others)
        {
            var results = new byte[1024];

            results[0] = first;

            for (var i = 1; i < 1024; i++)
                results[i] = others;

            return results;
        }

        #endregion
        #region PDB

        [TestMethod]
        public void PDB_Minimal_PDBStream()
        {
            //A minimal PDB that has a PDB Stream

            TestPDBView(
                null,
                WithIgnores(
                    before: 5,
                    action: new Action<IView>[]
                    {
                        //PDB Stream
                        v => v.VerifyLogicalRegion(name: "5 | 'PDB (1)' (1/1)", offset: 0x1400, size: 0x400,
                            c => c.VerifyStruct(name: "PDBStream70", offset: 0x1400, size: 28,
                                c1 => c1.VerifyField(name: "impv", PDBIMPV.PDBImpvVC70),
                                c1 => c1.VerifyFieldIgnoreValue(name: "sig"), //sig is time(0) when fRepro is not specified
                                c1 => c1.VerifyField(name: "age", value: 1),
                                c1 => c1.VerifyFieldIgnoreValue(name: "sig70") //sig70 is a random GUID when fRepro is not specified
                            ),
                            c => c.VerifyStruct(name: "Stream Name Table", offset: 0x141C, size: 24,
                                c1 => c1.VerifyField(name: "Name Buffer Size", value: 0),
                                c1 => c1.VerifyStruct(name: "Map", offset: 0x1420, size: 20,
                                    c2 => c2.VerifyField(name: "Size", value: 0),
                                    c2 => c2.VerifyField(name: "Capacity", value: 1),
                                    c2 => c2.VerifyField(name: "Present Word Count", value: 1),
                                    c2 => c2.VerifyField(name: "Present Words", value: new[]{0}),
                                    c2 => c2.VerifyField(name: "Deleted Word Count", value: 0)
                                )
                            ),
                            c => c.VerifyValue(offset: 0x1434, (PdbFeature) 0),
                            c => c.VerifyValue(offset: 0x1438, PdbFeature.impvVC140),
                            c => c.VerifyByteBlob(offset: 0x143C, value: new byte[964])
                        ),

                        //Stream Table
                        v => v.VerifyLogicalRegion(name: "6 | Stream Table (1/1)", offset: 0x1800, size: 0x400,
                            c => c.VerifyStruct(name: "Stream Table", offset: 0x1800, size: 32,
                                c1 => c1.VerifyField(name: "NumStreams", 5),
                                c1 => c1.VerifyField(name: "StreamSizes", new[]{4, 60, 0, 0, 0}),
                                c1 => c1.VerifyField(name: "PageList (0)", new PN[]{3}),
                                c1 => c1.VerifyField(name: "PageList (1)", new PN[]{5})
                            ),
                            c => c.VerifyByteBlob(offset: 0x1820, value: new byte[992])
                        )
                    },
                    after: 4
                )
            );
        }

        [TestMethod]
        public unsafe void PDB_PDBStream_WithNamedStream()
        {
            TestPDBView(pdb =>
                {
                    //It seems that only "x" (exclusive) mode is used, but let's just say we want to write for good measure
                    using (var stream = pdb.OpenStreamEx("foo", PdbOpenMode.pdbWrite))
                    {
                        int val = 4;
                        stream.Append((IntPtr) (&val), 4);
                    }

                    pdb.Commit();
                },
                WithIgnores(
                    before: 3,
                    action: new Action<IView>[]
                    {
                        v => v.VerifyLogicalRegion(name: "3 | 'foo (5)' (1/1)", offset: 0xC00, size: 0x400,
                            c =>
                            {
                                var bytes = new byte[1024];
                                bytes[0] = 4;
                                c.VerifyByteBlob(0xC00, bytes);
                            }
                        ),
                        v => v.VerifyLogicalRegion(name: "4 | 'PDB (1)' (1/1)", offset: 0x1000, size: 0x400,
                            WithIgnores(
                                before: 1,
                                action: new Action<IView>[]
                                {
                                    c => c.VerifyStruct(name: "Stream Name Table", offset: 0x101C, size: 36,
                                        c1 => c1.VerifyField(name: "Name Buffer Size", value: 4),
                                        c1 => c1.VerifyValue(offset: 0x1020, "foo"),
                                        c1 => c1.VerifyStruct(name: "Map", offset: 0x1024, size: 28,
                                            c2 => c2.VerifyField(name: "Size", value: 1),
                                            c2 => c2.VerifyField(name: "Capacity", value: 2),
                                            c2 => c2.VerifyField(name: "Present Word Count", value: 1),
                                            c2 => c2.VerifyField(name: "Present Words", value: new[]{1}),
                                            c2 => c2.VerifyField(name: "Deleted Word Count", value: 0),
                                            c2 => c2.VerifyStruct(name: "Entry", offset: 0x1038, size: 8,
                                                c3 => c3.VerifyField("Key", 0),
                                                c3 => c3.VerifyField("Value", 5)
                                            )
                                        )
                                    ),
                                },
                                after: 3
                            )
                        )
                    },
                    after: 6
                )
            );
        }

        private void TestPDBView(Action<PDB1> configurePDB, params Action<IView>[] verify) =>
            TestPDBView(configurePDB, verify, null);

        private void TestPDBView(
            Action<PDB1> configurePDB,
            Action<IView>[] verify,
            Action<IView[]> verifyExtra)
        {
            using var tempFile = TempFile.New();

            using (var vsPdb = VsPDB.CreatePDB(tempFile))
            {
                configurePDB?.Invoke(vsPdb.PDB);
            }

            TestViewInternal(tempFile, verify, verifyExtra);
        }

        #endregion
        #region DBI

        [TestMethod]
        public void PDB_Minimal_DBIStream()
        {
            //A minimal PDB that has a DBI Stream

            var actions = IgnoreValues(19);

            actions[3] = v => v.VerifyLogicalRegion(name: "3 | 'IPI (4)' (1/1)", offset: 0xC00, 0x400,
                c => Assert.IsInstanceOfType(c, typeof(ByteBlobView)) //When we implement proper support for IPI, this assert will fail
            );

            #region snPDB

            actions[4] = v => v.VerifyLogicalRegion(name: "4 | 'PDB (1)' (1/1)", offset: 0x1000, 0x400,
                WithIgnores(
                    before: 1,
                    action: new Action<IView>[]
                    {
                        c => c.VerifyStruct(name: "Stream Name Table", offset: 0x101C, size: 85,
                            c1 => c1.VerifyField(name: "Name Buffer Size", value: 37),
                            c1 => c1.VerifyValue(offset: 0x1033, "/UDTSRCLINEUNDONE"),
                            c1 => c1.VerifyValue(offset: 0x1020, "/LinkInfo"),
                            c1 => c1.VerifyValue(offset: 0x102A, "/TMCache"),
                            c1 => c1.VerifyStruct(name: "Map", offset: 0x1045, size: 44,
                                c2 => c2.VerifyField(name: "Size", value: 3),
                                c2 => c2.VerifyField(name: "Capacity", value: 6),
                                c2 => c2.VerifyField(name: "Present Word Count", value: 1),
                                c2 => c2.VerifyField(name: "Present Words", value: new[]{41}),
                                c2 => c2.VerifyField(name: "Deleted Word Count", value: 0),
                                c2 => c2.VerifyStruct(name: "Entry", offset: 0x1059, size: 8,
                                    c3 => c3.VerifyField("Key", 19),
                                    c3 => c3.VerifyField("Value", 11)
                                ),
                                c2 => c2.VerifyStruct(name: "Entry", offset: 0x1061, size: 8,
                                    c3 => c3.VerifyField("Key", 0),
                                    c3 => c3.VerifyField("Value", 5)
                                ),
                                c2 => c2.VerifyStruct(name: "Entry", offset: 0x1069, size: 8,
                                    c3 => c3.VerifyField("Key", 10),
                                    c3 => c3.VerifyField("Value", 6)
                                )
                            )
                        ),
                    },
                    after: 3
                )
            );

            #endregion

            actions[9] = v => v.VerifyLogicalRegionIgnoreChildren(name: "9 | 'Globals (7)' (1/1)", offset: 0x2400, size: 0x400); //snGSSyms (currently unimplemented)

            #region snDbi

            actions[10] = v => v.VerifyLogicalRegion(name: "10 | 'DBI (3)' (1/1)", offset: 0x2800, size: 0x400,
                c => c.VerifyStruct(name: "NewDBIHdr", offset: 0x2800, size: 64,
                    c1 => c1.VerifyField(name: "verSignature", value: -1),
                    c1 => c1.VerifyField(name: "verHdr", value: DBIImpv.DBIImpvV70),
                    c1 => c1.VerifyField(name: "age", value: 1),
                    c1 => c1.VerifyField(name: "snGSSyms", value: (SN) 7),
                    c1 => c1.VerifyBitField(name: "usVerPdbDllMin", value: (byte) 42, bits: 8),
                    c1 => c1.VerifyBitField(name: "usVerPdbDllMaj", value: (byte) 14, bits: 7),
                    c1 => c1.VerifyBitField(name: "fNewVerFmt", value: (byte) 1, bits: 1),
                    c1 => c1.VerifyField(name: "snPSSyms", value: (SN) 8),
                    c1 => c1.VerifyField(name: "usVerPdbDllBuild", value: (ushort) 34436),
                    c1 => c1.VerifyField(name: "snSymRecs", value: (SN) 9),
                    c1 => c1.VerifyField(name: "usVerPdbDllRBld", value: (ushort) 0),
                    c1 => c1.VerifyField(name: "cbGpModi", value: 0),
                    c1 => c1.VerifyField(name: "cbSC", value: 4),
                    c1 => c1.VerifyField(name: "cbSecMap", value: 4),
                    c1 => c1.VerifyField(name: "cbFileInfo", value: 4),
                    c1 => c1.VerifyField(name: "cbTSMap", value: 0),
                    c1 => c1.VerifyField(name: "iMFC", value: 0),
                    c1 => c1.VerifyField(name: "cbDbgHdr", value: 0),
                    c1 => c1.VerifyField(name: "cbECInfo", value: 25),
                    c1 => c1.VerifyBitField(name: "fIncLink", value: (byte) 0, bits: 1),
                    c1 => c1.VerifyBitField(name: "fStripped", value: (byte) 0, bits: 1),
                    c1 => c1.VerifyBitField(name: "fCTypes", value: (byte) 0, bits: 1),
                    c1 => c1.VerifyBitField(name: "unused", value: (ushort) 0, bits: 13),
                    c1 => c1.VerifyField(name: "wMachine", value: IMAGE_FILE_MACHINE.AMD64),
                    c1 => c1.VerifyField(name: "rgulReserved", value: 0)
                ),
                c => c.VerifyStruct(name: "Section Contribs", offset: 0x2840, size: 4,
                    c1 => c1.VerifyField(name: "Version", value: DBISCImpv.DBISCImpvV60)
                ),
                c => c.VerifyStruct(name: "OMFSegMap", offset: 0x2844, size: 4,
                    c1 => c1.VerifyField(name: "cSeg", value: (short) 0),
                    c1 => c1.VerifyField(name: "cSegLog", value: (short) 0)
                ),
                c => c.VerifyStruct(name: "File Info", offset: 0x2848, size: 4,
                    c1 => c1.VerifyField(name: "NumModules", value: (short) 0),
                    c1 => c1.VerifyField(name: "NumSourceFiles", value: (short) 0)
                ),
                c => c.VerifyStruct(name: "Name Table", offset: 0x284C, size: 25,
                    c1 => c1.VerifyStruct(name: "VHdr", offset: 0x284C, size: 8,
                        c2 => c2.VerifyField(name: "ulHdr", value: VHdr.Hdr.verHdr),
                        c2 => c2.VerifyField(name: "ulVer", value: VHdr.Ver.verLongHash)
                    ),
                    c1 => c1.VerifyField(name: "Name Buffer Size", 1),
                    c1 => c1.VerifyField(name: "Num Offsets", 1),
                    c1 => c1.VerifyField(name: "Offsets", new int[]{0}),
                    c1 => c1.VerifyField(name: "Num Strings", 0),
                    c1 => c1.VerifyValue(0x2858, string.Empty)
                ),
                c => c.VerifyByteBlob(offset: 0x2865, value: new byte[923])
            );

            #endregion

            actions[11] = v => v.VerifyLogicalRegionIgnoreChildren(name: "11 | 'Publics (8)' (1/1)", offset: 0x2C00, size: 0x400); //snPSSyms (currently unimplemented)

            actions[12] = v => v.VerifyLogicalRegion(name: "12 | 'TPI (2)' (1/1)", offset: 0x3000, size: 0x400,
                c => Assert.IsInstanceOfType(c, typeof(ByteBlobView)) //When we implement proper support for TPI, this assert will fail
            );

            actions[13] = v => v.VerifyLogicalRegion(name: "13 | Stream Table (1/1)", offset: 0x3400, size: 0x400,
                c => c.VerifyStruct(name: "Stream Table", offset: 0x3400, size: 84,
                    c1 => c1.VerifyField(name: "NumStreams", value: 13),
                    c1 => c1.VerifyField(name: "StreamSizes", new[] { 40, 121, 56, 101, 56, 0, 0, 16, 44, 0, 0, 0, 0 }),
                    c1 => c1.VerifyField(name: "PageList (0)", new PN[] { 7 }), //Each PN[] is written as a PageList field
                    c1 => c1.VerifyField(name: "PageList (1)", new PN[] { 4 }),
                    c1 => c1.VerifyField(name: "PageList (2)", new PN[] { 12 }),
                    c1 => c1.VerifyField(name: "PageList (3)", new PN[] { 10 }),
                    c1 => c1.VerifyField(name: "PageList (4)", new PN[] { 3 }),
                    c1 => c1.VerifyField(name: "PageList (7)", new PN[] { 9 }),
                    c1 => c1.VerifyField(name: "PageList (8)", new PN[] { 11 })
                ),
                c => c.VerifyByteBlob(offset: 0x3454, new byte[940])
            );

            TestDBIView(
                null,
                actions
            );
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo()
        {
            TestDBIStructView(
                ViewKind.Modi60Persist,
                dbi =>
                {
                    //The module name and object file name are appended to the end of the MODI
                    dbi.OpenModW("foo", "bar").Dispose();
                },
                v => v.VerifyStruct(name: "MODI_60_Persist", offset: 0x2840, size: 72,
                    c => c.VerifyField("pmod", 0),
                    c => c.VerifyStruct(name: "SC", offset: 0x2844, size: 28,
                        c1 => c1.VerifyField(name: "isect", value: (ISECT) ISECT.Nil),
                        c1 => c1.VerifyField(name: "padding1", value: (ushort) 0),
                        c1 => c1.VerifyField(name: "off", value: 0),
                        c1 => c1.VerifyField(name: "cb", value: -1),
                        c1 => c1.VerifyField(name: "dwCharacteristics", value: IMAGE_SCN.TYPE_REG),
                        c1 => c1.VerifyField(name: "imod", value: (IMOD) IMOD.Nil),
                        c1 => c1.VerifyField(name: "padding2", value: (ushort) 0),
                        c1 => c1.VerifyField(name: "dwDataCrc", value: 0),
                        c1 => c1.VerifyField(name: "dwRelocCrc", value: 0)
                    ),
                    c => c.VerifyBitField(name: "fWritten", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "fECEnabled", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "unused", value: (byte) 0, bits: 6),
                    c => c.VerifyBitField(name: "iTSM", value: (byte) 0, bits: 8),
                    c => c.VerifyField(name: "sn", (SN) SN.Nil),
                    c => c.VerifyField(name: "cbSyms", value: 0),
                    c => c.VerifyField(name: "cbLines", value: 0),
                    c => c.VerifyField(name: "cbC13Lines", value: 0),
                    c => c.VerifyField(name: "ifileMac", value: (ushort) 0),
                    c => c.VerifyField(name: "padding1", value: (ushort) 0),
                    c => c.VerifyField(name: "mpifileichFile", 0),
                    c => c.VerifyStruct(name: "ECInfo", offset: 0x2878, size: 8,
                        c1 => c1.VerifyField("niSrcFile", 0),
                        c1 => c1.VerifyField("niPdbFile", 0)
                    ),
                    c => c.VerifyField("szModule", "foo"),
                    c => c.VerifyField("szObjFile", "bar")
                )
            );
        }

        #region Module C13

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_Symbols()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_Lines()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_StringTable()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_FileCheckSums()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_FrameData()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_InlineeLines()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_CrossScopeImports()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_CrossScopeExports()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_ILLines()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_FuncMDTokenMap()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_TypeMDTokenMap()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_MergedAssemblyInput()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_ModuleInfo_C13_CoffSymbolRva()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        #endregion

        [TestMethod]
        public void PDB_DBIStream_SectionContribsV60()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_SectionMap()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_FileInfo()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_TypeServerMap()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void PDB_DBIStream_EditAndContinueInfo()
        {
            Assert.Inconclusive();
            throw new NotImplementedException();
        }

        #region Debug Header

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_FPO()
        {
            var bytes = new byte[0x400];
            bytes[0] = 1;
            bytes[15] = 128;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeFPO,
                new FPO_DATA
                {
                    ulOffStart = 1,
                    cbFrame = 2
                },
                v => v.VerifyLogicalRegion(name: "9 | 'FPO (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_Exception()
        {
            var bytes = new byte[0x400];
            bytes[0] = 1;
            bytes[4] = 2;
            bytes[8] = 3;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeException,
                new IMAGE_FUNCTION_ENTRY
                {
                    StartingAddress = 1,
                    EndingAddress = 2,
                    EndOfPrologue = 3
                },
                v => v.VerifyLogicalRegion(name: "9 | 'Exception (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_Fixup()
        {
            var bytes = new byte[0x400];
            bytes[0] = 1;
            bytes[8] = 2;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeFixup,
                new XFIXUP_DATA
                {
                    wType = 1,
                    rvaTarget = 2
                },
                v => v.VerifyLogicalRegion(name: "9 | 'Fixup (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_OmapToSrc()
        {
            var bytes = new byte[0x400];
            bytes[0] = 1;
            bytes[4] = 2;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeOmapToSrc,
                new OMAP_DATA
                {
                    rva = 1,
                    rvaTo = 2
                },
                v => v.VerifyLogicalRegion(name: "9 | 'OmapToSrc (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_OmapFromSrc()
        {
            var bytes = new byte[0x400];
            bytes[0] = 1;
            bytes[4] = 2;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeOmapFromSrc,
                new OMAP_DATA
                {
                    rva = 1,
                    rvaTo = 2
                },
                v => v.VerifyLogicalRegion(name: "9 | 'OmapFromSrc (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_SectionHdr()
        {
            var bytes = new byte[0x400];
            bytes[8] = 1;
            bytes[39] = 32;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeSectionHdr,
                new IMAGE_SECTION_HEADER
                {
                    VirtualSize = 1,
                    Characteristics = IMAGE_SCN.MEM_EXECUTE
                },
                v => v.VerifyLogicalRegion(name: "9 | 'SectionHdr (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_TokenRidMap()
        {
            var bytes = new byte[0x400];
            bytes[0] = 1;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeTokenRidMap,
                1, //DumpTokenMap() just shows dumping random ULONGs
                v => v.VerifyLogicalRegion(name: "9 | 'TokenRidMap (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_XData()
        {
            Assert.Inconclusive();

            //var bytes = new byte[0x400];

            //TestDBIDbgHdr(
            //    DBGTYPE.dbgtypeXdata,
            //    new,
            //    v => v.VerifyLogicalRegion(name: "9 | 'FPO (7)' (1/1)", offset: 0x2400, size: 0x400,
            //        c => c.VerifyByteBlob(offset: 0x2400, bytes)
            //    )
            //);

            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_PData()
        {
            Assert.Inconclusive();

            //var bytes = new byte[0x400];

            //TestDBIDbgHdr(
            //    DBGTYPE.dbgtypePdata,
            //    new,
            //    v => v.VerifyLogicalRegion(name: "9 | 'FPO (7)' (1/1)", offset: 0x2400, size: 0x400,
            //        c => c.VerifyByteBlob(offset: 0x2400, bytes)
            //    )
            //);

            throw new NotImplementedException();
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_NewFPO()
        {
            var bytes = new byte[0x400];
            bytes[0] = 1;
            bytes[28] = 4; //I think it's 4 because of the particular bitfield we set

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeNewFPO,
                new FRAMEDATA
                {
                    ulRvaStart = 1,
                    fIsFunctionStart = true
                },
                v => v.VerifyLogicalRegion(name: "9 | 'NewFPO (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, value: bytes)
                )
            );
        }

        [TestMethod]
        public void PDB_DBIStream_DbgHdr_SectionHdrOrig()
        {
            var bytes = new byte[0x400];
            bytes[8] = 1;
            bytes[39] = 32;

            TestDBIDbgHdr(
                DBGTYPE.dbgtypeSectionHdrOrig,
                new IMAGE_SECTION_HEADER
                {
                    VirtualSize = 1,
                    Characteristics = IMAGE_SCN.MEM_EXECUTE
                },
                v => v.VerifyLogicalRegion(name: "9 | 'SectionHdrOrig (7)' (1/1)", offset: 0x2400, size: 0x400,
                    c => c.VerifyByteBlob(offset: 0x2400, bytes)
                )
            );
        }

        #endregion

        private void TestDBIView(Action<DBI1> configureDBI, params Action<IView>[] verify) =>
            TestDBIView(configureDBI, verify, null);

        private void TestDBIStructView(ViewKind kind, Action<DBI1> configureDBI, params Action<IView>[] verify)
        {
            using var tempFile = TempFile.New();

            using (var vsPdb = VsPDB.CreatePDB(tempFile))
            {
                using (var dbi = vsPdb.PDB.CreateDBI(null))
                {
                    //Setting the MachineType acts as a useful gauge that we're deserializing the NewDbiHdr correctly, as this member is second last from the end
                    dbi.MachineType = IMAGE_FILE_MACHINE.AMD64;
                    configureDBI?.Invoke(dbi);
                }

                vsPdb.PDB.Commit();
            }

            using (var pdb = PDBFile.FromFile(tempFile))
            {
                var views = pdb.GetView().Children;

                var dbi = views.OfType<LogicalRegionView>().Single(v => v.Name.Contains("DBI (3)"));

                var matches = dbi.Children.Where(v => v.Kind == kind).ToArray();

                Assert.AreEqual(matches.Length, verify.Length);

                for (var i = 0; i < verify.Length; i++)
                    verify[i](matches[i]);
            }
        }

        private unsafe void TestDBIDbgHdr<TData>(
            DBGTYPE type,
            TData data,
            Action<IView> verify) where TData : unmanaged
        {
            using var tempFile = TempFile.New();

            using (var vsPdb = VsPDB.CreatePDB(tempFile))
            {
                using (var dbi = vsPdb.PDB.CreateDBI(null))
                {
                    //Setting the MachineType acts as a useful gauge that we're deserializing the NewDbiHdr correctly, as this member is second last from the end
                    dbi.MachineType = IMAGE_FILE_MACHINE.AMD64;

                    //Simply opening the stream won't actually create anything.
                    //I think because DBI1::fSaveDbgData will call nullifystream
                    //and delete the stream if the dbg buffer size was 0
                    using var dbg = dbi.OpenDbg(type);

                    var size = Marshal.SizeOf<TData>();
                    Assert.AreEqual(dbg.ElementSize, size);

                    dbg.Append(1, (IntPtr) (&data));
                }

                vsPdb.PDB.Commit();
            }

            using (var pdb = PDBFile.FromFile(tempFile))
            {
                var views = pdb.GetView().Children;

                var dbi = views.OfType<LogicalRegionView>().Single(v => v.Name.Contains("DBI (3)"));

                var dbgDataHdr = (StructView) dbi.Children.Single(c => c.Kind == ViewKind.DbgDataHdr);

                //Everyone should be snNil but the one we're after
                Assert.AreEqual(12, dbgDataHdr.Children.Length);

                var targetData = (FieldView<SN>) dbgDataHdr.Children[(int) type];
                Assert.AreNotEqual((SN) SN.Nil, targetData.Value);

                for (var i = 0; i < dbgDataHdr.Children.Length; i++)
                {
                    if (i == (int) type)
                        continue;

                    var otherData = (FieldView<SN>) dbgDataHdr.Children[i];
                    Assert.AreEqual((SN) SN.Nil, otherData.Value);
                }

                //Get the page associated with our target data
                var targetPage = pdb.StreamTable.StreamPages[targetData.Value][0];

                var targetPageView = views[targetPage];

                verify(targetPageView);
            }
        }

        private void TestDBIView(
            Action<DBI1> configureDBI,
            Action<IView>[] verify,
            Action<IView[]> verifyExtra)
        {
            using var tempFile = TempFile.New();

            using (var vsPdb = VsPDB.CreatePDB(tempFile))
            {
                using (var dbi = vsPdb.PDB.CreateDBI(null))
                {
                    //Setting the MachineType acts as a useful gauge that we're deserializing the NewDbiHdr correctly, as this member is second last from the end
                    dbi.MachineType = IMAGE_FILE_MACHINE.AMD64;
                    configureDBI?.Invoke(dbi);
                }

                vsPdb.PDB.Commit();
            }

            TestViewInternal(tempFile, verify, verifyExtra);
        }

        #endregion
        #region PDB2

        [TestMethod]
        public void PDB_V2_VC40()
        {
            TestPDBSample(
                Sample.VC40_PDB,
                null
            );
        }

        [TestMethod]
        public void PDB_V2_VC60()
        {
            TestPDBSample(
                Sample.VC60_PDB,
                null
            );
        }

        #endregion
        #region Splitting

        [TestMethod]
        public void PDB_View_SplitStruct_SingleChild_Field()
        {
            //The single child field of the parent struct overlaps both halves of the boundary

            //First, test on a StructView

            var view = new StructView(100, "OuterStruct", new IView[]
            {
                new FieldView<int>(100, "InnerField", 1, 4)
            }, 4, ViewKind.Value);

            var (first, second) = ((ISplittableView) view).Split(200, 102);

            first.VerifyStruct(name: "OuterStruct", offset: 100, size: 2,
                v => v.VerifyField(name: "InnerField", value: 1)
            );

            second.VerifyStruct(name: "OuterStruct", offset: 200, size: 2,
                v => v.VerifyField(name: "InnerField", value: 1)
            );

            //Now do the exact same test on an existing SplitStructView
        }

        [TestMethod]
        public void PDB_View_SplitStruct_SingleChild_Struct()
        {
            //The single child struct of the parent struct overlaps both halves of the boundary

            var view = new StructView(100, "OuterStruct", new IView[]
            {
                new StructView(100, "InnerStruct", new IView[]
                {
                    new FieldView<int>(100, "InnerField", 1, 4)
                }, size: 4, ViewKind.Value)
            }, 4, ViewKind.Value);

            var (first, second) = ((ISplittableView) view).Split(200, 102);

            first.VerifyStruct(name: "OuterStruct", offset: 100, size: 2,
                v => v.VerifyStruct(name: "InnerStruct", offset: 100, size: 2,
                    v2 => v2.VerifyField(name: "InnerField", value: 1)
                )
            );

            second.VerifyStruct(name: "OuterStruct", offset: 200, size: 2,
                v => v.VerifyStruct(name: "InnerStruct", offset: 200, size: 2,
                    v2 => v2.VerifyField(name: "InnerField", value: 1)
                )
            );
        }

        [TestMethod]
        public void PDB_View_SplitStruct_TwoChildren_PerfectSplit()
        {
            //There are two children of the parent struct, and there's no need to cut one of them in half. The left split gets the children to the left of the split, the right struct gets the children to the right

            var view = new StructView(100, "OuterStruct", new IView[]
            {
                new FieldView<int>(100, "InnerField1", 1, 4),
                new FieldView<int>(104, "InnerField2", 2, 4),
            }, 8, ViewKind.Value);

            var (first, second) = ((ISplittableView) view).Split(200, 104);

            first.VerifyStruct(name: "OuterStruct", offset: 100, size: 4,
                v => v.VerifyField(name: "InnerField1", 1)
            );

            second.VerifyStruct(name: "OuterStruct", offset: 200, size: 4,
                v => v.VerifyField(name: "InnerField2", 2)
            );
        }

        [TestMethod]
        public void PDB_View_SplitStruct_ThreeChildren_SplitMiddle()
        {
            //There's three children and we split the middle one in half, giving one half to each split struct

            var view = new StructView(100, "OuterStruct", new IView[]
            {
                new FieldView<int>(100, "InnerField1", 1, 4),
                new FieldView<int>(104, "InnerField2", 2, 4),
                new FieldView<int>(108, "InnerField3", 3, 4),
            }, 12, ViewKind.Value);

            var (first, second) = ((ISplittableView) view).Split(200, 106);

            first.VerifyStruct(name: "OuterStruct", offset: 100, size: 6,
                v => v.VerifyField("InnerField1", 1),
                v => v.VerifyField("InnerField2", 2)
            );

            second.VerifyStruct(name: "OuterStruct", offset: 200, size: 6,
                v => v.VerifyField("InnerField2", 2),
                v => v.VerifyField("InnerField3", 3)
            );
        }

        #endregion

        private void TestViewInternal(
            string tempFile,
            Action<IView>[] verify,
            Action<IView[]> verifyExtra)
        {
            using (var pdb = PDBFile.FromFile(tempFile))
            {
                var views = pdb.GetView().Children;

                Assert.AreEqual(views.Length, verify.Length);

                for (var i = 0; i < verify.Length; i++)
                    verify[i](views[i]);

                verifyExtra?.Invoke(views);
            }
        }

        private void TestPDBSample(
            string path,
            Action<IView>[] verify)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Could not find sample file '{path}'");

            using (var pdb = PDBFile.FromFile(path))
            {
                var views = pdb.GetView().Children;

                Assert.AreEqual(views.Length, verify.Length);

                for (var i = 0; i < verify.Length; i++)
                    verify[i](views[i]);
            }
        }
    }
}
