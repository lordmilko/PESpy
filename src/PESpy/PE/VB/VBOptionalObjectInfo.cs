using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.VB
{
    public class VBOptionalObjectInfo : IViewableValue //May not be present
    {
        private const int dwObjectGuidsOffset = 0;
        private const int lpObjectGuidOffset = 4;
        private const int dwNullOffset = 8;
        private const int lpuuidObjectTypesOffset = 12;
        private const int dwObjectTypeGuidsOffset = 16;
        private const int lpControls2Offset = 20;
        private const int dwNull2Offset = 24;
        private const int lpObjectGuid2Offset = 28;
        private const int dwControlCountOffset = 32;
        private const int lpControlsOffset = 36;
        private const int wEventCountOffset = 40;
        private const int wPCodeCountOffset = 42;
        private const int bWInitializeEventOffset = 44;
        private const int bWTerminateEventOffset = 46;
        private const int lpEventsOffset = 48;
        private const int lpBasicClassObjectOffset = 52;
        private const int dwNull3Offset = 56;
        private const int lpIdeDataOffset = 60;

        /// <summary>
        /// How many GUIDs to Register. 2 = Designer
        /// </summary>
        public int dwObjectGuids => chunk.PeekInt32(dwObjectGuidsOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<Guid> objectGuid;

        /// <summary>
        /// Unique GUID of the Object
        /// </summary>
        public VA<Guid> lpObjectGuid => ExeProjectInfo.ReadGuid(ref objectGuid, chunk, lpObjectGuidOffset);

        /// <summary>
        /// Unused.
        /// </summary>
        public int dwNull => chunk.PeekInt32(dwNullOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<VA<Guid>[]> uuidObjectTypes;

        /// <summary>
        /// Pointer to Array of Object Interface GUIDs
        /// </summary>
        public VA<VA<Guid>[]> lpuuidObjectTypes
        {
            get
            {
                if (uuidObjectTypes.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpuuidObjectTypesOffset);

                    var peFile = chunk.PEFile();

                    var imageBase = peFile.OptionalHeader.ImageBase;

                    var rva = (int) (va - imageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        //I don't think that this points to an array of GUIDs; I think it points to an array of pointers
                        //to GUIDs
                        var addresses = valueChunk.PeekNativeSpan<int>(0, dwObjectTypeGuids);

                        var results = new VA<Guid>[dwObjectTypeGuids];

                        for (var i = 0; i < results.Length; i++)
                        {
                            var addr = addresses[i];

                            var guidRVA = (int) (addr - imageBase);

                            if (peFile.TryGetValueChunkFromSection(guidRVA, out var guidChunk))
                                results[i] = new VA<Guid>(addr, guidChunk.AbsoluteOffset, guidChunk.PeekGuid(0));
                            else
                                results[i] = new VA<Guid>(addr);
                        }

                        uuidObjectTypes = new VA<VA<Guid>[]>(va, valueChunk.AbsoluteOffset, results);
                    }
                    else
                        uuidObjectTypes = new VA<VA<Guid>[]>(va);
                }

                return uuidObjectTypes;
            }
        }

        /// <summary>
        /// How many GUIDs in the Array above.
        /// </summary>
        public int dwObjectTypeGuids => chunk.PeekInt32(dwObjectTypeGuidsOffset);

        /// <summary>
        /// Usually the same as lpControls.
        /// </summary>
        public int lpControls2 => chunk.PeekInt32(lpControls2Offset);

        /// <summary>
        /// Unused.
        /// </summary>
        public int dwNull2 => chunk.PeekInt32(dwNull2Offset);

        /// <summary>
        /// Pointer to Array of Object GUIDs.
        /// </summary>
        public int lpObjectGuid2 => chunk.PeekInt32(lpObjectGuid2Offset);

        /// <summary>
        /// Number of Controls in array below.
        /// </summary>
        public int dwControlCount => chunk.PeekInt32(dwControlCountOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<VBControlInfo[]> controls;

        /// <summary>
        /// Pointer to Controls Array.
        /// </summary>
        public VA<VBControlInfo[]> lpControls
        {
            get
            {
                if (controls.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpControlsOffset);

                    var peFile = chunk.PEFile();

                    var rva = (int) (va - peFile.OptionalHeader.ImageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var results = new VBControlInfo[dwControlCount];

                        for (var i = 0; i < results.Length; i++)
                            results[i] = new VBControlInfo(valueChunk.Slice(i * VBControlInfo.StructSize));

                        controls = new VA<VBControlInfo[]>(va, valueChunk.AbsoluteOffset, results);
                    }
                    else
                        controls = new VA<VBControlInfo[]>(va);
                }

                return controls;
            }
        }

        /// <summary>
        /// Number of Events in Event Array.
        /// </summary>
        public short wEventCount => chunk.PeekInt16(wEventCountOffset);

        /// <summary>
        /// Number of P-Codes used by this Object.
        /// </summary>
        public short wPCodeCount => chunk.PeekInt16(wPCodeCountOffset);

        /// <summary>
        /// Offset to Initialize Event from Event Table.
        /// </summary>
        public short bWInitializeEvent => chunk.PeekInt16(bWInitializeEventOffset);

        /// <summary>
        /// Offset to Terminate Event in Event Table.
        /// </summary>
        public short bWTerminateEvent => chunk.PeekInt16(bWTerminateEventOffset);

        /// <summary>
        /// Pointer to Events Array.
        /// </summary>
        public int lpEvents => chunk.PeekInt32(lpEventsOffset);

        /// <summary>
        /// Pointer to in-memory Class Objects.
        /// </summary>
        public int lpBasicClassObject => chunk.PeekInt32(lpBasicClassObjectOffset);

        /// <summary>
        /// Unused.
        /// </summary>
        public int dwNull3 => chunk.PeekInt32(dwNull3Offset);

        /// <summary>
        /// Only valid in IDE.
        /// </summary>
        public int lpIdeData => chunk.PeekInt32(lpIdeDataOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //dwObjectGuids
            sizeof(int) + //lpObjectGuid
            sizeof(int) + //dwNull
            sizeof(int) + //lpuuidObjectTypes
            sizeof(int) + //dwObjectTypeGuids
            sizeof(int) + //lpControls2
            sizeof(int) + //dwNull2
            sizeof(int) + //lpObjectGuid2
            sizeof(int) + //dwControlCount
            sizeof(int) + //lpControls
            sizeof(short) + //wEventCount
            sizeof(short) + //wPCodeCount
            sizeof(short) + //bWInitializeEvent
            sizeof(short) + //bWTerminateEvent
            sizeof(int) + //lpEvents
            sizeof(int) + //lpBasicClassObject
            sizeof(int) + //dwNull3
            sizeof(int); //lpIdeData

        private readonly MemoryChunk chunk;

        internal VBOptionalObjectInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteVAPointerField(lpControls, offset, lpControlsOffset);
            writer.WriteVAPointerField(lpObjectGuid, ViewKind.Guid, offset, lpObjectGuidOffset);

            var guids = lpuuidObjectTypes;

            writer.WriteVAXRef(offset, lpuuidObjectTypesOffset, guids.ListedAddress);

            if (guids.IsValid)
            {
                var off = guids.ActualOffset;

                for (var i = 0; i < guids.Value.Length; i++)
                {
                    writer.WriteVAPointerField(guids.Value[i], ViewKind.Guid, off, i * sizeof(int));
                }
            }

            //There's an xref from the field to the list of VAs, and then even more xrefs from each VA to the GUID that it points to
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBOptionalObjectInfo, StructSize);

        int IViewable.NumChildren() => 18;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(dwObjectGuids), dwObjectGuidsOffset, dwObjectGuids);
                    break;

                case 1:
                    structWriter.WriteVAPointerField(nameof(lpObjectGuid), lpObjectGuidOffset, lpObjectGuid);
                    break;

                case 2:
                    structWriter.WriteField(nameof(dwNull), dwNullOffset, dwNull);
                    break;

                case 3:
                    structWriter.WritePointerField(nameof(lpuuidObjectTypes), lpuuidObjectTypesOffset, lpuuidObjectTypes.ListedAddress, FieldViewFlags.Address);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dwObjectTypeGuids), dwObjectTypeGuidsOffset, dwObjectTypeGuids);
                    break;

                case 5:
                    structWriter.WriteField(nameof(lpControls2), lpControls2Offset, lpControls2);
                    break;

                case 6:
                    structWriter.WriteField(nameof(dwNull2), dwNull2Offset, dwNull2);
                    break;

                case 7:
                    structWriter.WriteField(nameof(lpObjectGuid2), lpObjectGuid2Offset, lpObjectGuid2);
                    break;

                case 8:
                    structWriter.WriteField(nameof(dwControlCount), dwControlCountOffset, dwControlCount);
                    break;

                case 9:
                    structWriter.WriteVAPointerField(nameof(lpControls), lpControlsOffset, lpControls);
                    break;

                case 10:
                    structWriter.WriteField(nameof(wEventCount), wEventCountOffset, wEventCount);
                    break;

                case 11:
                    structWriter.WriteField(nameof(wPCodeCount), wPCodeCountOffset, wPCodeCount);
                    break;

                case 12:
                    structWriter.WriteField(nameof(bWInitializeEvent), bWInitializeEventOffset, bWInitializeEvent);
                    break;

                case 13:
                    structWriter.WriteField(nameof(bWTerminateEvent), bWTerminateEventOffset, bWTerminateEvent);
                    break;

                case 14:
                    structWriter.WriteField(nameof(lpEvents), lpEventsOffset, lpEvents);
                    break;

                case 15:
                    structWriter.WriteField(nameof(lpBasicClassObject), lpBasicClassObjectOffset, lpBasicClassObject);
                    break;

                case 16:
                    structWriter.WriteField(nameof(dwNull3), dwNull3Offset, dwNull3);
                    break;

                case 17:
                    structWriter.WriteField(nameof(lpIdeData), lpIdeDataOffset, lpIdeData);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
