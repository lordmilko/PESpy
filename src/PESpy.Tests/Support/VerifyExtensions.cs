using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    internal static class VerifyExtensions
    {
        internal static void Verify<T>(this T[] @this, params string[] expected)
        {
            Assert.AreEqual(expected.Length, @this.Length);

            for (var i = 0; i < @this.Length; i++)
            {
                var actual = @this[i];

                Assert.AreEqual(expected[i], actual.ToString());
            }
        }
    }
}
