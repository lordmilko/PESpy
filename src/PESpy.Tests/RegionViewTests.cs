using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    [TestClass]
    public class RegionViewTests
    {
        //All types that are supposed generate special region views should be tested here to confirm that they do indeed do that.

        #region ImageImportDescriptor

        [TestMethod]
        public void RegionView_ImageImportDescriptor_Test()
        {
            WithRegions(WellKnownTestModule.AzureAttest, r =>
            {
                /* We receive a list of several regions, as follows
                 * 
                 * Many "Functions" regions
                 * 
                 * ImportAddressTable bcrypt.dll
                 * ImportAddressTable ucrtbase_enclave.dll
                 * ImportAddressTable vertdll.dll
                 * 
                 * Strings + Unwind Infos regions
                 * 
                 * Export Address Table
                 * Export Names Table
                 * Export Ordinals Table
                 * 
                 * Import Lookup Table (Thunks) - a super region that collects the following child regions
                 * (note that the ImportAddressTable items above are not wrapped up in a super region because they'll get directly wrapped up in the IAT directory)
                 * - ImportLookupTable bcrypt.dll
                 * - ImportLookupTable ucrtbase_enclave.dll
                 * - ImportLookupTable vertdll.dll
                 * 
                 * 4x regions containing Import Strings
                 */

                Assert.AreEqual("[ImportAddressTable] bcrypt.dll", r[4].Name);
                Assert.AreEqual("[ImportAddressTable] ucrtbase_enclave.dll", r[5].Name);
                Assert.AreEqual("[ImportAddressTable] vertdll.dll", r[6].Name);

                Assert.AreEqual("[ImportLookupTable] bcrypt.dll", r[15].Name);
                Assert.AreEqual("[ImportLookupTable] ucrtbase_enclave.dll", r[16].Name);
                Assert.AreEqual("[ImportLookupTable] vertdll.dll", r[17].Name);

                Assert.AreEqual(14, r[4].Children.Count);
                Assert.AreEqual(38, r[5].Children.Count);
                Assert.AreEqual(22, r[6].Children.Count);

                Assert.AreEqual(14, r[15].Children.Count);
                Assert.AreEqual(38, r[16].Children.Count);
                Assert.AreEqual(22, r[17].Children.Count);
            });
        }

        #endregion
        #region ImageDelayLoadDescriptor

        [TestMethod]
        public void RegionView_ImageDelayLoadDescriptor_Test()
        {
            WithRegions(WellKnownTestModule.coreclr, r =>
            {
                /* We receive a list of 45 regions, as follows
                 * 
                 * 14 ImportAddressTable regions
                 * 
                 * Delay Import Lookup Table (Thunks) - a super region that contains the following child regions
                 * - DelayImportLookupTable VERSION.dll
                 * - DelayImportLookupTable api-ms-win-core-winrt-l1-1-0.dll
                 * 
                 * Delay Import Strings
                 * 
                 * Export Address Table
                 * Export Names Table
                 * Export Ordinals Table
                 * 
                 * Import Lookup Table (Thunks) - a super region that contains ImportLookupTable entries for all of the normal import descriptors
                 * 
                 * 6x Import Strings regions
                 * 
                 * - DelayImportAddressTable - a super region that contains the following child regions
                 * - DelayImportAddressTable VERSION.dll
                 * - DelayImportAddressTable api-ms-win-core-winrt-l1-1-0.dll
                 */

                Assert.AreEqual("[DelayImportLookupTable] VERSION.dll", r[89].Name);
                Assert.AreEqual("[DelayImportLookupTable] api-ms-win-core-winrt-l1-1-0.dll", r[90].Name);

                Assert.AreEqual("[DelayImportAddressTable] VERSION.dll", r[117].Name);
                Assert.AreEqual("[DelayImportAddressTable] api-ms-win-core-winrt-l1-1-0.dll", r[118].Name);

                Assert.AreEqual(4, r[89].Children.Count);
                Assert.AreEqual(3, r[90].Children.Count);

                Assert.AreEqual(4, r[117].Children.Count);
                Assert.AreEqual(3, r[118].Children.Count);
            });
        }

        #endregion

        //DBGFile Exported Names has a region, but our sample DBGFile doesn't have any exports

        #region ImageExportDirectory

        //Export Address Table
        //Export Names Table
        //Export Ordinals Table

        [TestMethod]
        public void RegionView_ImageExportDirectory_Test()
        {
            WithRegions(WellKnownTestModule.ntdll, r =>
            {
                Assert.AreEqual(139, r.Length);
                Assert.AreEqual("Export Address Table", r[135].Name);
                Assert.AreEqual("Export Names Table", r[136].Name);
                Assert.AreEqual("Export Ordinals Table", r[137].Name);

                //Given an ordinal export ordinals gets the name of that ordinal. The first item at ordinal 8 does not have a name,
                //and therefore does not have an item in either names or ordinal names
                Assert.AreEqual(2486, r[135].Children.Count);
                Assert.AreEqual(2485, r[136].Children.Count);
                Assert.AreEqual(2485, r[137].Children.Count);
            });
        }

        #endregion
        #region CompressedModelHeap

        //An example metadata table

        #endregion
        #region StorageStream

        //CompressedModelStream
        //StringPoolStream
        //USBlobPoolStream
        //BlobPoolStream
        //GuidPoolStream
        //Hot Model?

        #endregion
        #region DotNetRuntimeDebugHeader

        //DebugTypeEntries
        //GlobalValueEntries

        #endregion
        #region OMF

        [TestMethod]
        public void RegionView_OMF_Test()
        {
            WithRegions(Sample.VC50_EXE, r =>
            {
                //todo: i was randomly getting different lenght here
                //and i think its got something to do with markpadding;
                //0x3afa is sometimes unknown sometimes data -> unknown
                //and that throws our logic off cos we only look for
                //unknown

                Assert.AreEqual(12, r.Length);

                var region = r[10];
                Assert.AreEqual("NB11 OMF Data", region.Name);
                Assert.AreEqual(3518, region.Children.Count);
            });
        }

        #endregion
        #region Data Directories

        [TestMethod]
        public void RegionView_DataDirectory_SplitEnd()
        {
            using var peFile = PEFile.FromKey(WellKnownTestModule.coreclr);

            var view = peFile.GetView();

            //The ResourceTableDirectory ends with 4 bytes of padding, and then there's padding after it
            //for the rest of the .rsrc section, and these two sequences of bytes should be split
            var sectionView = (SectionView) view[9];
            Assert.AreEqual(".rsrc", sectionView.Name);

            var lastDirectoryEntry = (ByteBlobView) sectionView[0].Children.Last();
            var lastSectionEntry = (ByteBlobView) sectionView[1];

            Assert.AreEqual(4, lastDirectoryEntry.Bytes.Length);
            Assert.AreEqual(480, lastSectionEntry.Bytes.Length);
        }

        [TestMethod]
        public void RegionView_DataDirectory_AfterUnwindInfo()
        {
            //As of writing, we don't currently support reading the data in an NGEN
            //data directory that is right after an Unwind Info area; we don't want
            //the Unwind Info to expand over the data directories; so we should say that
            //Unwind Info stops prior to the start of a data directory after it

            WithRegions(Sample.NGEN_NI_DLL, (r, d) =>
            {
                var unwindInfos = r[16];
                Assert.AreEqual("Unwind Infos", unwindInfos.Name);
                Assert.AreEqual(0x1C20, unwindInfos.Offset);

                //The region currently encompasses the UNWIND_INFO and the unknown bytes after it.
                //Not sure if it actually _should_ be encompassing the unknown bytes after it
                Assert.AreEqual(208, unwindInfos.Size);

                var debugMap = d[18];
                Assert.AreEqual("NGEN DebugMap Directory", debugMap.Name);

                var virtualSectionsTable = d[19];
                Assert.AreEqual("NGEN VirtualSectionsTable Directory", virtualSectionsTable.Name);
            });
        }

        #endregion

        [TestMethod]
        public void RegionView_BundleManifest()
        {
            WithRegions(Sample.SingleFileApp_EXE, r =>
            {
                Assert.AreEqual(1029, r.Length);

                var region = r[1028];
                Assert.AreEqual("Bundle Manifest", region.Name);
                Assert.AreEqual(15, region.Children.Count);
            });
        }

        private void WithRegions(SymStoreKey key, Action<LogicalRegionView[]> validate)
        {
            using var peFile = PEFile.FromKey(key);

            var view = peFile.GetView();

            var regionCollector = new RegionCollector();

            view.Accept(regionCollector);

            var regions = regionCollector.Regions;

            validate(regions.ToArray());
        }

        private void WithRegions(string path, Action<LogicalRegionView[]> validate)
        {
            using var peFile = PEFile.FromFile(path);

            var view = peFile.GetView();

            var regionCollector = new RegionCollector();

            view.Accept(regionCollector);

            var regions = regionCollector.Regions;

            validate(regions.ToArray());
        }

        private void WithRegions(string path, Action<LogicalRegionView[], LogicalRegionView[]> validate)
        {
            using var peFile = PEFile.FromFile(path);

            var view = peFile.GetView();

            var regionCollector = new RegionCollector();

            view.Accept(regionCollector);

            validate(regionCollector.Regions.ToArray(), regionCollector.DataDirectories.ToArray());
        }

        class RegionCollector : ViewWalker
        {
            public List<LogicalRegionView> Regions { get; } = new List<LogicalRegionView>();

            public List<LogicalRegionView> DataDirectories { get; } = new List<LogicalRegionView>();

            protected internal override void VisitLogicalRegion(LogicalRegionView view)
            {
                if (view.Kind == ViewKind.DataDirectory)
                    DataDirectories.Add(view);
                else
                    Regions.Add(view);

                base.VisitLogicalRegion(view);
            }
        }
    }
}
