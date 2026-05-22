using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    [TestClass]
    public class ExceptionHandlerContextTests : BaseTest
    {
        #region GSHandlerCheck

        [TestMethod]
        public void ExceptionHandlerContext_GSHandlerCheck()
        {
            using var peFile = PEFileFromKey(WellKnownTestModule.ntdll);

            var exceptionData = (GsHandlerData) peFile.ExceptionTable[0].UnwindData.Value.ExceptionData;

            Assert.AreEqual(0x0015be5c, exceptionData.Offset);

            var view = peFile.GetView(trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x15c1f4);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            var gsHandlerDataView = view.GetViewFromOffset(0x15be5c);
            Assert.AreEqual(ViewKind.GsHandlerData, gsHandlerDataView.Kind);

            //Verify we have various xrefs
            var exceptionHandler = unwindInfoView.GetField("ExceptionHandler");
            Assert.IsTrue(exceptionHandler.XRefs.Count > 0);
        }

        [TestMethod]
        public void ExceptionHandlerContext_GSHandlerCheck_SEH()
        {
            using var peFile = PEFileFromKey(WellKnownTestModule.ntdll);

            var exceptionData = (ScopeTableAndGsHandlerData) peFile.ExceptionTable[28].UnwindData.Value.ExceptionData;

            Assert.AreEqual(0x15c214, exceptionData.Offset);

            var view = peFile.GetView(trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x15c1f4);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            //Verify we have various xrefs
            var exceptionHandler = unwindInfoView.GetField("ExceptionHandler");
            Assert.IsTrue(exceptionHandler.XRefs.Count > 0);

            //The ExceptionData starts at 0x15c214
            var scopeTableView = view.GetViewFromOffset(0x15c214);
            Assert.AreEqual(ViewKind.ScopeTable, scopeTableView.Kind);

            var gsHandlerDataView = view.GetViewFromOffset(0x15c238);
            Assert.AreEqual(ViewKind.GsHandlerData, gsHandlerDataView.Kind);
        }

        [TestMethod]
        public void ExceptionHandlerContext_GSHandlerCheck_EH()
        {
            using var peFile = PEFileFromKey(WellKnownTestModule.DbgEng);

            var exceptionData = (FuncInfoAndGsHandlerData) peFile.ExceptionTable[39].UnwindData.Value.ExceptionData;

            Assert.AreEqual(0x54c690, exceptionData.Offset);

            var view = peFile.GetView(trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x54de78);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            //Verify we have various xrefs
            var exceptionHandler = unwindInfoView.GetField("ExceptionHandler");
            Assert.IsTrue(exceptionHandler.XRefs.Count > 0);

            //The ExceptionData starts at 0x54c690
            var funcInfoRva = unwindInfoView.GetChild(ViewKind.FuncInfoRva);
            var gsHandlerDataView = view.GetViewFromOffset(0x54c694);

            Assert.AreEqual(1, funcInfoRva.XRefs.Count);

            var funcInfoView = view.GetViewFromRVA(0x4cfcd0);

            var xrefTarget = funcInfoRva.XRefs[0].Other;
            Assert.AreEqual(funcInfoView.Offset, xrefTarget.Offset);
        }

        [TestMethod]
        public void ExceptionHandlerContext_GSHandlerCheck_EH4()
        {
            using var peFile = PEFileFromKey(WellKnownTestModule.AzureAttest);

            var exceptionData = (FuncInfo4AndGsHandlerData) peFile.ExceptionTable[512].UnwindData.Value.ExceptionData;

            Assert.AreEqual(0x36c20, exceptionData.Offset);

            var view = peFile.GetView(ViewMode.Physical, trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x36c04);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            //Verify we have various xrefs
            var exceptionHandler = unwindInfoView.GetField("ExceptionHandler");
            Assert.IsTrue(exceptionHandler.XRefs.Count > 0);

            //The ExceptionData starts at 0x36C20
            var funcInfoRva = unwindInfoView.GetChild(ViewKind.FuncInfo4Rva);
            var gsHandlerDataView = view.GetViewFromOffset(0x36c24);

            Assert.AreEqual(1, funcInfoRva.XRefs.Count);

            var funcInfo4View = view.GetViewFromOffset(0x36c28);

            var xrefTarget = funcInfoRva.XRefs[0].Other;
            Assert.AreEqual(funcInfo4View.Offset, xrefTarget.Offset);
        }

        #endregion
        #region C Specific Handler

        [TestMethod]
        public void ExceptionHandlerContext_C_specific_handler()
        {
            using var peFile = PEFileFromKey(WellKnownTestModule.ntdll);

            var exceptionData = (ScopeTable) peFile.ExceptionTable[3].UnwindData.Value.ExceptionData;

            Assert.AreEqual(0x15bea4, exceptionData.Offset);

            var view = peFile.GetView(ViewMode.Physical, trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x15be90);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            var scopeTable = view.GetViewFromOffset(0x15bea4);
            Assert.AreEqual(ViewKind.ScopeTable, scopeTable.Kind);
        }

        #endregion
        #region CxxFrameHandler

        [TestMethod]
        public void ExceptionHandlerContext_CxxFrameHandler()
        {
            //Check that all various xrefs were written

            using var peFile = PEFileFromKey(WellKnownTestModule._7z);

            var view = peFile.GetView(ViewMode.Physical, trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x1544D0);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            //This isn't written by UnwindInfo.WriteGlobals; rather it's written by the backdoor ViewByteViewWriter has
            //into writing info
            var funcInfoView = view.GetViewFromRVA(0x1342F8);
            Assert.AreEqual(ViewKind.FuncInfo, funcInfoView.Kind);

            Assert.AreEqual(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1, ((BitFieldView<EH_MAGIC_NUMBER>) funcInfoView.GetField("magicNumber")).Value);

            //Verify we have various xrefs
            var exceptionHandler = unwindInfoView.GetField("ExceptionHandler");
            Assert.IsTrue(exceptionHandler.XRefs.Count > 0);

            var funcInfoRva = unwindInfoView.GetChild(ViewKind.FuncInfoRva);
            Assert.AreEqual(1, funcInfoRva.XRefs.Count);

            var xrefTarget = funcInfoRva.XRefs[0].Other;
            Assert.AreEqual(funcInfoView.Offset, xrefTarget.Offset);
        }

        [TestMethod]
        public void ExceptionHandlerContext_CxxFrameHandler_AllEntitiesAfterRVA()
        {
            //There's an RVA to the FuncInfo data...which points to directly after the RVA.
            //And then after the FuncInfo...is all of the other various entities referenced
            //by the FuncInfo. It's all sequential, and so if we measure it we should see that
            //actually there's no space left for _GS_HANDLER_DATA

            //UnwindInfo at 0x1544D0

            using var peFile = PEFileFromKey(WellKnownTestModule._7z);

            var runtimeFunction = peFile.ExceptionTable[2594];
            var unwindInfo = runtimeFunction.UnwindData.Value;

            var kind = unwindInfo.ExceptionHandlerKind;
            Assert.AreEqual(WellKnownExceptionHandlerKind.__CxxFrameHandler, kind);

            var data = unwindInfo.ExceptionData;
            Assert.IsInstanceOfType(data, typeof(RVA<FuncInfo>));
        }

        [TestMethod]
        public void ExceptionHandlerContext_CxxFrameHandler3()
        {
            using var peFile = PEFileFromKey(WellKnownTestModule.AuthExt);

            var exceptionData = (RVA<FuncInfo>) peFile.ExceptionTable[74].UnwindData.Value.ExceptionData;

            Assert.AreEqual(0xed90, exceptionData.ActualOffset);

            var view = peFile.GetView(trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x105e4);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            var funcInfoView = view.GetViewFromRVA(0xed90);
            Assert.AreEqual(ViewKind.FuncInfo, funcInfoView.Kind);

            Assert.AreEqual(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3, ((BitFieldView<EH_MAGIC_NUMBER>) funcInfoView.GetField("magicNumber")).Value);

            //Verify we have various xrefs
            var exceptionHandler = unwindInfoView.GetField("ExceptionHandler");
            Assert.IsTrue(exceptionHandler.XRefs.Count > 0);

            var funcInfoRva = unwindInfoView.GetChild(ViewKind.FuncInfoRva);
            Assert.AreEqual(1, funcInfoRva.XRefs.Count);

            var xrefTarget = funcInfoRva.XRefs[0].Other;
            Assert.AreEqual(funcInfoView.Offset, xrefTarget.Offset);
        }

        [TestMethod]
        public void ExceptionHandlerContext_CxxFrameHandler4()
        {
            using var peFile = PEFileFromKey(WellKnownTestModule.AzureAttest);

            var exceptionData = (RVA<FuncInfo4>) peFile.ExceptionTable[316].UnwindData.Value.ExceptionData;

            Assert.AreEqual(0x37678, exceptionData.Value.Offset);

            var view = peFile.GetView(ViewMode.Physical, trackXRefs: true);

            var unwindInfoView = view.GetViewFromRVA(0x37668);
            Assert.AreEqual(ViewKind.UnwindInfo, unwindInfoView.Kind);

            var funcInfo4View = view.GetViewFromRVA(0x37678);
            Assert.AreEqual(ViewKind.FuncInfo4, funcInfo4View.Kind);

            //Verify we have various xrefs
            var exceptionHandler = unwindInfoView.GetField("ExceptionHandler");
            Assert.IsTrue(exceptionHandler.XRefs.Count > 0);

            var funcInfoRva = unwindInfoView.GetChild(ViewKind.FuncInfo4Rva);
            Assert.AreEqual(1, funcInfoRva.XRefs.Count);

            var xrefTarget = funcInfoRva.XRefs[0].Other;
            Assert.AreEqual(funcInfo4View.Offset, xrefTarget.Offset);
        }

        #endregion

        private static (WellKnownExceptionHandlerKind kind, int index)[] GetExceptionHandlerKinds(SymStoreKey key)
        {
            using var peFile = PEFileFromKey(key);

            var seen = new Dictionary<WellKnownExceptionHandlerKind, int>();

            if (peFile.ExceptionTable == null)
                return new (WellKnownExceptionHandlerKind kind, int index)[0];

            var i = 0;

            foreach (var item in peFile.ExceptionTable)
            {
                var kind = item.UnwindData.Value.ExceptionHandlerKind;

                if (kind != WellKnownExceptionHandlerKind.None && !seen.ContainsKey(kind))
                {
                    seen[kind] = i;
                }

                i++;
            }

            return seen.Select(kv => (kv.Key, kv.Value)).ToArray();
        }
    }
}
