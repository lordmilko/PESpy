using System;
using System.Diagnostics;
using System.IO;

namespace PESpy
{
    public enum SymStoreKeyKind
    {
        PE,
        PDB,
        DBG,

        CLR,
        DAC,
        DBI
    }

    [DebuggerDisplay("[{Kind}] {Index.ToString(),nq}")]
    public readonly struct SymStoreKey
    {
        /// <summary>
        /// Creates a <see cref="SymStoreKey"/> around the identity of a PE File.
        /// </summary>
        /// <param name="name">The basename of the file with file extension</param>
        /// <param name="timeDateStamp">The value of <see cref="ImageFileHeader.TimeDateStamp"/></param>
        /// <param name="sizeOfImage">The value of <see cref="ImageOptionalHeader.SizeOfImage"/></param>
        /// <param name="kind">The kind of key to create.</param>
        /// <returns>A <see cref="SymStoreKey"/> that identifies the PE File on a symbol server.</returns>
        public static SymStoreKey FromPE(string name, uint timeDateStamp, int sizeOfImage, SymStoreKeyKind kind = SymStoreKeyKind.PE)
        {
            var lowerName = name.ToLowerInvariant();

            var index = $"{lowerName}/{timeDateStamp:X8}{sizeOfImage:x}/{lowerName}";

            return new SymStoreKey(index, kind);
        }

        public static SymStoreKey FromRSDSI(RSDSI rsds) => FromRSDSI(rsds.Path.ToString(), rsds.Guid, rsds.Age);

        public static SymStoreKey FromRSDSI(string name, Guid guid, int age)
        {
            //SymSrv seems to use uppercase GUIDs, and apparently certain symbol servers only support uppercase

            name = Path.GetFileName(name); //symsrv doesn't seem to modify the case. It could potentially be the full path of the original file location
            var index = $"{name}/{guid.ToString("N").ToUpperInvariant()}{age:X}/{name}";

            return new SymStoreKey(index, SymStoreKeyKind.PDB);
        }

        public static SymStoreKey FromNB10(NB10I nb10) => FromNB10(nb10.Path.ToString(), nb10.PdbSignature, nb10.Age);

        public static SymStoreKey FromNB10(string name, uint pdbSignature, int age)
        {
            name = Path.GetFileName(name);

            var index = $"{name}/{pdbSignature:X}{age:X}/{name}";

            return new SymStoreKey(index, SymStoreKeyKind.PDB);
        }

        /// <summary>
        /// Creates a <see cref="SymStoreKey"/> around the identity of a DBG File.
        /// </summary>
        /// <param name="name">The name of the file pointed to by <see cref="ImageDebugMisc.Data"/></param>
        /// <param name="timeDateStamp">The value of <see cref="ImageFileHeader.TimeDateStamp"/></param>
        /// <param name="sizeOfImage">The value of <see cref="ImageOptionalHeader.SizeOfImage"/></param>
        /// <returns>A <see cref="SymStoreKey"/> that identifies the DBG File on a symbol server.</returns>
        public static SymStoreKey FromMisc(string name, uint timeDateStamp, int sizeOfImage)
        {
            //I've seen cases where the ImageDebugMisc pointed to an exe, and also had a path in it. e.g. Debug/TestApp.exe
            //But it's possible it was the dbg file that had an ImageDebugMisc like that, not the exe

            var index = $"{name}/{timeDateStamp:X8}{sizeOfImage:x}/{name}";

            return new SymStoreKey(index, SymStoreKeyKind.DBG);
        }

        public string Index { get; }

        public SymStoreKeyKind Kind { get; }

        public SymStoreKey(string index, SymStoreKeyKind kind)
        {
            Index = index;
            Kind = kind;
        }
    }
}
