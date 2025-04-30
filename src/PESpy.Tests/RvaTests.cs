using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    [TestClass]
    public class RvaTests
    {
        [TestMethod]
        public void Rva_Equals()
        {
            RVA<string> value1 = new RVA<string>(1, 2, "hello");
            RVA<string> value2 = new RVA<string>(1, 2, "hello");

            Assert.IsTrue(value1 == "hello");
            Assert.IsTrue("hello" == value1);
            Assert.IsTrue(value1 == value2);
        }

        [TestMethod]
        public void Va_Equals()
        {
            VA<string> value1 = new VA<string>(1, 2, "hello");
            VA<string> value2 = new VA<string>(1, 2, "hello");

            Assert.IsTrue(value1 == "hello");
            Assert.IsTrue("hello" == value1);
            Assert.IsTrue(value1 == value2);
        }
    }
}
