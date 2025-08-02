using System;
using System.Linq;
using System.Reflection;
using ClrDebug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    [MTATestClass]
    public class PEFileViewTests
    {
        [TestMethod]
        public void PEFileView_PhysicalImage_StressTest_Calc()
        {
            //1 string and padding are being grouped together and it doesnt say a number. e.g. 0x656CF. fix this before the next issue
            //multiple strings are not being grouped together. e.g. 0x6585c onwards
            //debuggerdisplay of item 424 in sortedstructs throws a nullreferenceexception
            //todo: add a test that padding is a valid mechanism of starting a repeating type group for anything?
            Assert.Inconclusive();
        }

        [TestMethod]
        public void PEFileView_VirtualImage()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void AssertAllViewablePropertiesWritten()
        {
            var types = typeof(PEFile).Assembly.GetTypes().Where(t => !t.IsInterface && typeof(IViewable).IsAssignableFrom(t)).ToArray();

            foreach (var type in types)
            {
                var writeStructMethod  = typeof(ImageNtHeaders).GetMethod($"{typeof(IViewable).FullName}.{nameof(IViewable.WriteStruct)}", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(writeStructMethod);

                var il = ILDisassembler.Disassemble(writeStructMethod);

                var disp = new MetaDataDispenserEx();
                var mdi = disp.OpenScope<MetaDataImport>(typeof(ImageNtHeaders).Assembly.Location, CorOpenFlags.ofRead);

                var methodTokens = il.Select(v => v.Operand).OfType<mdToken>().Where(t => t.Type == CorTokenType.mdtMethodDef).ToArray();
                var actualProperties = methodTokens
                    .Select(m => mdi.GetMethodProps((mdMethodDef) m))
                    .Where(p => p.szMethod.StartsWith("get_"))
                    .Select(v => v.szMethod.Substring(4))
                    .ToArray();

                //todo: this should be limited to properties touched in the ctor, since there could be get-only properties?

                var expectedProperties = type.GetProperties().Select(p => p.Name).Where(v => v != nameof(IValue.Offset) && v != "RegionSize").ToArray();

                var missing = expectedProperties.Except(actualProperties).ToArray();

                if (missing.Length > 0)
                    throw new NotImplementedException();
            }
        }
    }
}
