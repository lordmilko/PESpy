using System.IO;

namespace PESpy.Tests
{
    internal readonly ref struct TempFile
    {
        public string FileName { get; }

        static TempFile()
        {
            //Cleanup any old files
            var dir = Path.Combine(Path.GetTempPath(), "PESpyTest");

            if (Directory.Exists(dir))
            {
                var files = Directory.EnumerateFiles(dir);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static TempFile New()
        {
            return new TempFile(Path.Combine(Path.GetTempPath(), $"PESpyTest", Path.GetRandomFileName()));
        }

        private TempFile(string fileName)
        {
            FileName = fileName;
        }

        public static implicit operator string(TempFile file) => file.FileName;

        public void Dispose()
        {
            File.Delete(FileName);
        }
    }
}
