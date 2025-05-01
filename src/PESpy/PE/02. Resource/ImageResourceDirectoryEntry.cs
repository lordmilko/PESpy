using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/> structure that describes an entry contained within an <see cref="IMAGE_RESOURCE_DIRECTORY"/>
    /// that either points to an <see cref="IMAGE_RESOURCE_DATA_ENTRY"/>, or yet another <see cref="IMAGE_RESOURCE_DIRECTORY"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ImageResourceDirectoryEntry : IValue, IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                var ancestors = new List<ImageResourceDirectoryEntry>();

                var current = this;

                while (current != null)
                {
                    ancestors.Add(current);

                    current = current.Parent;
                }

                var builder = new StringBuilder();

                builder.Append("/");

                for (var i = ancestors.Count - 1; i >= 0; i--)
                {
                    var ancestor = ancestors[i];

                    if (i == ancestors.Count - 1)
                    {
                        builder.Append(ancestor.Type?.ToString() ?? ancestor.NameOrId.ToString());
                    }
                    else
                        builder.Append(ancestors[i].NameOrId);

                    if (i >= 1)
                        builder.Append("/");
                }

                builder.Append("/");

                return builder.ToString();
            }
        }

        /// <summary>
        /// Gets the parent directory entry of this entry, or <see langword="null"/> if this is the top level entry.<para/>
        /// This member is not part of the native struct definition.
        /// </summary>
        public ImageResourceDirectoryEntry? Parent { get; }

        /// <summary>
        /// Provides access to the string or numeric identifier of this directory entry.
        /// </summary>
        public UnionNameOrId NameOrId { get; }

        /// <summary>
        /// Gets the offset to the <see cref="IMAGE_RESOURCE_DATA_ENTRY"/> this entry points to. Only applies when <see cref="DataIsDirectory"/> is false.
        /// </summary>
        public RVA<ImageResourceDataEntry> OffsetToData => dataAndDirectoryUnion.OffsetToData;

        /// <summary>
        /// Gets the offset to the <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/> this entry points to. Only applies when <see cref="DataIsDirectory"/> is true.
        /// </summary>
        public RVA<ImageResourceDirectory> OffsetToDirectory => dataAndDirectoryUnion.OffsetToDirectory;

        /// <summary>
        /// Gets whether this entry points to another <see cref="IMAGE_RESOURCE_DIRECTORY"/>. If false, it points to an <see cref="IMAGE_RESOURCE_DATA_ENTRY"/>.
        /// </summary>
        public bool DataIsDirectory => dataAndDirectoryUnion.DataIsDirectory;

        //It's way too confusing having to go through OffsetToData to get to the union...and then go through OffsetToData again!
        private readonly UnionOffsetToData dataAndDirectoryUnion;

        /// <summary>
        /// Gets the type of resource contained in this directory entry.<para/>
        /// If this directory entry is not the top level directory entry, or has a string identifier, this member returns <see langword="null"/>.
        /// </summary>
        public ResourceType? Type
        {
            get
            {
                if (Parent != null || NameOrId.NameIsString)
                    return null;

                return (ResourceType) NameOrId.Id;
            }
        }

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //NameOrId
            sizeof(int);  //Offset

#if PEFAST
        private readonly MemoryChunk chunk;
        private readonly int rootRVA;

        internal ImageResourceDirectoryEntry(in MemoryChunk chunk, int rootRVA, ImageResourceDirectoryEntry? parent)
        {
            this.chunk = chunk;
            this.rootRVA = rootRVA;
            Parent = parent;

            NameOrId = new UnionNameOrId(chunk, rootRVA);
            dataAndDirectoryUnion = new UnionOffsetToData(chunk.Slice(4), rootRVA, this);
        }
#else
        internal ImageResourceDirectoryEntry(IFileReader reader, PEFile peFile, ImageResourceDirectoryEntry? parent, RawOffset rootOffset)
        {
            Offset = (RawOffset) reader.Position;

            Parent = parent;

            NameOrId = new UnionNameOrId(reader, rootOffset);
            dataAndDirectoryUnion = new UnionOffsetToData(reader, peFile, this, rootOffset);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_RESOURCE_DIRECTORY_ENTRY), this, ViewKind.ImageResourceDirectoryEntry);

            if (NameOrId.NameIsString)
                s.WriteRVAField("Name", NameOrId.NameOffset);
            else
                s.WriteField("Id", (int) NameOrId.Id);

            if (dataAndDirectoryUnion.DataIsDirectory)
            {
                //We need to write the original value, where the high bit is set. The high bit will have been cleared
                //in OffsetToDirectory, but is still present in OffsetToData (where we stored it for posterity)
                s.WriteField("OffsetToData", (int) dataAndDirectoryUnion.OffsetToData.ListedOffset);

                if (dataAndDirectoryUnion.OffsetToDirectory.IsValid)
                    writer.WriteGlobal(dataAndDirectoryUnion.OffsetToDirectory.Value);
            }
            else
            {
                //No high bit to set, so we can just write OffsetToData as is
                s.WriteRVAField("OffsetToData", dataAndDirectoryUnion.OffsetToData);
            }
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public struct UnionNameOrId
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    if (NameIsString)
                        return "[Name] " + NameOffset;

                    return "[Id] " + Id;
                }
            }

            //These three sets are unioned together

            #region Bitfield

            public RVA<ImageResourceDirStringU> NameOffset { get; }

            public bool NameIsString => ((Name >> 31) & 1) == 1;

            #endregion

            public int Name { get; } //Stores the raw data

            public ushort Id => (ushort) Name; //Must be ushort, you can have big values

