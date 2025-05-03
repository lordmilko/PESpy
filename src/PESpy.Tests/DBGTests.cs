#if PEFAST
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    [TestClass]
    public class DBGTests
    {
        [TestMethod]
        public void DBG_VC60()
        {
            using var dbg = DBGFile.FromFile(Sample.VC60_DBG);

            var view = dbg.GetView();

            Assert.AreEqual(14, view.Children.Length);
        }
    }
}
#endif
