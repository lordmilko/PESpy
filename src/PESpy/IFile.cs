using System;

namespace PESpy
{
    public enum FileKind
    {
        PE,
        NE,
        DBG,
        PDB,
        OBJ,
        LIB,
        LE
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
    }
}
