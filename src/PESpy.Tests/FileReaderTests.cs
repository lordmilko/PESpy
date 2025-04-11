using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    [TestClass]
    public class FileReaderTests
    {
        [TestMethod]
        public void FileReader_ReadArray_LargerThanBuffer()
        {
            var numbers = Enumerable.Range(1, 100).ToArray();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            foreach (var number in numbers)
                writer.Write(number);

            stream.Seek(0, SeekOrigin.Begin);

            var reader = new StreamFileReader(stream, true);
            var actual = reader.ReadArray<int>(numbers.Length);

            Assert.AreEqual(numbers.Length, actual.Length);

            for (var i = 0; i < numbers.Length; i++)
                Assert.AreEqual(numbers[i], actual[i]);
        }
    }
}
