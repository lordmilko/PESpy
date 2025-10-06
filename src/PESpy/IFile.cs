using System;
using PESpy.View;

namespace PESpy
{
    public enum FileKind
    {
        /// <summary>
        /// A <see cref="PEFile"/> describing Portable Executable.
        /// </summary>
        PE,

        /// <summary>
        /// A <see cref="NEFile"/> describing a New Executable.
        /// </summary>
        NE,

        /// <summary>
        /// A <see cref="LEFile"/> describing a Linear Executable (used by VXDs).
        /// </summary>
        LE,

        /// <summary>
        /// A <see cref="DOSFile"/> describing an MS-DOS COFF executable.
        /// </summary>
        DOS,

        /// <summary>
        /// A <see cref="DBGFile"/> describing a COFF based debug file.
        /// </summary>
        DBG,

        /// <summary>
        /// A <see cref="PDBFile"/> describing a (Classic) Windows Program Database.
        /// </summary>
        PDB,

        /// <summary>
        /// A <see cref="PortablePDBFile"/> describing a Portable Program Database.
        /// </summary>
        PortablePDB,

        /// <summary>
        /// An <see cref="OBJFile"/> describing a COFF based object file.<para/>
        /// Also represents any COFF based file not better represented by any other <see cref="FileKind"/>
        /// (such as *.exp files)
        /// </summary>
        OBJ,

        /// <summary>
        /// A <see cref="LIBFile"/> describing a COFF based object library.
        /// </summary>
        LIB,

        /// <summary>
        /// An <see cref="OMFFile"/> describing an Object Module Format file.
        /// </summary>
        OMF,

        /// <summary>
        /// An <see cref="OMFLIBFile"/> describing an object library stored using Object Module Format.
        /// </summary>
        OMFLIB,
    }

    public interface IFile : IDisposable
    {
        /// <summary>
        /// Gets the name of the file including file extension.<para/>
        /// If the file was not opened from a file path, this value is <see langword="null"/>
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Gets the full path to the file<para/>
        /// If the file was not opened from a file path, this value is <see langword="null"/>
        /// </summary>
        string? FileName { get; }

        FileKind Kind { get; }

        int Length { get; }

        FileView GetView();
    }

    internal interface IFileWithCodeViewData
    {
        ICodeView? CodeViewData { get; }
    }
}
