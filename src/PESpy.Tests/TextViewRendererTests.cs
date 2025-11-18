using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;
using ReFlow.FileAnalysis;

namespace PESpy.Tests
{
    [TestClass]
    public class TextViewRendererTests
    {
        private static PEFileAccessor fileAccessor;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            fileAccessor = (PEFileAccessor) FileAnalyzer.Analyze(PEFile.FromKey(WellKnownTestModule.ntdll), IntelFileDisassembler.Instance);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            fileAccessor?.Dispose();
        }

        [TestMethod]
        public void TextViewRenderer_Struct_InitialFields()
        {
            //Draw a simple struct and some of its fields

            var graphics = new MockGraphics();
            var renderer = new TextViewRenderer(graphics, fileAccessor, 800, 100, 0);

            renderer.Paint(default);

            var lines = graphics.ScreenLines;

            lines.Verify(
                "[0-20] HEADER:0000         IMAGE_DOS_HEADER",
                "[20-40] HEADER:0000             e_magic                        = 0x5A4D",
                "[40-60] HEADER:0002             e_cblp                         = 0x0090",
                "[60-80] HEADER:0004             e_cp                           = 0x0003",
                "[80-100] HEADER:0006             e_crlc                         = 0x0000"
            );
        }

        [TestMethod]
        public void TextViewRenderer_Struct_InitialFields_ScrollDown_EntireLogicalLines()
        {
            //Draw the initial fields of the struct, and then scroll down.
            //Additional fields should show

            var graphics = new MockGraphics();
            var renderer = new TextViewRenderer(graphics, fileAccessor, 800, 100, 0);

            //Render the initial lines
            renderer.Paint(default);

            //The top 3 lines should disappear off screen
            renderer.ScrollLinesDown(3);

            var lines = graphics.ScreenLines;

            lines.Verify(
                "[0-20] HEADER:0004             e_cp                           = 0x0003",
                "[20-40] HEADER:0006             e_crlc                         = 0x0000",
                "[40-60] HEADER:0008             e_cparhdr                      = 0x0004",
                "[60-80] HEADER:000A             e_minalloc                     = 0x0000",
                "[80-100] HEADER:000C             e_maxalloc                     = 0xFFFF"
            );

            //Our bookkeeping should have been updated correctly
            var firstLogicalLine = renderer.LogicalLines[0];
            Assert.AreEqual("HEADER:0004             e_cp                           = 0x0003 (3)\n", firstLogicalLine.ToString());
        }

        [TestMethod]
        public void TextViewRenderer_Struct_InitialFields_ScrollDown_ThenUp()
        {
            var graphics = new MockGraphics();
            var renderer = new TextViewRenderer(graphics, fileAccessor, 800, 100, 0);

            //Render the initial lines
            renderer.Paint(default);

            //The top 3 lines should disappear off screen
            renderer.ScrollLinesDown(3);

            //Those 3 lines should now come back
            renderer.ScrollLinesUp(3);

            var lines = graphics.ScreenLines;

            lines.Verify(
                "[0-20] HEADER:0000         IMAGE_DOS_HEADER",
                "[20-40] HEADER:0000             e_magic                        = 0x5A4D",
                "[40-60] HEADER:0002             e_cblp                         = 0x0090",
                "[60-80] HEADER:0004             e_cp                           = 0x0003",
                "[80-100] HEADER:0006             e_crlc                         = 0x0000"
            );

            //Our bookkeeping should have been updated correctly
            var firstLogicalLine = renderer.LogicalLines[0];
            Assert.AreEqual("HEADER:0000         IMAGE_DOS_HEADER\n", firstLogicalLine.ToString());
        }

        [TestMethod]
        public void TextViewRenderer_Struct_Goto_Start()
        {
            TestGoto(
                0xE0,
                "[0-20] HEADER:00E0         IMAGE_NT_HEADERS",
                "[20-40] HEADER:00E0             Signature                      = 0x00004550",
                "[40-60] HEADER:00E4             FileHeader (IMAGE_FILE_HEADER)",
                "[60-80] HEADER:00E4                 Machine                        = 0x8664",
                "[80-100] HEADER:00E6                 NumberOfSections               = 0x000B"
            );
        }

        [TestMethod]
        public void TextViewRenderer_Struct_Goto_FirstField()
        {
            //The first field shares its address with its parent struct (or in this case, struct field)
            //so both should be listed
            TestGoto(
                0xE4,
                "[0-20] HEADER:00E4             FileHeader (IMAGE_FILE_HEADER)",
                "[20-40] HEADER:00E4                 Machine                        = 0x8664",
                "[40-60] HEADER:00E6                 NumberOfSections               = 0x000B",
                "[60-80] HEADER:00E8                 TimeDateStamp                  = 0xBCED4B82",
                "[80-100] HEADER:00EC                 PointerToSymbolTable           = 0x00000000"
            );
        }

        [TestMethod]
        public void TextViewRenderer_Struct_Goto_SecondField()
        {
            //We should skip past the start of the struct (both the header and first field) and start from the second field
            TestGoto(
                0xE6,
                "[0-20] HEADER:00E6                 NumberOfSections               = 0x000B",
                "[20-40] HEADER:00E8                 TimeDateStamp                  = 0xBCED4B82",
                "[40-60] HEADER:00EC                 PointerToSymbolTable           = 0x00000000",
                "[60-80] HEADER:00F0                 NumberOfSymbols                = 0x00000000",
                "[80-100] HEADER:00F4                 SizeOfOptionalHeader           = 0x00F0"
            );
        }

        [TestMethod]
        public void TextViewRenderer_Code_LogicalLines()
        {
            //Each subsequent instruction should be its own logical line

            TestGoto(
                0x1008,
                "[0-20] .text:001008 ---------------------------------------------------------------------------",
                "[20-40] .text:001008         LdrpGetModuleName",
                "[40-60] .text:001008             push    rbp",
                "[60-80] .text:00100A             push    rbx",
                "[80-100] .text:00100B             push    rsi"
            );
        }
        private void TestGoto(int targetAddress, string[] expected, Action<TextViewRenderer> verifyExtra)
        {
            var graphics = new MockGraphics();
            var renderer = new TextViewRenderer(graphics, fileAccessor, 800, 100, 0);

            //Render the initial lines
            renderer.Paint(default);

            renderer.Goto(targetAddress, false);

            var lines = graphics.ScreenLines;

            lines.Verify(expected);

            verifyExtra?.Invoke(renderer);
        }
}
