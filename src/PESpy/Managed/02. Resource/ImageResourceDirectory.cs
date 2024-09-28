using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RESOURCE_DIRECTORY"/> structure. Immediately following this structure
    /// are <see cref="NumberOfNamedEntries"/> + <see cref="NumberOfIdEntries"/> <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/> structures.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ImageResourceDirectory : IValue, IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal string DebuggerDisplay
        {
            get
            {
                var builder = new StringBuilder();

                builder.Append("[Directory] ");

                builder.Append("[");

                for (var i = 0; i < Entries.Length; i++)
                {
                    var entry = Entries[i];

                    builder.Append(entry.NameOrId);

                    if (i < Entries.Length - 1)
                        builder.Append(", ");
                }

                builder.Append("]");

                return builder.ToString();
            }
        }

        /// <summary>
        /// Resource flags. This field is reserved for future use. It is currently set to zero.
        /// </summary>
        public uint Characteristics { get; init; }

        /// <summary>
        /// The time that the resource data was created by the resource compiler.
        /// </summary>
        public uint TimeDateStamp { get; init; }

        /// <summary>
        /// The major version number, set by the user.
        /// </summary>
        public ushort MajorVersion { get; init; }

        /// <summary>
        /// The minor version number, set by the user.
        /// </summary>
        public ushort MinorVersion { get; init; }

        /// <summary>
        /// The number of directory entries immediately following the table that use strings to identify Type, Name, or Language entries (depending on the level of the table).
        /// </summary>
        public ushort NumberOfNamedEntries { get; init; }

        /// <summary>
        /// The number of directory entries immediately following the Name entries that use numeric IDs for Type, Name, or Language entries.
        /// </summary>
        public ushort NumberOfIdEntries { get; init; }

        public ImageResourceDirectoryEntry[] Entries { get; }

        public RawOffset Offset { get; }

        internal const int StructSize =
            sizeof(uint) + //Characteristics
            sizeof(uint) + //TimeDateStamp
            sizeof(ushort) + //MajorVersion
            sizeof(ushort) + //MinorVersion
            sizeof(ushort) + //NumberOfNamedEntries
            sizeof(ushort);  //NumberOfIdEntries

        internal ImageResourceDirectory(ref FileReader reader, PEFile peFile, ImageResourceDirectoryEntry? parent, RawOffset rootOffset)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Characteristics = reader.ReadUInt32();
            TimeDateStamp = reader.ReadUInt32();
            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();
            NumberOfNamedEntries = reader.ReadUInt16();
            NumberOfIdEntries = reader.ReadUInt16();

            var totalEntries = NumberOfNamedEntries + NumberOfIdEntries;

            if (totalEntries > 0)
            {
                var entries = new ImageResourceDirectoryEntry[totalEntries];

                for (var i = 0; i < totalEntries; i++)
                    entries[i] = new ImageResourceDirectoryEntry(ref reader, peFile, parent, rootOffset);

                Entries = entries;
            }
            else
                Entries = Array.Empty<ImageResourceDirectoryEntry>();
        }

        public IEnumerable<IValue> EnumerateResources() => EnumerateResources<IValue>();

        /// <summary>
        /// Enumerates all resource values pointed to by <see cref="ImageResourceDataEntry"/> entries that are descended from this directory.
        /// </summary>
        /// <returns>An enumeration of resource values.</returns>
        public IEnumerable<T> EnumerateResources<T>() where T : IValue
        {
            foreach (var directoryEntry in Entries)
            {
                if (directoryEntry.DataIsDirectory)
                {
                    foreach (var childEntry in directoryEntry.OffsetToDirectory.Value.EnumerateResources<T>())
                        yield return childEntry;
                }
                else
                {
                    var value = directoryEntry.OffsetToData.Value.OffsetToData;

                    if (value.IsValid && value.Value is T t)
                        yield return t;
                }
            }
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_RESOURCE_DIRECTORY), this, ViewKind.ImageResourceDirectory);

            s.WriteField(nameof(Characteristics), Characteristics);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(MajorVersion), MajorVersion);
            s.WriteField(nameof(MinorVersion), MinorVersion);
            s.WriteField(nameof(NumberOfNamedEntries), NumberOfNamedEntries);
            s.WriteField(nameof(NumberOfIdEntries), NumberOfIdEntries);

            writer.WriteGlobal(Entries);
        }
    }
}
