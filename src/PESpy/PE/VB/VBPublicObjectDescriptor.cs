using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.VB
{
    public struct VBPublicObjectDescriptor : IViewableValue
    {
        private const int lpObjectInfoOffset = 0;
        private const int dwReservedOffset = 4;
        private const int lpPublicBytesOffset = 8;
        private const int lpStaticBytesOffset = 12;
        private const int lpModulePublicOffset = 16;
        private const int lpModuleStaticOffset = 20;
        private const int lpszObjectNameOffset = 24;
        private const int dwMethodCountOffset = 28;
        private const int lpMethodNamesOffset = 32;
        private const int bStaticVarsOffset = 36;
        private const int fObjectTypeOffset = 40;
        private const int dwNullOffset = 44;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<VBObjectInfo> objectInfo;

        /// <summary>
        /// Pointer to the Object Info for this Object.
        /// </summary>
        public VA<VBObjectInfo> lpObjectInfo
        {
            get
            {
                if (objectInfo.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpObjectInfoOffset);

                    var peFile = chunk.PEFile();

                    var rva = (int) (va - peFile.OptionalHeader.ImageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        objectInfo = new VA<VBObjectInfo>(va, valueChunk.AbsoluteOffset, new VBObjectInfo(valueChunk));
                    }
                    else
                        objectInfo = new VA<VBObjectInfo>(va);
                }

                return objectInfo;
            }
        }

        //Hanging off the end of the object info may be an optional object info. It's not really "part" of the object info,
        //and since it relies on information contained in the public object descriptor, I've decided to store it on the public
        //object descriptor instead

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VBOptionalObjectInfo? optionalObjectInfo;

        public VBOptionalObjectInfo? OptionalObjectInfo
        {
            get
            {
                if (optionalObjectInfo == null && (fObjectType & 2) != 0)
                {
                    var va = chunk.PeekInt32(lpObjectInfoOffset);

                    var peFile = chunk.PEFile();

                    var rva = (int) (va - peFile.OptionalHeader.ImageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        optionalObjectInfo = new VBOptionalObjectInfo(valueChunk.Slice(VBObjectInfo.StructSize));
                }

                return optionalObjectInfo;
            }
        }

        /// <summary>
        /// Always set to -1 after compiling.
        /// </summary>
        public int dwReserved => chunk.PeekInt32(dwReservedOffset);

        /// <summary>
        /// Pointer to Public Variable Size integers.
        /// </summary>
        public int lpPublicBytes => chunk.PeekInt32(lpPublicBytesOffset);

        /// <summary>
        /// Pointer to Static Variable Size integers.
        /// </summary>
        public int lpStaticBytes => chunk.PeekInt32(lpStaticBytesOffset);

        /// <summary>
        /// Pointer to Public Variables in DATA section
        /// </summary>
        public int lpModulePublic => chunk.PeekInt32(lpModulePublicOffset);

        /// <summary>
        /// Pointer to Static Variables in DATA section
        /// </summary>
        public int lpModuleStatic => chunk.PeekInt32(lpModuleStaticOffset);

        private VA<AnsiString> objectName;

        /// <summary>
        /// Name of the Object.
        /// </summary>
        public VA<AnsiString> lpszObjectName => ExeProjectInfo.ReadVAAnsiString(ref objectName, chunk, lpszObjectNameOffset);

        /// <summary>
        /// Number of Methods in Object.
        /// </summary>
        public int dwMethodCount => chunk.PeekInt32(dwMethodCountOffset);

        private VA<VA<AnsiString>[]> methodNames;

        /// <summary>
        /// If present, pointer to Method names array.
        /// </summary>
        public VA<VA<AnsiString>[]> lpMethodNames
        {
            get
            {
                if (methodNames.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpMethodNamesOffset);

                    var peFile = chunk.PEFile();

                    var imageBase = peFile.OptionalHeader.ImageBase;

                    var rva = (int) (va - imageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var addrs = valueChunk.PeekNativeSpan<int>(0, dwMethodCount);

                        var results = new VA<AnsiString>[dwMethodCount];

                        for (var i = 0; i < results.Length; i++)
                        {
                            var addr = addrs[i];

                            //So many of these seem to be 0, but looking at what we have in IDA,
                            //it does seem to be correct
                            if (addr == 0)
                            {
                                results[i] = new VA<AnsiString>(addr);
                                continue;
                            }

                            rva = (int) (addr - imageBase);

                            if (peFile.TryGetValueChunkFromSection(rva, out var chunk))
                                results[i] = new VA<AnsiString>(addr, chunk.AbsoluteOffset, chunk.PeekAnsiNullTerminatedString(0));
                            else
                                results[i] = new VA<AnsiString>(addr);
                        }

                        methodNames = new VA<VA<AnsiString>[]>(va, valueChunk.AbsoluteOffset, results);
                    }
                    else
                        methodNames = new VA<VA<AnsiString>[]>(va);
                }

                return methodNames;
            }
        }

        /// <summary>
        /// Offset to where to copy Static Variables.
        /// </summary>
        public int bStaticVars => chunk.PeekInt32(bStaticVarsOffset);

        /// <summary>
        /// Flags defining the Object Type.
        /// </summary>
        public int fObjectType => chunk.PeekInt32(fObjectTypeOffset); //The bits for this are defined in https://sandsprite.com/vb-reversing/VBParser/?utm_source=chatgpt.com in figure 15 but it's incomprehensible to me

        /// <summary>
        /// Not valid after compilation.
        /// </summary>
        public int dwNull => chunk.PeekInt32(dwNullOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //lpObjectInfo
            sizeof(int) + //dwReserved
            sizeof(int) + //lpPublicBytes
            sizeof(int) + //lpStaticBytes
            sizeof(int) + //lpModulePublic
            sizeof(int) + //lpModuleStatic
            sizeof(int) + //lpszObjectName
            sizeof(int) + //dwMethodCount
            sizeof(int) + //lpMethodNames
            sizeof(int) + //bStaticVars
            sizeof(int) + //fObjectType
            sizeof(int); //dwNull

        private readonly MemoryChunk chunk;

        internal VBPublicObjectDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteVAPointerField(lpObjectInfo, offset, lpObjectInfoOffset);
            writer.WriteVAAnsiNullTerminatedField(lpszObjectName, ViewKind.AnsiString, offset, lpszObjectNameOffset);
            writer.WriteGlobal(OptionalObjectInfo);

            var names = lpMethodNames;

            if (names.IsValid)
            {
                var off = names.ActualOffset;

                for (var i = 0; i < names.Value.Length; i++)
                {
                    writer.WriteVAAnsiNullTerminatedField(names.Value[i], ViewKind.AnsiString, off, i * sizeof(int));
                }
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBPublicObjectDescriptor, StructSize);

        int IViewable.NumChildren() => 12;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteVAPointerField(nameof(lpObjectInfo), lpObjectInfoOffset, lpObjectInfo);
                    break;

                case 1:
                    structWriter.WriteField(nameof(dwReserved), dwReservedOffset, dwReserved);
                    break;

                case 2:
                    structWriter.WriteField(nameof(lpPublicBytes), lpPublicBytesOffset, lpPublicBytes);
                    break;

                case 3:
                    structWriter.WriteField(nameof(lpStaticBytes), lpStaticBytesOffset, lpStaticBytes);
                    break;

                case 4:
                    structWriter.WriteField(nameof(lpModulePublic), lpModulePublicOffset, lpModulePublic);
                    break;

                case 5:
                    structWriter.WriteField(nameof(lpModuleStatic), lpModuleStaticOffset, lpModuleStatic);
                    break;

                case 6:
                    structWriter.WriteVAAnsiNullTerminatedField(nameof(lpszObjectName), lpszObjectNameOffset, lpszObjectName);
                    break;

                case 7:
                    structWriter.WriteField(nameof(dwMethodCount), dwMethodCountOffset, dwMethodCount);
                    break;

                case 8:
                    structWriter.WritePointerField(nameof(lpMethodNames), lpMethodNamesOffset, lpMethodNames.ListedAddress, FieldViewFlags.Address);
                    break;

                case 9:
                    structWriter.WriteField(nameof(bStaticVars), bStaticVarsOffset, bStaticVars);
                    break;

                case 10:
                    structWriter.WriteField(nameof(fObjectType), fObjectTypeOffset, fObjectType);
                    break;

                case 11:
                    structWriter.WriteField(nameof(dwNull), dwNullOffset, dwNull);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return lpszObjectName.ToString();
        }
    }
}
