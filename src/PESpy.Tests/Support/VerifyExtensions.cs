using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    internal static class VerifyExtensions
    {
        internal static void Verify(this MockTextLine[] lines, params string[] expected)
        {
            Assert.AreEqual(expected.Length, lines.Length);

            for (var i = 0; i < lines.Length; i++)
            {
                var expectedStr = expected[i];
                var actual = lines[i];

                //For the purposes of this test, ignore meaning strings

                var chars = new List<char>
                {
                    ';'
                };

                if (!expectedStr.Contains('(')) //We want to trim the meaning of fields, but struct fields inherently have the struct name in them
                    chars.Add('(');

                var comma = actual.Text.LastIndexOfAny(chars.ToArray());

                if (comma != -1)
                {
                    var newText = actual.Text.Substring(0, comma).TrimEnd();

                    actual = new MockTextLine(newText, actual.Left, actual.Top, actual.Right, actual.Height);
                }

                var actualStr = actual.ToString();

                Assert.AreEqual(expectedStr, actualStr);
            }
        }

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
