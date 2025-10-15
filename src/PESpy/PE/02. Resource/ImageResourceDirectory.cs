using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RESOURCE_DIRECTORY"/> structure. Immediately following this structure
    /// are <see cref="NumberOfNamedEntries"/> + <see cref="NumberOfIdEntries"/> <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/> structures.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ImageResourceDirectory : IValue, IViewable
    {
        private const int CharacteristicsOffset = 0;
        private const int TimeDateStampOffset = 4;
        private const int MajorVersionOffset = 8;
        private const int MinorVersionOffset = 10;
        private const int NumberOfNamedEntriesOffset = 12;
        private const int NumberOfIdEntriesOffset = 14;

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

                    builder.Append(entry.NameOrId.ToString());

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
        public uint Characteristics => chunk.PeekUInt32(CharacteristicsOffset);

        /// <summary>
        /// The time that the resource data was created by the resource compiler.
        /// </summary>
        public Timestamp TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        /// <summary>
        /// The major version number, set by the user.
        /// </summary>
        public ushort MajorVersion => chunk.PeekUInt16(MajorVersionOffset);

        /// <summary>
        /// The minor version number, set by the user.
        /// </summary>
        public ushort MinorVersion => chunk.PeekUInt16(MinorVersionOffset);

        /// <summary>
        /// The number of directory entries immediately following the table that use strings to identify Type, Name, or Language entries (depending on the level of the table).
        /// </summary>
        public ushort NumberOfNamedEntries => chunk.PeekUInt16(NumberOfNamedEntriesOffset);

        /// <summary>
        /// The number of directory entries immediately following the Name entries that use numeric IDs for Type, Name, or Language entries.
        /// </summary>
        public ushort NumberOfIdEntries => chunk.PeekUInt16(NumberOfIdEntriesOffset);

        private ImageResourceDirectoryEntry[] entries;

        public ImageResourceDirectoryEntry[] Entries
        {
            get
            {
                if (entries == null)
                {
                    var totalEntries = NumberOfNamedEntries + NumberOfIdEntries;

                    if (totalEntries > 0)
                    {
                        var results = new ImageResourceDirectoryEntry[totalEntries];

                        for (var i = 0; i < totalEntries; i++)
                            results[i] = new ImageResourceDirectoryEntry(chunk.Slice(FixedStructSize + (i * ImageResourceDirectoryEntry.StructSize)), rootRVA, parent);

                        entries = results;
                    }
                    else
                        entries = Array.Empty<ImageResourceDirectoryEntry>();
                }

                return entries;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(uint) + //Characteristics
            sizeof(uint) + //TimeDateStamp
            sizeof(ushort) + //MajorVersion
            sizeof(ushort) + //MinorVersion
            sizeof(ushort) + //NumberOfNamedEntries
            sizeof(ushort);  //NumberOfIdEntries

        private readonly MemoryChunk chunk;
        private readonly int rootRVA;
        private readonly ImageResourceDirectoryEntry? parent;

        internal ImageResourceDirectory(in MemoryChunk chunk, int rootRVA, ImageResourceDirectoryEntry? parent)
        {
            this.chunk = chunk;
            this.rootRVA = rootRVA;
            this.parent = parent;

            entries = null!;

#if STRESS_TEST
            _ = Entries;
#endif
        }

        public IValue[] Resources => EnumerateResources().ToArray();

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Entries);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_RESOURCE_DIRECTORY, this, ViewKind.ImageResourceDirectory, FixedStructSize); //The entries are written as global, so the FixedStructSize is all we care about

        int IViewable.NumChildren => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Characteristics), CharacteristicsOffset, Characteristics);
                    break;

                case 1:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MajorVersion), MajorVersionOffset, MajorVersion);
                    break;

                case 3:
                    structWriter.WriteField(nameof(MinorVersion), MinorVersionOffset, MinorVersion);
                    break;

                case 4:
                    structWriter.WriteField(nameof(NumberOfNamedEntries), NumberOfNamedEntriesOffset, NumberOfNamedEntries);
                    break;

                case 5:
                    structWriter.WriteField(nameof(NumberOfIdEntries), NumberOfIdEntriesOffset, NumberOfIdEntries);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
