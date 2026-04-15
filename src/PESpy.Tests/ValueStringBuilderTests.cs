using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    [TestClass]
    public class ValueStringBuilderTests
    {
        [TestMethod]
        public void ValueStringBuilder_Replace()
        {
            using var builder = new ValueStringBuilder();

            builder.Append("abc");
            builder.Replace("b".AsSpan(), "z".AsSpan(), 0, 3);
            Assert.AreEqual("azc", builder.ToString());

            builder.Clear();
            builder.Append("abcbd");
            builder.Replace("b".AsSpan(), "yz".AsSpan(), 0, 5);
            Assert.AreEqual("ayzcyzd", builder.ToString());
        }
    }
}
