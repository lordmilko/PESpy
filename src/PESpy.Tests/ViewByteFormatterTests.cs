using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;
using ReFlow.FileAnalysis;

namespace PESpy.Tests
{
    [TestClass]
    public class ViewByteFormatterTests
    {
        private static PEFileAccessor accessor;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var peFile = PEFile.FromKey(WellKnownTestModule.ntdll);

            accessor = (PEFileAccessor) FileAnalyzer.Analyze(peFile, IntelFileDisassembler.Instance);
        }

        [TestMethod]
        public void ViewByteFormatter_Struct_TopLevelOnly()
        {
            var formatter = new ViewByteFormatter(accessor);

            TestStart(
                0,
                int.MaxValue,
                formatter,
                "HEADER:0000         IMAGE_DOS_HEADER\n"
            );
        }

        [TestMethod]
        public void ViewByteFormatter_Struct_MoveNextToFirstField()
        {
            var formatter = new ViewByteFormatter(accessor);

            SkipStart(0, formatter);

            TestMoveNext(
                formatter,
                "HEADER:0000             e_magic    = 5A4D\n"
            );
        }

        [TestMethod]
        public void ViewByteFormatter_Struct_MoveNextToLastField()
        {
            var formatter = new ViewByteFormatter(accessor);

            SkipStart(0, formatter);
            SkipMoveNext(18, formatter);

            TestMoveNext(
                formatter,
                "HEADER:003C             e_lfanew   = 0xE0\n"
            );
        }

        [TestMethod]
        public void ViewByteFormatter_Struct_MoveNextToCodeGlobal_OneLineNeeded()
        {
            //It's the last field in the struct, and there's no more items
            //in the stack, so the next item is global

            var formatter = new ViewByteFormatter(accessor);

            SkipStart(0, formatter, numLinesNeeded: 21);
            SkipMoveNext(19, formatter);

            //Code is going to start blasting through lines needed, so we need it to stop
            //after writing just one line

            //We get more lines than we needed here, because we also had to write the header
            TestMoveNext(
                formatter,
                "HEADER:0040         DOS Stub\nHEADER:0040             push    cs\n"
            );
        }

        [TestMethod]
        public void ViewByteFormatter_Code_Resume()
        {
            var formatter = new ViewByteFormatter(accessor);

            SkipStart(0, formatter, numLinesNeeded: 22); //The first DOS Stub line also has a header
            SkipMoveNext(21, formatter);

            formatter.RequestLines(1);

            TestMoveNext(
                formatter,
                "HEADER:0041                 pop     ds\n"
            );
        }

        [TestMethod]
        public void ViewByteFormatter_Code_Start()
        {
            var formatter = new ViewByteFormatter(accessor);

            TestStart(
                0x40,
                3,
                formatter,
                "HEADER:0040         DOS Stub\nHEADER:0040             push    cs\nHEADER:0041             pop     ds\n"
            );
        }
}
