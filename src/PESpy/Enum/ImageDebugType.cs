using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_DEBUG_TYPE_* enumeration which specifies the type of an <see cref="IMAGE_DEBUG_DIRECTORY"/>.
    /// </summary>
    public enum ImageDebugType
    {
        /// <summary>
        /// An unknown value that is ignored by all tools.<para/>
        /// IMAGE_DEBUG_TYPE_UNKNOWN
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_COFF<para/>
        /// The COFF debug information (line numbers, symbol table, and string table).
        /// This type of debug information is also pointed to by fields in the file headers.
        /// </summary>
        Coff = 1,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_CODEVIEW<para/>
        /// Associated PDB file description.<para/>
        /// Data is a structure whose type is determined by the CodeView signature. Typically <see cref="RSDSI"/> or <see cref="NB10I"/>.
        /// </summary>
        /// <remarks>
        /// See https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#codeview-debug-directory-entry-type-2 for specification.
        /// </remarks>
        CodeView = 2,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_FPO<para/>
        /// The frame pointer omission (FPO) information. This information tells the debugger how to interpret nonstandard stack frames,
        /// which use the EBP register for a purpose other than as a frame pointer.<para/>
        /// Data is a <see cref="FpoData"/>[]
        /// </summary>
        FPO = 3,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_MISC<para/>
        /// The location of DBG file.<para/>
        /// Data is an <see cref="ImageDebugMisc"/>
        /// </summary>
        Misc = 4,

        /// <summary>
        ///IMAGE_DEBUG_TYPE_EXCEPTION<para/>
        /// A copy of .pdata section.
        /// </summary>
        Exception = 5,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_FIXUP<para/>
        /// Reserved.
        /// </summary>
        Fixup = 6,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_OMAP_TO_SRC<para/>
        /// The mapping from an RVA in image to an RVA in source image.
        /// </summary>
        OmapToSrc = 7,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_OMAP_FROM_SRC<para/>
        /// The mapping from an RVA in source image to an RVA in image.
        /// </summary>
        OmapFromSrc = 8,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_BORLAND<para/>
        /// Reserved for Borland.
        /// </summary>
        Borland = 9,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_RESERVED10<para/>
        /// Reserved.
        /// </summary>
        Reserved10 = 10,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_BBT
        /// </summary>
        BBT = Reserved10,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_CLSID<para/>
        /// Reserved.
        /// </summary>
        Clsid = 11,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_VC_FEATURE<para/>
        /// Data is a <see cref="VCFeature"/>.
        /// </summary>
        VCFeature = 12,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_POGO<para/>
        /// Data is a <see cref="PogoData"/>
        /// </summary>
        Pogo = 13,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_ILTCG
        /// </summary>
        ILTCG = 14,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_MPX
        /// </summary>
        MPX = 15,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_REPRO<para/>
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
        Reproducible = 16, //The documentation says it's empty, but that's not true!

        /// <summary>
        /// IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB<para/>
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
        EmbeddedPortablePdb = 17,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_SPGO
        /// </summary>
        SPGO = 18,

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
        PdbChecksum = 19,

        /// <summary>
        /// IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS<para/>
        /// Extended DLL characteristics bits.<para/>
        /// Data is a <see cref="ImageDllCharacteristicsEx"/> (which PESpy wraps in a <see cref="RawValue{T}"/>).
        /// </summary>
        ExDllCharacteristics = 20,

        //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#r2r-perfmap-debug-directory-entry-type-21
        R2RPerfMap = 21
    }
}
