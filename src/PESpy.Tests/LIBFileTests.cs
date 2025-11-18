using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.LIB;

namespace PESpy.Tests
{
    [TestClass]
    public class LIBFileTests : BaseTest
    {
        [TestMethod]
        public void ImageArchiveMemberHeader_Test()
        {
            TestStruct<ImageArchiveMemberHeader>(
                v => v.Name == "/               ",
                v => v.Date == "-1          ",
                v => v.UserID == "      ",
                v => v.GroupID == "      ",
                v => v.Mode == "0       ",
                v => v.Size == 120,
                v => v.EndHeader == "`\n"
            );

            TestView<ImageArchiveMemberHeader>(
                v => v.VerifyStruct(
                    name: "IMAGE_ARCHIVE_MEMBER_HEADER", offset: 8, size: 60,
                    c => c.VerifyField(name: "Name", value: "/               "),
                    c => c.VerifyField(name: "Date", value: "-1          "),
                    c => c.VerifyField(name: "UserID", value: "      "),
                    c => c.VerifyField(name: "GroupID", value: "      "),
                    c => c.VerifyField(name: "Mode", value: "0       "),
                    c => c.VerifyField(name: "Size", value: "120       "),
                    c => c.VerifyField(name: "EndHeader", value: "`\n")
                )
            );
        }

        [TestMethod]
        public void FirstLinkerMember_Test()
        {
            TestStruct<FirstLinkerMember>(
                v => v.NumberOfSymbols == 5,
                v => v.Offsets == new int[] { 378, 932, 1242, 1580, 1580 },
                v => v.StringTable == IgnoreValue
            );

            TestView<FirstLinkerMember>(
                v => v.VerifyStruct(
                    name: "First Linker Member", offset: 8, size: 180,
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_ARCHIVE_MEMBER_HEADER", offset: 8, size: 60),
                    c => c.VerifyField(name: "Number Of Symbols", value: 5),
                    c => c.VerifyField(name: "Offsets", value: new int[] { 378, 932, 1242, 1580, 1580 }),
                    c => c.VerifyValue(offset: 0x5C, value: "__IMPORT_DESCRIPTOR_TestApp"),
                    c => c.VerifyValue(offset: 0x78, value: "__NULL_IMPORT_DESCRIPTOR"),
                    c => c.VerifyValue(offset: 0x91, value: "\u007fTestApp_NULL_THUNK_DATA"), //Null thunk data is a special symbol that is in the form \x7f <library name> _NULL_THUNK_DATA and is the terminator for the ILT and IAT
                    c => c.VerifyValue(offset: 0xAA, value: "__imp__main"),
                    c => c.VerifyValue(offset: 0xB6, value: "_main")
                )
            );
        }

        [TestMethod]
        public void SecondLinkerMember_Test()
        {
            Assert.Inconclusive();

            var str = GenerateTest<SecondLinkerMember>();

            throw new System.NotImplementedException();
        }

        [TestMethod]
        public void LongNamesMember_Test()
        {
            var str = GenerateTest<LongNamesMember>();

            throw new System.NotImplementedException();
        }
}
