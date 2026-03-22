using System;
using System.Diagnostics;
using System.IO;

namespace PESpy
{
    /// <summary>
    /// Specifies types of files that can be identified by a <see cref="SymStoreKey"/>.
    /// </summary>
    public enum SymStoreKeyKind
    {
        /// <summary>
        /// The identity of the <see cref="PEFile"/>. Microsoft Symbol Servers support the downloading of both image and symbol files
        /// to support dump debugging.
        /// </summary>
        PE,

        /// <summary>
        /// The identity of a <see cref="PDBFile"/> or <see cref="PortablePDBFile"/> that is associated with a <see cref="PEFile"/>.<para/>
        /// This identity is constructed using the true age listed in the <see cref="PEFile"/>. If a <see cref="PEFile"/> is compatible with
        /// Portable PDBs, either a <see cref="PDB"/> key or a <see cref="PortablePDB"/> key may be used to locate a file on the symbol server.
        /// Both keys may return results, and both results could either be a <see cref="PortablePDBFile"/>, or even a combination of a regular
        /// <see cref="PDBFile"/> and a <see cref="PortablePDBFile"/>.
        /// </summary>
        PDB, //Could point to either a PDB or Portable PDB

        /// <summary>
        /// A special case of identifying a <see cref="PortablePDBFile"/> where the <see cref="SymStoreKey"/> is constructed using
        /// an age of -1 (ffffffff) for the age, rather than the true age of the <see cref="PEFile"/>.
        /// </summary>
        PortablePDB, //Age is -1, we're expecting it should point to a Portable PDB

        /// <summary>
        /// The identity of a <see cref="DBGFile"/> associated with a <see cref="PEFile"/>.
        /// </summary>
        DBG,

        /// <summary>
        /// The identity of a coreclr.dll file that is described in the "DotNetRuntimeInfo" export of a single file app.
        /// </summary>
        CLR,

        /// <summary>
        /// The identity of a mscordaccore.dll file that is described in the "DotNetRuntimeInfo" export of a single file app.
        /// </summary>
        DAC,

        /// <summary>
        /// The identity of a mscordbi.dll file that is described in the "DotNetRuntimeInfo" export of a single file app.
        /// </summary>
        DBI
    }

    /// <summary>
    /// Represents the relative path to a file on a symbol store.
    /// </summary>
    [DebuggerDisplay("[{Kind}] {Index?.ToString(),nq}")]
    public readonly struct SymStoreKey : IEquatable<SymStoreKey>
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

        public static SymStoreKey FromPE(string fileName)
        {
            using var peFile = PEFile.FromFile(fileName);

            return FromPE(peFile.Name, peFile.FileHeader.TimeDateStamp, peFile.OptionalHeader.SizeOfImage);
        }

        public static SymStoreKey FromRSDSI(RSDSI rsds) => FromRSDSI(rsds.Path.ToString(), rsds.Guid, rsds.Age);

        public static SymStoreKey FromRSDSI(string name, Guid guid, int age, SymStoreKeyKind kind = SymStoreKeyKind.PDB)
        {
            //SymSrv seems to use uppercase GUIDs, and apparently certain symbol servers only support uppercase

            name = Path.GetFileName(name); //symsrv doesn't seem to modify the case. It could potentially be the full path of the original file location
            var index = $"{name}/{guid.ToString("N").ToUpperInvariant()}{age:X}/{name}";

            return new SymStoreKey(index, kind);
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

        /// <summary>
        /// Gets the index of the key that represents the relative path to the file on the symbol store.
        /// </summary>
        public string Index { get; }

        /// <summary>
        /// Gets the type of file identified by <see cref="Index"/>.
        /// </summary>
        public SymStoreKeyKind Kind { get; }

        public SymStoreKey(string index, SymStoreKeyKind kind)
        {
            Index = index;
            Kind = kind;
        }

        public static bool operator ==(SymStoreKey left, SymStoreKey right) => left.Index == right.Index;

        public static bool operator !=(SymStoreKey left, SymStoreKey right) => left.Index != right.Index;

        public bool Equals(SymStoreKey other)
        {
            //The key kind is just informational; if the index is the same, they match
            return Index.Equals(other.Index);
        }

        public override bool Equals(object obj)
        {
            if (obj is not SymStoreKey key)
                return false;

            return Equals(key);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public override string ToString()
        {
            return Index;
        }
    }
}
