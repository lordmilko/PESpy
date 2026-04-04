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
                /* We receive a list of 14 regions, as follows
                 * 
                 * ImportAddressTable bcrypt.dll
                 * ImportAddressTable ucrtbase_enclave.dll
                 * ImportAddressTable vertdll.dll
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

                Assert.AreEqual("[ImportAddressTable] bcrypt.dll", r[0].Name);
                Assert.AreEqual("[ImportAddressTable] ucrtbase_enclave.dll", r[1].Name);
                Assert.AreEqual("[ImportAddressTable] vertdll.dll", r[2].Name);

                Assert.AreEqual("[ImportLookupTable] bcrypt.dll", r[7].Name);
                Assert.AreEqual("[ImportLookupTable] ucrtbase_enclave.dll", r[8].Name);
                Assert.AreEqual("[ImportLookupTable] vertdll.dll", r[9].Name);

                Assert.AreEqual(14, r[0].Children.Count);
                Assert.AreEqual(38, r[1].Children.Count);
                Assert.AreEqual(22, r[2].Children.Count);

                Assert.AreEqual(14, r[7].Children.Count);
                Assert.AreEqual(38, r[8].Children.Count);
                Assert.AreEqual(22, r[9].Children.Count);
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

                Assert.AreEqual("[DelayImportLookupTable] VERSION.dll", r[15].Name);
                Assert.AreEqual("[DelayImportLookupTable] api-ms-win-core-winrt-l1-1-0.dll", r[16].Name);

                Assert.AreEqual("[DelayImportAddressTable] VERSION.dll", r[34].Name);
                Assert.AreEqual("[DelayImportAddressTable] api-ms-win-core-winrt-l1-1-0.dll", r[35].Name);

                Assert.AreEqual(4, r[15].Children.Count);
                Assert.AreEqual(3, r[16].Children.Count);

                Assert.AreEqual(4, r[34].Children.Count);
                Assert.AreEqual(3, r[35].Children.Count);
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
                Assert.AreEqual(4, r.Length);
                Assert.AreEqual("Unwind Infos", r[0].Name);
                Assert.AreEqual("Export Address Table", r[1].Name);
                Assert.AreEqual("Export Names Table", r[2].Name);
                Assert.AreEqual("Export Ordinals Table", r[3].Name);

                //Given an ordinal export ordinals gets the name of that ordinal. The first item at ordinal 8 does not have a name,
                //and therefore does not have an item in either names or ordinal names
                Assert.AreEqual(4114, r[0].Children.Count);
                Assert.AreEqual(2486, r[1].Children.Count);
                Assert.AreEqual(2485, r[2].Children.Count);
                Assert.AreEqual(2485, r[3].Children.Count);
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

        [TestMethod]
        public void RegionView_BundleManifest()
        {
            WithRegions(Sample.SingleFileApp_EXE, r =>
            {
                Assert.AreEqual(337, r.Length);

                var region = r[336];
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

        class RegionCollector : ViewWalker
        {
            public List<LogicalRegionView> Regions { get; } = new List<LogicalRegionView>();

            protected internal override void VisitLogicalRegion(LogicalRegionView view)
            {
                if (view.Kind != ViewKind.DataDirectory)
                    Regions.Add(view);

                base.VisitLogicalRegion(view);
            }
        }
    }
}
