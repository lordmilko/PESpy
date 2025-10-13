using System;
using ClrDebug.PDB;
using PESpy.Native;

namespace PESpy
{
    internal enum SourceKind
    {
        /// <summary>
        /// Represents a type made up by PESpy for the purposes of representing a type without a well known native type definition,
        /// or encapsulating several values that do not have a well known enclosing type definition.
        /// </summary>
        Synthetic,

        /// <summary>
        /// Represents a type defined in <c>winnt.h</c>, which describes the main structures found in the PE File Format, including <see cref="IMAGE_DOS_HEADER"/> and its associated types.
        /// </summary>
        winnt,

        /// <summary>
        /// Represents a type defined in <c>cvexefmt.h</c> which describes the OMF related structures like <see cref="OMFDirEntry"/> accepted by CodeView 4.0 and later
        /// </summary>
        cvexefmt,

        /// <summary>
        /// Represents a type defined in <c>cvinfo.h</c> where <see cref="SYMTYPE"/>, <see cref="TYPTYPE"/> and their associated types.
        /// </summary>
        cvinfo,

        /// <summary>
        /// Represents a type defined by the Microsoft C 6.0 Developer's Toolkit, included as part of the Microsoft Programmer's Library 1.3 CD-ROM,
        /// and listed online at https://www.pcjs.org/documents/books/mspl13/c/ctoolkit/
        /// </summary>
        C6DevToolkit
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