#if PEFAST
            internal UnionNameOrId(in MemoryChunk chunk, RawOffset rootRVA)
            {
                var value = chunk.PeekInt32(0);

                var nameIsString = ((value >> 31) & 1) == 1;
                var nameOffset = (RVA) (value & 0x7FFFFFFF); //Remove the top bit

                Name = value;

                if (nameIsString)
                {
                    var offset = rootRVA + (int) nameOffset;

                    //Tested and confirmed this works for both loaded and unloaded modules
                    if (chunk.PEFile().TryGetValueChunkFromSection(offset, out var valueChunk))
                    {
                        var name = new ImageResourceDirStringU(valueChunk);
                        NameOffset = new RVA<ImageResourceDirStringU>(nameOffset, valueChunk.AbsoluteOffset, name);
                    }
                    else
                    {
                        //It's bad
                        NameOffset = new RVA<ImageResourceDirStringU>(nameOffset);
                    }
                }
                else
                {
                    //It's bad; just list what the bottom 31 bits were
                    NameOffset = new RVA<ImageResourceDirStringU>(nameOffset);
                }
            }
#else
            internal UnionNameOrId(IFileReader reader, RawOffset rootOffset)
            {
                var value = reader.ReadInt32();

                var nameIsString = ((value >> 31) & 1) == 1;
                var nameOffset = (RVA) (value & 0x7FFFFFFF); //Remove the top bit

                Name = value;

                if (nameIsString)
                {
                    var oldPosition = reader.Position;

                    var offset = rootOffset + (int) nameOffset;
                    reader.Seek(offset);

                    var name = new ImageResourceDirStringU(reader);

                    NameOffset = new RVA<ImageResourceDirStringU>(nameOffset, offset, name);

                    reader.Seek(oldPosition);
                }
                else
                {
                    //It's bad; just list what the bottom 31 bits were
                    NameOffset = new RVA<ImageResourceDirStringU>(nameOffset);
                }
            }
#endif

            public override string ToString()
            {
                if (NameIsString)
                    return NameOffset.ToString();

                return Id.ToString();
            }
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        private readonly struct UnionOffsetToData
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    if (DataIsDirectory)
                        return OffsetToDirectory.ToString();

                    return "[Data] " + OffsetToData;
                }
            }

            //These two sets are unioned together
            public RVA<ImageResourceDataEntry> OffsetToData { get; } //Stores the raw data

            #region Bitfield

            public RVA<ImageResourceDirectory> OffsetToDirectory { get; }
            public bool DataIsDirectory => (((int) OffsetToData.ListedOffset >> 31) & 1) == 1;

            #endregion

#if PEFAST
            internal UnionOffsetToData(in MemoryChunk chunk, int rootRVA, ImageResourceDirectoryEntry parent)
            {
                var value = chunk.PeekInt32(0);

                var dataIsDirectory = ((value >> 31) & 1) == 1;
                var offsetToDirectory = (RVA) (value & 0x7FFFFFFF); //Remove the top bit

                if (dataIsDirectory)
                {
                    var offset = rootRVA + (int) offsetToDirectory;

                    if (chunk.PEFile().TryGetValueChunkFromSection(offset, out var valueChunk))
                    {
                        var directory = new ImageResourceDirectory(valueChunk, rootRVA, parent);

                        OffsetToData = new RVA<ImageResourceDataEntry>((RVA) value); //Just store the raw data
                        OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory, offset, directory);
                    }
                    else
                    {
                        OffsetToData = new RVA<ImageResourceDataEntry>((RVA) value); //Just store the raw data
                        OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory);
                    }
                }
                else
                {
                    var offset = rootRVA + value; //The high bit isn't set, so the value is OffsetToData

                    if (chunk.PEFile().TryGetValueChunkFromSection(offset, out var valueChunk))
                    {
                        var data = new ImageResourceDataEntry(valueChunk, parent);

                        OffsetToData = new RVA<ImageResourceDataEntry>((RVA) value, offset, data);

                        //Just list what the bottom 31 bits were
                        OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory);
                    }
                    else
                    {
                        OffsetToData = new RVA<ImageResourceDataEntry>(value);
                        OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory);
                    }
                }
            }
#else
            public UnionOffsetToData(IFileReader reader, PEFile peFile, ImageResourceDirectoryEntry parent, RawOffset rootOffset)
            {
                var value = reader.ReadInt32();

                var dataIsDirectory = ((value >> 31) & 1) == 1;
                var offsetToDirectory = (RVA) (value & 0x7FFFFFFF); //Remove the top bit

                var oldPosition = reader.Position;

                if (dataIsDirectory)
                {
                    var offset = rootOffset + (int) offsetToDirectory;
                    reader.Seek(offset);

                    var directory = new ImageResourceDirectory(reader, peFile, parent, rootOffset);

                    OffsetToData = new RVA<ImageResourceDataEntry>((RVA) value); //Just store the raw data
                    OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory, offset, directory);
                }
                else
                {
                    var offset = rootOffset + value; //The high bit isn't set, so the value is OffsetToData
                    reader.Seek(offset);

                    var data = new ImageResourceDataEntry(reader, peFile, parent);

                    OffsetToData = new RVA<ImageResourceDataEntry>((RVA) value, offset, data);

                    //Just list what the bottom 31 bits were
                    OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory);
                }

                reader.Seek(oldPosition);
            }
#endif
        }
    }
}
