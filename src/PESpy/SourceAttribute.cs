using System;
using ClrDebug.PDB;
using PESpy.Native;

namespace PESpy
{
    //This should be flags because certain items are defined in different ways in different sources
    //e.g. nsg
    [Flags]
    internal enum SourceKind : uint
    {
        /// <summary>
        /// Represents a type made up by PESpy for the purposes of representing a type without a well known native type definition,
        /// or encapsulating several values that do not have a well known enclosing type definition.
        /// </summary>
        Synthetic = 1 << 0,

        /// <summary>
        /// Represents a type defined in <c>winnt.h</c>, which describes the main structures found in the PE File Format, including <see cref="IMAGE_DOS_HEADER"/> and its associated types.
        /// </summary>
        winnt_h = 1 << 1,

        /// <summary>
        /// Represents a type defined in <c>cvexefmt.h</c> which describes the OMF related structures like <see cref="OMFDirEntry"/> accepted by CodeView 4.0 and later
        /// </summary>
        cvexefmt_h = 1 << 2,

        /// <summary>
        /// Represents a type defined by the Microsoft C 6.0 Developer's Toolkit, included as part of the Microsoft Programmer's Library 1.3 CD-ROM,
        /// and listed online at https://www.pcjs.org/documents/books/mspl13/c/ctoolkit/
        /// </summary>
        C6DevToolkit = 1 << 3,

        /// <summary>
        /// Represents a type defined in <c>cvinfo.h</c> where <see cref="SYMTYPE"/>, <see cref="TYPTYPE"/> and their associated types.
        /// </summary>
        cvinfo_h = 1 << 4,

        /// <summary>
        /// Represents a type defined in <c>corcompile.h</c> which describes NGEN related data structures.
        /// </summary>
        corcompile_h = 1 << 5,

        /// <summary>
        /// Represents a type defined in <c>corbbtprof.h</c> which describes NGEN profiling related data structures.
        /// </summary>
        corbbtprof_h = 1 << 6,

        /// <summary>
        /// Represents a type defined in <c>corinfo.h</c> which describes CLR data structures related to generating native code.
        /// </summary>
        corinfo_h = 1 << 7,

        /// <summary>
        /// Represents a type defined in <c>mapsym.h</c> which describes the data structures used by mapsym.exe
        /// </summary>
        mapsym_h = 1 << 8,

        /// <summary>
        /// Represents a type defined in <c>mdfileformat.h</c> which defines the data structures described
        /// in EMCA-335 II.24.2, relating to the top level data structures for ECMA-335 metadata.
        /// </summary>
        mdfileformat_h = 1 << 9,

        /// <summary>
        /// Represents a type defined in <c>dbgenginemetrics.h</c> which defines the CLR_ENGINE_METRICS type
        /// used by debuggers principally to locate the g_hContinueStartupEvent inside the target CLR module.
        /// </summary>
        dbgenginemetrics_h = 1 << 10,

        /// <summary>
        /// Represents a type defined in <c>exehdr.h</c> which defines the DOS executable header struct format.
        /// </summary>
        exehdr_h = 1 << 11,

        /// <summary>
        /// Represents a type defined in <c>newexe.h</c> which defines the New Executable (NE) header struct format.
        /// </summary>
        newexe_h = 1 << 12,

        /// <summary>
        /// Represents a type defined in <c>msf.cpp</c> which implements the PDB Multistream File (MSF) format<para/>
        /// https://github.com/microsoft/microsoft-pdb/blob/master/PDB/msf/msf.cpp
        /// </summary>
        msf_cpp = 1 << 13,

        /// <summary>
        /// Represents a type defined in <c>ehdata4_export.h</c> which defines the format of __CxxFrameHandler4 metadata.
        /// </summary>
        ehdata4_export_h = 1 << 14,
    }

    /// <summary>
    /// Specifies the location that a well known struct definition came from.
    /// </summary>
    internal class SourceAttribute : Attribute
    {
        public SourceKind Kind { get; }

        internal SourceAttribute(SourceKind kind)
        {
            Kind = kind;
        }
    }
}
