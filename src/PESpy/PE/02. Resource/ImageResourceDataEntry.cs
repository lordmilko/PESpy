using System;
using System.Diagnostics;
using System.Text;
using PESpy.Native;
using PESpy.View;
using static PESpy.ClrDebugResource;
using static PESpy.RT;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RESOURCE_DATA_ENTRY"/> structure that describes a data entry pointed to by an <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/> contained in an <see cref="IMAGE_RESOURCE_DIRECTORY"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ImageResourceDataEntry : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        private const int OffsetToDataOffset = 0;
        private const int SizeOffset = 4;
        private const int CodePageOffset = 8;
        private const int ReservedOffset = 12;

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

        private RVA<object> offsetToData;

        /// <summary>
        /// The address of a unit of resource data in the Resource Data area.
        /// </summary>
        public unsafe RVA<object> OffsetToData
        {
            get
            {
                if (offsetToData.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(OffsetToDataOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var type = Type;
                        
                        //While we would like to say that we encapsulate an IValue, in the case of a FixedUtf8String
                        //that would result in an RVA<RawValue<FixedUtf8String>> which is no good
                        object? value;

                        if (type is RT t)
                        {
                            switch (t)
                            {
                                case RT_CURSOR:
                                case RT_BITMAP:
                                case RT_ICON:
                                case RT_MENU:
                                case RT_DIALOG:
                                case RT_STRING: //https://devblogs.microsoft.com/oldnewthing/20040130-00/?p=40813
                                case RT_FONTDIR:
                                case RT_FONT:
                                case RT_ACCELERATOR:
                                    goto default;

                                case RT_RCDATA:
                                    if (!TryParseRCData(valueChunk, out value))
                                        goto default;

                                    break;

                                case RT_MESSAGETABLE:
                                    value = new MessageResourceData(valueChunk);
                                    break;

                                case RT_GROUP_CURSOR:
                                case RT_GROUP_ICON:
                                    goto default;

                                case RT_VERSION:
                                    value = new VsVersionInfo(valueChunk);
                                    break;

                                case RT_DLGINCLUDE:
                                case RT_PLUGPLAY:
                                case RT_VXD:
                                case RT_ANICURSOR:
                                case RT_ANIICON:
                                case RT_HTML:
                                    goto default;

                                case RT_MANIFEST:
                                    //Note that the manifest may start with a UTF-8 BOM
                                    value = new FixedUtf8String(valueChunk.Pointer, Size);
                                    break;

                                default:
                                    value = new ByteBlob(valueChunk, Size, ViewKind.UnknownResource);
                                    break;
                            }
                        }
                        else
                        {
                            /* - MUI
                             *
                             *   You might be inclined to think that MUI is FILEMUIINFO, but this is wrong! FILEMUIINFO is what is returned by GetFileMUIInfo,
                             *   but this is not the physical representation. The physical data starts with CD FE CD FE. The physical representation is converted to FILEMUIINFO
                             *   by kernelbase!GetFileMUIInfo. There's a bit of complexity to it. ReactOS has their interpretation of the physical data structure
                             *
                             * - IMAGE
                             * - WEVT_TEMPLATE (https://github.com/libyal/libfwevt/blob/main/documentation/Windows%20Event%20manifest%20binary%20format.asciidoc). need to include this reference permanently
                             */

                            //If it's not a well known type, just parse as a byte blob

                            //IMAGE
                            //MUI
                            //WEVT_TEMPLATE (https://github.com/libyal/libfwevt/blob/main/documentation/Windows%20Event%20manifest%20binary%20format.asciidoc). need to include this reference permanently
                            value = new ByteBlob(valueChunk, Size, ViewKind.UnknownResource);
                        }

                        offsetToData = new RVA<object>(rva, valueChunk.AbsoluteOffset, value!);
                    }
                    else
                        offsetToData = new RVA<object>(rva);
                }

                return offsetToData;
            }
        }

        /// <summary>
        /// The size, in bytes, of the resource data that is pointed to by the Data RVA field.
        /// </summary>
        public int Size => chunk.PeekInt32(SizeOffset);

        /// <summary>
        /// The code page that is used to decode code point values within the resource data. Typically, the code page would be the Unicode code page.
        /// </summary>
        public int CodePage => chunk.PeekInt32(CodePageOffset);

        public int Reserved => chunk.PeekInt32(ReservedOffset);

        /// <summary>
        /// Gets the <see cref="RT"/> or <see cref="string"/> that describes the type of data contained in this entry.
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

                return (RT) parent.NameOrId.Id;
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

        private bool TryParseRCData(in MemoryChunk valueChunk, out object? value)
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

                if (signature == CLR_ID_V4_DESKTOP ||
                    signature == CLR_ID_CORECLR ||
                    signature == CLR_ID_PHONE_CLR ||
                    signature == CLR_ID_ONECORE_CLR)
                {
                    value = new ClrDebugResource(valueChunk);
                    return true;
                }
                else
                {
                    Debug.Assert(false, $"Encountered an unknown GUID '{signature}'");
                }
            }

            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offsetToData = OffsetToData;

            if (offsetToData.IsValid)
            {
                if (Type is RT rt)
                {
                    switch (rt)
                    {
                        case RT_CURSOR:
                        case RT_BITMAP:
                        case RT_ICON:
                        case RT_MENU:
                        case RT_DIALOG:
                        case RT_STRING:
                        case RT_FONTDIR:
                        case RT_FONT:
                        case RT_ACCELERATOR:
                            goto default;

                        case RT_RCDATA:
                            if (offsetToData.Value is ClrDebugResource)
                                WriteOffsetToData<ClrDebugResource>(writer, offsetToData);
                            else
                                goto default;
                            break;

                        case RT_MESSAGETABLE:
                            WriteOffsetToData<MessageResourceData>(writer, offsetToData);
                            break;

                        case RT_GROUP_CURSOR:
                        case RT_GROUP_ICON:
                            goto default;

                        case RT_VERSION:
                            WriteOffsetToData<VsVersionInfo>(writer, offsetToData);
                            break;

                        case RT_DLGINCLUDE:
                        case RT_PLUGPLAY:
                        case RT_VXD:
                        case RT_ANICURSOR:
                        case RT_ANIICON:
                        case RT_HTML:
                            goto default;

                        case RT_MANIFEST:
                            writer.WriteRVAUtf8FixedLengthField(
                                new RVA<FixedUtf8String>(
                                    offsetToData.ListedOffset,
                                    offsetToData.ActualOffset,
                                    (FixedUtf8String) offsetToData.Value
                                ),
                                ViewKind.Manifest,
                                Offset,
                                OffsetToDataOffset
                            );
                            break;

                        default:
                            //We don't support these yet and they should implicitly be a ByteBlob
                            WriteOffsetToData<ByteBlob>(writer, offsetToData);
                            break;
                    }
                }
                else
                {
                    //We don't support any string types yet
                    WriteOffsetToData<ByteBlob>(writer, offsetToData);
                }
            }
        }

        private void WriteOffsetToData<T>(ViewWriter writer, RVA<object> offsetToData) where T : IViewable, IValue
        {
            writer.WriteRVAField(new RVA<T>(offsetToData.ListedOffset, offsetToData.ActualOffset, (T) offsetToData.Value), Offset, OffsetToDataOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageResourceDataEntry, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(OffsetToData), OffsetToDataOffset, (int) OffsetToData.ListedOffset);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Size), SizeOffset, Size);
                    break;

                case 2:
                    structWriter.WriteField(nameof(CodePage), CodePageOffset, CodePage);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Reserved), ReservedOffset, Reserved);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
