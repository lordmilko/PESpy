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
    /// Represents the <see cref="IMAGE_RESOURCE_DATA_ENTRY"/> structure that describes a data entry pointed to by an <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/> contained in an <see cref="IMAGE_RESOURCE_DIRECTORY"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ImageResourceDataEntry : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                var ancestors = new List<ImageResourceDirectoryEntry>();

                var current = Parent;

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

                    if (i == ancestors.Count - 1 && ancestors.Count == 3)
                    {
                        builder.Append(ancestor.Type?.ToString() ?? ancestor.NameOrId.ToString());
                    }
                    else
                        builder.Append(ancestors[i].NameOrId);

                    if (i >= 1)
                        builder.Append("/");
                }

                builder.Append("/<data>");

                return builder.ToString();
            }
        }

        public ImageResourceDirectoryEntry Parent { get; }

        /// <summary>
        /// The address of a unit of resource data in the Resource Data area.
        /// </summary>
        public RVA<IValue> OffsetToData { get; init; }

        /// <summary>
        /// The size, in bytes, of the resource data that is pointed to by the Data RVA field.
        /// </summary>
        public int Size { get; init; }

        /// <summary>
        /// The code page that is used to decode code point values within the resource data. Typically, the code page would be the Unicode code page.
        /// </summary>
        public int CodePage { get; init; }

        public int Reserved { get; init; }

        /// <summary>
        /// Gets the <see cref="ResourceType"/> or <see cref="string"/> that describes the type of data contained in this entry.
        /// </summary>
        public object? Type
        {
            get
            {
                var parent = Parent;

                while (true)
                {
                    if (parent.Parent == null)
                        break;

                    parent = parent.Parent;
                }

                if (parent == null)
                    return null;

                if (parent.NameOrId.NameIsString)
                    return parent.NameOrId.NameOffset.ToString();

                return ((ResourceType) parent.NameOrId.Id);
            }
        }

        public RawOffset Offset { get; }

        internal const int StructSize =
            sizeof(int) + //OffsetToData
            sizeof(int) + //Size
            sizeof(int) + //CodePage
            sizeof(int);  //Reserved

        internal ImageResourceDataEntry(IFileReader reader, PEFile peFile, ImageResourceDirectoryEntry parent)
        {
            Offset = (RawOffset) reader.Position;

            Parent = parent;

            reader.FillBuffer(StructSize);

            var offsetToData = (RVA) reader.ReadInt32();
            Size = reader.ReadInt32();
            CodePage = reader.ReadInt32();
            Reserved = reader.ReadInt32();

            if (!peFile.TryGetOffset(offsetToData, out var offset))
                OffsetToData = new RVA<IValue>(offsetToData);
            else
            {
                reader.Seek(offset);

                var type = Type;
                IValue value;

                if (type is ResourceType t)
                {
                    switch (t)
                    {
                        case ResourceType.Cursor:
                        case ResourceType.Bitmap:
                        case ResourceType.Icon:
                        case ResourceType.Menu:
                        case ResourceType.Dialog:
                        case ResourceType.String:
                        case ResourceType.FontDir:
                        case ResourceType.Font:
                        case ResourceType.Accelerator:
                        case ResourceType.RCData:
                        case ResourceType.MessageTable:
                        case ResourceType.GroupCursor:
                        case ResourceType.GroupIcon:
                            goto default;

                        case ResourceType.Version:
                            value = new VsVersionInfo(reader);
                            break;

                        case ResourceType.DlgInclude:
                        case ResourceType.PlugPlay:
                        case ResourceType.Vxd:
                        case ResourceType.AniCursor:
                        case ResourceType.AniIcon:
                        case ResourceType.Html:
                        case ResourceType.Manifest:
                        default:
                            value = new ByteBlob(reader, Size);
                            break;
                    }
                }
                else
                {
                    //If it's not a well known type, just parse as a byte blob

                    //IMAGE
                    //MUI
                    //WEVT_TEMPLATE (https://github.com/libyal/libfwevt/blob/main/documentation/Windows%20Event%20manifest%20binary%20format.asciidoc). need to include this reference permanently
                    value = new ByteBlob(reader, Size);
                }

                OffsetToData = new RVA<IValue>(offsetToData, offset, value);
            }
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_RESOURCE_DATA_ENTRY), this, ViewKind.ImageResourceDataEntry);

            s.WriteField(nameof(OffsetToData), (int) OffsetToData.ListedOffset);
            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(CodePage), CodePage);
            s.WriteField(nameof(Reserved), Reserved);

            if (OffsetToData.IsValid)
            {
                if (OffsetToData.Value is VsVersionInfo v)
                    writer.WriteGlobal(v);
                else if (OffsetToData.Value is ByteBlob b)
                    writer.WriteGlobal(b);
                else
                    throw new NotImplementedException($"Don't know how to write a resource of type {OffsetToData.Value.GetType().Name}");
            }
        }
    }
}
