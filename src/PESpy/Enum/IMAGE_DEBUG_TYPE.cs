using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_DEBUG_TYPE_* enumeration which specifies the type of an <see cref="IMAGE_DEBUG_DIRECTORY"/>.
    /// </summary>
    public enum IMAGE_DEBUG_TYPE
    {
        /// <summary>
        /// An unknown value that is ignored by all tools.
        /// </summary>
        IMAGE_DEBUG_TYPE_UNKNOWN = 0,

        /// <summary>
        /// The COFF debug information (line numbers, symbol table, and string table).
        /// This type of debug information is also pointed to by fields in the file headers.
        /// </summary>
        IMAGE_DEBUG_TYPE_COFF = 1,

        /// <summary>
        /// Associated PDB file description.<para/>
        /// Data is a structure whose type is determined by the CodeView signature. Typically <see cref="RSDSI"/> or <see cref="NB10I"/>.
        /// </summary>
        /// <remarks>
        /// See https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#codeview-debug-directory-entry-type-2 for specification.
        /// </remarks>
        IMAGE_DEBUG_TYPE_CODEVIEW = 2,

        /// <summary>
        /// The frame pointer omission (FPO) information. This information tells the debugger how to interpret nonstandard stack frames,
        /// which use the EBP register for a purpose other than as a frame pointer.<para/>
        /// Data is a <see cref="FpoData"/>[]
        /// </summary>
        IMAGE_DEBUG_TYPE_FPO = 3,

        /// <summary>
        /// The location of DBG file.<para/>
        /// Data is an <see cref="ImageDebugMisc"/>
        /// </summary>
        IMAGE_DEBUG_TYPE_MISC = 4,

        /// <summary>
        /// A copy of .pdata section.
        /// </summary>
        IMAGE_DEBUG_TYPE_EXCEPTION = 5,

        /// <summary>
        /// Reserved.
        /// </summary>
        IMAGE_DEBUG_TYPE_FIXUP = 6,

        /// <summary>
        /// The mapping from an RVA in image to an RVA in source image.
        /// </summary>
        IMAGE_DEBUG_TYPE_OMAP_TO_SRC = 7,

        /// <summary>
        /// The mapping from an RVA in source image to an RVA in image.
        /// </summary>
        IMAGE_DEBUG_TYPE_OMAP_FROM_SRC = 8,

        /// <summary>
        /// Reserved for Borland.
        /// </summary>
        IMAGE_DEBUG_TYPE_BORLAND = 9,

        /// <summary>
        /// Reserved.
        /// </summary>
        IMAGE_DEBUG_TYPE_RESERVED10 = 10,

        IMAGE_DEBUG_TYPE_BBT = IMAGE_DEBUG_TYPE_RESERVED10,

        /// <summary>
        /// Reserved.
        /// </summary>
        IMAGE_DEBUG_TYPE_CLSID = 11,

        /// <summary>
        /// Data is a <see cref="VCFeature"/>.
        /// </summary>
        IMAGE_DEBUG_TYPE_VC_FEATURE = 12,

        /// <summary>
        /// Data is a <see cref="PogoData"/>
        /// </summary>
        IMAGE_DEBUG_TYPE_POGO = 13,

        IMAGE_DEBUG_TYPE_ILTCG = 14,

        IMAGE_DEBUG_TYPE_MPX = 15,

        /// <summary>
        /// Presence of this entry indicates deterministic PE/COFF file.<para/>
        /// Data is a <see cref="Reproducible"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The tool that produced the deterministic PE/COFF file guarantees that the entire content of the file
        /// is based solely on documented inputs given to the tool (such as source files, resource files, compiler options, etc.)
        /// rather than ambient environment variables (such as the current time, the operating system,
        /// the bitness of the process running the tool, etc.).
        /// </para>
        /// <para>
        /// The value of field TimeDateStamp in COFF File Header of a deterministic PE/COFF file
        /// does not indicate the date and time when the file was produced and should not be interpreted that way.
        /// Instead the value of the field is derived from a hash of the file content. The algorithm to calculate
        /// this value is an implementation detail of the tool that produced the file.
        /// </para>
        /// <para>
        /// The debug directory entry of type <see cref="Reproducible"/> must have all fields, except for Type zeroed.
        /// </para>
        /// <para>
        /// See https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#deterministic-debug-directory-entry-type-16 for specification.
        /// </para>
        /// </remarks>
        IMAGE_DEBUG_TYPE_REPRO = 16, //The documentation says it's empty, but that's not true!

        /// <summary>
        /// The entry points to a blob containing Embedded Portable PDB.<para/>
        /// Data is a <see cref="PESpy.EmbeddedPortablePdb"/>
        /// </summary>
        /// <remarks>
        /// The Embedded Portable PDB blob has the following format:
        ///
        /// blob ::= uncompressed-size data
        ///
        /// Data spans the remainder of the blob and contains a Deflate-compressed Portable PDB.
        ///
        /// See https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#embedded-portable-pdb-debug-directory-entry-type-17 for specification.
        /// </remarks>
        IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB = 17,

        IMAGE_DEBUG_TYPE_SPGO = 18,

        /// <summary>
        /// The entry stores crypto hash of the content of the symbol file the PE/COFF file was built with.<para/>
        /// Data is a <see cref="PdbChecksum"/>.
        /// </summary>
        /// <remarks>
        /// The hash can be used to validate that a given PDB file was built with the PE/COFF file and not altered in any way.
        /// More than one entry can be present, in case multiple PDBs were produced during the build of the PE/COFF file (e.g. private and public symbols).
        ///
        /// See https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#pdb-checksum-debug-directory-entry-type-19 for specification.
        /// </remarks>
        IMAGE_DEBUG_TYPE_PDB_CHECKSUM = 19,

        /// <summary>
        /// Extended DLL characteristics bits.<para/>
        /// Data is a <see cref="IMAGE_DLLCHARACTERISTICS_EX"/> (which PESpy wraps in a <see cref="RawValue{T}"/>).
        /// </summary>
        IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS = 20,

        //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#r2r-perfmap-debug-directory-entry-type-21
        IMAGE_DEBUG_TYPE_R2R_PERFMAP = 21 //This name is guessed
    }
}
