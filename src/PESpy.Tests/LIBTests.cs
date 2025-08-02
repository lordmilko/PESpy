using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    [TestClass]
    public class LIBTests
    {
        [TestMethod]
        public void LIB_VS22()
        {
            using var lib = LIBFile.FromFile(Sample.VS22_LIB);

            var view = lib.GetView();
        }
    }
}
