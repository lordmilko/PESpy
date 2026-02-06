using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PESpy.Native;
using PESpy.View;

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
                using var ancestors = new PooledList<ImageResourceDirectoryEntry>();

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

        private const int NameOrIdOffset = 0;
        internal const int DataAndDirectoryOffset = 4;

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
        public RT? Type
        {
            get
            {
                if (Parent != null || NameOrId.NameIsString)
                    return null;

                return (RT) NameOrId.Id;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //NameOrId
            sizeof(int);  //Offset

        private readonly MemoryChunk chunk;
        private readonly int rootRVA;

        internal ImageResourceDirectoryEntry(in MemoryChunk chunk, int rootRVA, ImageResourceDirectoryEntry? parent)
        {
            this.chunk = chunk;
            this.rootRVA = rootRVA;
            Parent = parent;

            NameOrId = new UnionNameOrId(chunk, rootRVA);
            dataAndDirectoryUnion = new UnionOffsetToData(chunk.Slice(DataAndDirectoryOffset), rootRVA, this);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var structOffset = Offset;

            if (NameOrId.NameIsString)
                writer.WriteRVAField(NameOrId.NameOffset, structOffset, fieldOffset: NameOrIdOffset);

            if (dataAndDirectoryUnion.DataIsDirectory)
                writer.WriteRVAField(dataAndDirectoryUnion.OffsetToDirectory, structOffset, fieldOffset: DataAndDirectoryOffset);
            else
                writer.WriteRVAField(dataAndDirectoryUnion.OffsetToData, structOffset, fieldOffset: DataAndDirectoryOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_RESOURCE_DIRECTORY_ENTRY, this, ViewKind.ImageResourceDirectoryEntry, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    if (NameOrId.NameIsString)
                        structWriter.WriteRVAField("Name", NameOrIdOffset, NameOrId.NameOffset);
                    else
                        structWriter.WriteField("Id", NameOrIdOffset, (int) NameOrId.Id);

                    break;

                case 1:
                    if (dataAndDirectoryUnion.DataIsDirectory)
                    {
                        //We need to write the original value, where the high bit is set. The high bit will have been cleared
                        //in OffsetToDirectory, but is still present in OffsetToData (where we stored it for posterity)
                        structWriter.WriteField("OffsetToData", DataAndDirectoryOffset, (int) dataAndDirectoryUnion.OffsetToData.ListedOffset);
                    }
                    else
                    {
                        //No high bit to set, so we can just write OffsetToData as is
                        structWriter.WriteRVAField("OffsetToData", DataAndDirectoryOffset, dataAndDirectoryUnion.OffsetToData);
                    }

                    break;

                default:
                    throw new IndexOutOfRangeException();
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

            internal UnionNameOrId(in MemoryChunk chunk, int rootRVA)
            {
                var value = chunk.PeekInt32(0);

                var nameIsString = ((value >> 31) & 1) == 1;
                var nameOffset = value & 0x7FFFFFFF; //Remove the top bit

                Name = value;

                if (nameIsString)
                {
                    //rootRVA is the ResourceTableDirectory.VirtualAddress
                    if (chunk.PEFile().TryGetRVARelativeValueChunk(rootRVA, nameOffset, out var valueChunk))
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

            internal UnionOffsetToData(in MemoryChunk chunk, int rootRVA, ImageResourceDirectoryEntry parent)
            {
                var value = chunk.PeekInt32(0);

                var dataIsDirectory = ((value >> 31) & 1) == 1;
                var offsetToDirectory = value & 0x7FFFFFFF; //Remove the top bit

                if (dataIsDirectory)
                {
                    var offset = rootRVA + (int) offsetToDirectory;

                    if (chunk.PEFile().TryGetValueChunkFromSection(offset, out var valueChunk))
                    {
                        var directory = new ImageResourceDirectory(valueChunk, rootRVA, parent);

                        OffsetToData = new RVA<ImageResourceDataEntry>(value); //Just store the raw data
                        OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory, valueChunk.AbsoluteOffset, directory);
                    }
                    else
                    {
                        OffsetToData = new RVA<ImageResourceDataEntry>(value); //Just store the raw data
                        OffsetToDirectory = new RVA<ImageResourceDirectory>(offsetToDirectory);
                    }
                }
                else
                {
                    var offset = rootRVA + value; //The high bit isn't set, so the value is OffsetToData

                    if (chunk.PEFile().TryGetValueChunkFromSection(offset, out var valueChunk))
                    {
                        var data = new ImageResourceDataEntry(valueChunk, parent);

                        OffsetToData = new RVA<ImageResourceDataEntry>(value, valueChunk.AbsoluteOffset, data);

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
        }
    }
}
