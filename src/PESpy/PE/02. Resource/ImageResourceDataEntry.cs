using System;
using System.Diagnostics;
using System.Text;
using PESpy.Native;
using PESpy.View;

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
                using var ancestors = new PooledList<ImageResourceDirectoryEntry>();

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

        private RVA<IValue> offsetToData;

        /// <summary>
        /// The address of a unit of resource data in the Resource Data area.
        /// </summary>
        public unsafe RVA<IValue> OffsetToData
        {
            get
            {
                if (offsetToData.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(0);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var type = Type;
                        IValue? value;

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
                                    goto default;

                                case ResourceType.RCData:
                                    if (!TryParseRCData(valueChunk, out value))
                                        goto default;

                                    break;

                                case ResourceType.MessageTable:
                                    value = new MessageResourceData(valueChunk);
                                    break;

                                case ResourceType.GroupCursor:
                                case ResourceType.GroupIcon:
                                    goto default;

                                case ResourceType.Version:
                                    value = new VsVersionInfo(valueChunk);
                                    break;

                                case ResourceType.DlgInclude:
                                case ResourceType.PlugPlay:
                                case ResourceType.Vxd:
                                case ResourceType.AniCursor:
                                case ResourceType.AniIcon:
                                case ResourceType.Html:
                                case ResourceType.Manifest:
                                    //Note that the manifest may start with a UTF-8 BOM
                                    value = new RawValue<FixedUtf8String>(valueChunk.AbsoluteOffset, new FixedUtf8String(valueChunk.Pointer, valueChunk.Remaining));
                                    break;

                                default:
                                    value = new ByteBlob(valueChunk, Size);
                                    break;
                            }
                        }
                        else
                        {
                            //If it's not a well known type, just parse as a byte blob

                            //IMAGE
                            //MUI
                            //WEVT_TEMPLATE (https://github.com/libyal/libfwevt/blob/main/documentation/Windows%20Event%20manifest%20binary%20format.asciidoc). need to include this reference permanently
                            value = new ByteBlob(valueChunk, Size);
                        }

                        offsetToData = new RVA<IValue>(rva, valueChunk.AbsoluteOffset, value!);
                    }
                    else
                        offsetToData = new RVA<IValue>(rva);
                }

                return offsetToData;
            }
        }

        /// <summary>
        /// The size, in bytes, of the resource data that is pointed to by the Data RVA field.
        /// </summary>
        public int Size => chunk.PeekInt32(4);

        /// <summary>
        /// The code page that is used to decode code point values within the resource data. Typically, the code page would be the Unicode code page.
        /// </summary>
        public int CodePage => chunk.PeekInt32(8);

        public int Reserved => chunk.PeekInt32(12);

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

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //OffsetToData
            sizeof(int) + //Size
            sizeof(int) + //CodePage
            sizeof(int);  //Reserved

        private readonly MemoryChunk chunk;

        internal ImageResourceDataEntry(in MemoryChunk chunk, ImageResourceDirectoryEntry parent)
        {
            this.chunk = chunk;
            Parent = parent;
        }

        private bool TryParseRCData(in MemoryChunk valueChunk, out IValue? value)
        {
            value = null;

            if (Parent?.Parent?.NameOrId.ToString().StartsWith("CLRDEBUGINFO") == true)
            {
                if (Size != ClrDebugResource.StructSize)
                    return false;

                //https://github.com/dotnet/runtime/blob/511d26611c051c56e546404ea616c220cc78817c/src/coreclr/dlls/mscoree/coreclr/GenClrDebugResource.ps1#L4
                var version = valueChunk.PeekInt32(0);

                if (version != 0)
                    throw new NotImplementedException("Don't know how to handle version being 0. Rewind our IFileReader?");

                var signature = valueChunk.PeekGuid(4);

                if (signature != ClrDebugResource.CLR_ID_ONECORE_CLR)
                    throw new NotImplementedException("Don't know how to handle Guid not being CLR_ID_ONECORE_CLR. Rewind our IFileReader?");

                value = new ClrDebugResource(valueChunk);
                return true;
            }

            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            if (OffsetToData.IsValid)
            {
                if (OffsetToData.Value is IViewable v)
                    writer.WriteGlobal(v);
                else if (OffsetToData.Value is RawValue<FixedUtf8String> r)
                    writer.WriteGlobal(r.Offset, r.Value, r.Value.Length, ViewKind.Manifest);
                else
                    throw new NotImplementedException($"Don't know how to write a resource of type {OffsetToData.Value.GetType().Name}");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_RESOURCE_DATA_ENTRY, this, ViewKind.ImageResourceDataEntry, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(OffsetToData), (int) OffsetToData.ListedOffset);
            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(CodePage), CodePage);
            s.WriteField(nameof(Reserved), Reserved);

            return s.ToArray();
        }
    }
}
