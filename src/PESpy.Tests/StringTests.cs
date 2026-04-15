using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    [TestClass]
    public class StringTests
    {
        private static SymString SymStringLengthPrefixed;
        private static SymString SymStringNullTerminated;
        private static SymString SymStringNull;

        private static AnsiString AnsiString;
        private static AnsiString AnsiStringNull;

        private static Utf8String Utf8String;
        private static Utf8String Utf8StringNull;

        private static Utf16String Utf16String;
        private static Utf16String Utf16StringNull;

        private static FixedAnsiString FixedAnsiString;
        private static FixedAnsiString FixedAnsiStringNull;

        private static FixedUtf8String FixedUtf8String;
        private static FixedUtf8String FixedUtf8StringNull;

        private static FixedUtf16String FixedUtf16String;
        private static FixedUtf16String FixedUtf16StringNull;

        public static NullTerminatedString NullTerminatedString_Ansi;
        public static NullTerminatedString NullTerminatedString_UTF8;
        public static NullTerminatedString NullTerminatedString_UTF16;
        public static NullTerminatedString NullTerminatedString_Null;

        private static readonly string equalsStr;
        private static readonly string startStr;
        private static readonly string endStr;
        private static readonly string containsStr;
        private static readonly string randomStr;

        static unsafe StringTests()
        {
            equalsStr = "FooBarBaz";
            startStr = "Foo";
            endStr = "Baz";
            containsStr = "Bar";
            randomStr = "random";

            var ansiStr = (byte*) Marshal.StringToHGlobalAnsi(equalsStr);
            var wideStr = (char*) Marshal.StringToHGlobalUni(equalsStr);

            var lengthPrefixed = Marshal.AllocHGlobal(equalsStr.Length + 1);
            *((byte*) lengthPrefixed) = (byte) equalsStr.Length;
            new Span<byte>(ansiStr, equalsStr.Length).CopyTo(new Span<byte>(((byte*) lengthPrefixed) + 1, equalsStr.Length));

            SymStringLengthPrefixed = new SymString(((byte*) lengthPrefixed) + 1, true);
            SymStringNullTerminated = new SymString(ansiStr, false);
            SymStringNull = default;

            AnsiString = new AnsiString(ansiStr);
            AnsiStringNull = default;

            Utf8String = new Utf8String(ansiStr);
            Utf8StringNull = default;

            Utf16String = new Utf16String(wideStr);
            Utf16StringNull = default;

            FixedAnsiString = new FixedAnsiString(ansiStr, equalsStr.Length);
            FixedAnsiStringNull = default;

            FixedUtf8String = new FixedUtf8String(ansiStr, equalsStr.Length);
            FixedUtf8StringNull = default;

            FixedUtf16String = new FixedUtf16String(wideStr, equalsStr.Length);
            FixedUtf16StringNull = default;

            NullTerminatedString_Ansi = new NullTerminatedString(ansiStr, StringKind.ANSI);
            NullTerminatedString_UTF8 = new NullTerminatedString(ansiStr, StringKind.UTF8);
            NullTerminatedString_UTF16 = new NullTerminatedString((byte*) wideStr, StringKind.UTF16);
            NullTerminatedString_Null = default;
        }

        [TestMethod]
        public void String_SymString_LengthPrefixed_Tests() => TestString<SymString, byte>(SymStringLengthPrefixed, SymStringNull);

        [TestMethod]
        public void String_SymString_NullTerminated_Tests() => TestString<SymString, byte>(SymStringNullTerminated, SymStringNull);

        [TestMethod]
        public void String_AnsiString_Tests() => TestString<AnsiString, byte>(AnsiString, AnsiStringNull);

        [TestMethod]
        public void String_Utf8String_Tests() => TestString<Utf8String, byte>(Utf8String, Utf8StringNull);

        [TestMethod]
        public void String_Utf16String_Tests() => TestString<Utf16String, char>(Utf16String, Utf16StringNull);

        [TestMethod]
        public void String_FixedAnsiString_Tests() => TestString<FixedAnsiString, byte>(FixedAnsiString, FixedAnsiStringNull);

        [TestMethod]
        public void String_FixedUtf8String_Tests() => TestString<FixedUtf8String, byte>(FixedUtf8String, FixedUtf8StringNull);

        [TestMethod]
        public void String_FixedUtf16String_Tests() => TestString<FixedUtf16String, char>(FixedUtf16String, FixedUtf16StringNull);

        [TestMethod]
        public void String_NullTerminatedString_Ansi_Tests() => TestString<NullTerminatedString, byte>(NullTerminatedString_Ansi, NullTerminatedString_Null);

        [TestMethod]
        public void String_NullTerminatedString_UTF8_Tests() => TestString<NullTerminatedString, byte>(NullTerminatedString_UTF8, NullTerminatedString_Null);

        [TestMethod]
        public void String_NullTerminatedString_UTF16_Tests() => TestString<NullTerminatedString, byte>(NullTerminatedString_UTF16, NullTerminatedString_Null);

        private void TestString<TString, TChar>(
            TString str, TString nullStr) where TString : IString<TString, TChar>
        {
            //Equality of strings with null ptrs inside
            Assert.IsTrue(nullStr.Equals(nullStr), "NullStr <-> NullStr failed");
            Assert.IsTrue(nullStr.Equals(null), "NullStr <-> null failed");
            Assert.IsTrue(((object) nullStr).Equals((object) nullStr), "NullStr <-> (object) NullStr failed");
            
            //Length
            Assert.AreEqual(equalsStr.Length, str.Length, "Length failed");
            Assert.AreEqual(0, nullStr.Length, "Null length failed");

            //Equality of strings with null ptrs not inside
            Assert.IsFalse(str.Equals(nullStr), "Str <-> NullStr failed");
            Assert.IsFalse(str.Equals(null), "Str <-> null failed");

            //StartsWith
            Assert.IsTrue(str.StartsWith(startStr));
            Assert.IsFalse(str.StartsWith(endStr));

            //EndsWith
            Assert.IsTrue(str.EndsWith(endStr));
            Assert.IsFalse(str.EndsWith(startStr));

            //Contains
            Assert.IsTrue(str.Contains(containsStr));
            Assert.IsFalse(str.Contains(randomStr));

            //Equals
            Assert.IsTrue(str.Equals(str), "Str <-> Str failed");
            Assert.IsFalse(str.Equals(randomStr), "Str <-> RandomStr failed");
            Assert.IsTrue(str.Equals(equalsStr), "Str <-> EqualsStr failed");
        }
    }
}
