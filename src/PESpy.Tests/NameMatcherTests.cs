using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    [TestClass]
    public class NameMatcherTests
    {
        [TestMethod]
        public void NameMatcher_EndsWithIgnoreCaseNameMatcher()
        {
            var nameMatcher = NameMatcher.Create("*foo");
            Assert.IsInstanceOfType(nameMatcher, typeof(NameMatcher.EndsWithIgnoreCaseNameMatcher));

            AssertTrue("FOO", nameMatcher.IsMatch);
            AssertTrue("1FOO", nameMatcher.IsMatch);
            AssertFalse("FOO1", nameMatcher.IsMatch);
            AssertFalse("FO", nameMatcher.IsMatch);
        }

        [TestMethod]
        public void NameMatcher_ContainsIgnoreCaseNameMatcher()
        {
            var nameMatcher = NameMatcher.Create("*foo*");
            Assert.IsInstanceOfType(nameMatcher, typeof(NameMatcher.ContainsIgnoreCaseNameMatcher));

            AssertTrue("FOO", nameMatcher.IsMatch);
            AssertTrue("1FOO", nameMatcher.IsMatch);
            AssertTrue("FOO1", nameMatcher.IsMatch);
            AssertTrue("1FOO1", nameMatcher.IsMatch);
            AssertFalse("FO", nameMatcher.IsMatch);
        }

        [TestMethod]
        public unsafe void NameMatcher_RegexNameMatcher()
        {
            var nameMatcher = NameMatcher.Create("*foo*bar*");
            Assert.IsInstanceOfType(nameMatcher, typeof(NameMatcher.RegexNameMatcher));

            AssertTrue("FOOBAZBARFOO", nameMatcher.IsMatch);
            AssertFalse("FOBAZBARFOO", nameMatcher.IsMatch);
        }

        [TestMethod]
        public void NameMatcher_StartsWithIgnoreCaseNameMatcher()
        {
            var nameMatcher = NameMatcher.Create("foo*");
            Assert.IsInstanceOfType(nameMatcher, typeof(NameMatcher.StartsWithIgnoreCaseNameMatcher));

            AssertTrue("FOO", nameMatcher.IsMatch);
            AssertTrue("FOO1", nameMatcher.IsMatch);
            AssertFalse("1FOO", nameMatcher.IsMatch);
            AssertFalse("FO", nameMatcher.IsMatch);
        }

        [TestMethod]
        public void NameMatcher_IgnoreCaseNameMatcher()
        {
            var nameMatcher = NameMatcher.Create("foo");
            AssertTrue("FOO", nameMatcher.IsMatch);
            AssertFalse("FO", nameMatcher.IsMatch);
            AssertFalse("FOOO", nameMatcher.IsMatch);
        }

        private unsafe void AssertTrue(string str, Func<SymString, bool> validate)
        {
            var ptr = Marshal.StringToHGlobalAnsi(str);

            try
            {
                var s = new SymString((byte*) ptr, false);

                Assert.IsTrue(validate(s));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private unsafe void AssertFalse(string str, Func<SymString, bool> validate)
        {
            var ptr = Marshal.StringToHGlobalAnsi(str);

            try
            {
                var s = new SymString((byte*) ptr, false);

                var result = validate(s);

                Assert.IsFalse(result);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
