using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    [TestClass]
    public class StringParserTests
    {
        [TestMethod]
        public unsafe void StringParser_Ansi_NullTerminated()
        {
            var bytes = new byte[]
            {
                0x4B, 0x45, 0x52, 0x4E, 0x45, 0x4C, 0x33, 0x32, 0x2E, 0x64, 0x6C, 0x6C, 0x00, 0x42, 0x61, 0x73, 0x65, 0x54, 0x68, 0x72, 0x65, 0x61,
                0x64, 0x49, 0x6E, 0x69, 0x74, 0x54, 0x68, 0x75, 0x6E, 0x6B, 0x00, 0x49, 0x6E, 0x74, 0x65, 0x72, 0x6C, 0x6F, 0x63, 0x6B, 0x65, 0x64,
                0x50, 0x75, 0x73, 0x68, 0x4C, 0x69, 0x73, 0x74, 0x53, 0x4C, 0x69, 0x73, 0x74, 0x00
            };

            fixed (byte* p = bytes)
            {
                var strs = StringParser.GetStrings(p, bytes.Length);

                Assert.AreEqual(3, strs.Length);
                Assert.AreEqual("KERNEL32.dll", strs[0].ToString());
                Assert.AreEqual("BaseThreadInitThunk", strs[1].ToString());
                Assert.AreEqual("InterlockedPushListSList", strs[2].ToString());
            }
        }

        [TestMethod]
        public unsafe void StringParser_UTF16_NullTerminated()
        {
            var bytes = new byte[]
            {
                0x4b, 0x00, 0x45, 0x00, 0x52, 0x00, 0x4e, 0x00, 0x45, 0x00, 0x4c, 0x00, 0x33, 0x00, 0x32, 0x00, 0x2e, 0x00, 0x64, 0x00, 0x6c, 0x00, 0x6c, 0x00, 0x00, 0x00
            };

            fixed (byte* p = bytes)
            {
                var str = StringParser.GetStrings(p, bytes.Length);
                Assert.AreEqual(1, str.Length);
                Assert.AreEqual("KERNEL32.dll", str[0].ToString());
                Assert.AreEqual(26, str[0].Length);
            }
        }

        [TestMethod]
        public unsafe void StringParser_GarbageAnsi_Then_UTF16_NullTerminated()
        {
            /* Consider a sequence with the following bytes
             *     D <- garbage
             *     k <- garbage
             *     c <- real unicode string begin
             *    \0
             *     a
             *    \0
             *     t
             *    \0
             *    \0
             *    \0
             */

            var bytes = new byte[]
            {
                0x82, 0x6B, 0x53, 0x00, 0x58, 0x00, 0x53, 0x00, 0x4D, 0x00, 0x61, 0x00, 0x6E, 0x00, 0x69, 0x00, 0x66, 0x00, 0x65, 0x00, 0x73, 0x00, 0x74, 0x00, 0x00, 0x00
            };

            fixed (byte* p = bytes)
            {
                var str = StringParser.GetStrings(p, bytes.Length);

                Assert.AreEqual(1, str.Length);
                Assert.AreEqual("SXSManifest", str[0].ToString());
                Assert.AreEqual(24, str[0].Length);
            }
        }
    }
}
