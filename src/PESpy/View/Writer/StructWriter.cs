using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.Ecma335;
using PESpy.PDB;
using Enum = System.Enum;
using SN = PESpy.PDB.SN;

namespace PESpy.View
{
    public ref struct StructWriter
    {
        private readonly ViewWriter _viewWriter;
        private readonly int _parentOffset;

        internal IView? Field;
        internal IView[]? EagerFields;

        internal ViewWriter ViewWriter => _viewWriter;
        internal int ParentOffset => _parentOffset;

        internal StructWriter(ViewWriter viewWriter, int parentOffset)
        {
            _viewWriter = viewWriter;
            _parentOffset = parentOffset;
            Field = default;
            EagerFields = default;
        }

        internal static Exception GetEagerLoadOnlyException() =>
            new NotSupportedException($"This type is too complex to support direct indexing. Use {nameof(ViewChildList)} instead.");

        internal EagerStructWriter CreateEagerWriter() => new EagerStructWriter(this);

        internal ISymbolAccessor GetSymbolAccessor() => _viewWriter.GetSymbolAccessor();

        //Write a ByteBlob to ensure the specified alignment of the contents of the struct, or throw if we're already aligned, in which case
        //the caller shouldn't be asking us to align again
        internal IMAGE_FILE_MACHINE GetMachine(in MemoryChunk chunk) => throw new NotImplementedException();

        internal static bool NeedsEagerChildren(ViewKind kind)
        {
            //Certain types involve very complex packing and aligning logic; so much so that a lot of extra work would be involved
            //if we were to try and recompute where we're up to with each successive child index that we try to access. As such, for
            //these troublesome types, we special case these and provide a mechanism to get all of their children in one go

            switch (kind)
            {
                case ViewKind.CvDebugSSubsectionHeader:
                case ViewKind.StreamTable:
                case ViewKind.PogoData:

                case ViewKind.LfFieldList:
                case ViewKind.LfFieldList16t:
                case ViewKind.StringFileInfo:

                case ViewKind.VsVersionInfo:
                case ViewKind.StringTable:
                case ViewKind.StringTable_String:
                case ViewKind.VarFileInfo:
                case ViewKind.VarFileInfo_Var:

                case ViewKind.ImageCorILMethodTiny:
                case ViewKind.ImageCorILMethodFat:
                case ViewKind.ImageLoadConfigDirectory:
                case ViewKind.UnwindInfo:
                    return true;

                default:
                    return false;
            }
        }

        internal void AlignOrThrow(int bytesUsed)
        {
            var paddingSize = ((bytesUsed + 3) & ~3) - bytesUsed;

            if (paddingSize > 0)
                WriteByteBlob(bytesUsed, paddingSize);
            else
                throw new IndexOutOfRangeException();
        }

        internal static int GetNumChildrenAlign4(int numChildren, int bytesUsed)
        {
            if (bytesUsed != ((bytesUsed + 3) & ~3))
                return numChildren + 1; //Used bytes are not 32-bit aligned; we'll need to add padding

            return numChildren;
        }

        #region Byte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, byte value) =>
            RelayField(name, relativeOffset, value, sizeof(byte));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, sbyte value) =>
            RelayField(name, relativeOffset, value, sizeof(sbyte));

        #endregion
        #region Int16

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, short value) =>
            RelayField(name, relativeOffset, value, sizeof(short));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, ushort value) =>
            RelayField(name, relativeOffset, value, sizeof(ushort));

        #endregion
        #region Int32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, int value) =>
            RelayField(name, relativeOffset, value, sizeof(int));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, uint value) =>
            RelayField(name, relativeOffset, value, sizeof(uint));

        #endregion
        #region Int64

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, long value) =>
            RelayField(name, relativeOffset, value, sizeof(long));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, ulong value) =>
            RelayField(name, relativeOffset, value, sizeof(ulong));

        #endregion
        #region Float / Double

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, float value) =>
            RelayField(name, relativeOffset, value, sizeof(float));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, double value) =>
            RelayField(name, relativeOffset, value, sizeof(double));

        #endregion
        #region Strings
        #region Ansi

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAnsiNullTerminatedField(string name, int relativeOffset, string value) =>
            RelayField(name, relativeOffset, value, value.Length + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAnsiNullTerminatedField(string name, int relativeOffset, AnsiString value) =>
            RelayField(name, relativeOffset, value, value.Length + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAnsiFixedLengthField(string name, int relativeOffset, FixedAnsiString value) =>
            RelayField(name, relativeOffset, value, value.Length);
        #region Utf8

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf8NullTerminatedField(string name, int relativeOffset, string value) =>
            RelayField(name, relativeOffset, value, value.Length + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf8NullTerminatedField(string name, int relativeOffset, Utf8String value) =>
            RelayField(name, relativeOffset, value, value.Length + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf8FixedLengthField(string name, int relativeOffset, FixedUtf8String value) =>
            RelayField(name, relativeOffset, value, value.Length);

        #endregion
        #region Utf16

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf16NullTerminatedField(string name, int relativeOffset, string value) =>
            RelayField(name, relativeOffset, value, (value.Length + 1) * 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf16FixedLengthField(string name, int relativeOffset, FixedUtf16String value) =>
            RelayField(name, relativeOffset, value, value.Length * 2);

        //Sometimes you can have a fixed length field that ends in a null terminator; in that case, we need to report the true length
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf16FixedLengthField(string name, int relativeOffset, FixedUtf16String value, int length) =>
            RelayField(name, relativeOffset, value, length * 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf16NullTerminatedField(string name, int relativeOffset, Utf16String value) =>
            RelayField(name, relativeOffset, value, (value.Length + 1) * 2);

        #endregion
        #region Null Padded

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNullPaddedAnsiField(string name, int relativeOffset, FixedAnsiString value, int length) => RelayField(name, relativeOffset, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNullPaddedUtf8Field(string name, int relativeOffset, FixedUtf8String value, int length) => RelayField(name, relativeOffset, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNullPaddedUtf8Field(string name, int relativeOffset, string value, int length) => RelayField(name, relativeOffset, value, length);

        #endregion
        #region Other

        public void WriteNullTerminatedField(string name, int relativeOffset, NullTerminatedString value)
        {
            switch (value.Kind)
            {
                case StringKind.ANSI:
                case StringKind.UTF8:
                    RelayField(name, relativeOffset, value, value.Length + 1);
                    break;

                default:
                    Debug.Assert(value.Kind == StringKind.UTF16);
                    RelayField(name, relativeOffset, value, (value.Length + 1) * 2);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSymStringField(string name, int relativeOffset, SymString value) => RelayField(name, relativeOffset, value, value.Length + 1);

        #endregion
        #endregion
        #region Pointer

        public void WritePointerField(string name, int relativeOffset, long value)
        {
            if (((PEViewWriter) _viewWriter).Is32Bit)
                RelayField(name, relativeOffset, (int) value, sizeof(int));
            else
                RelayField(name, relativeOffset, value, sizeof(long));
        }

        public void WritePointerField(string name, int relativeOffset, ulong value)
        {
            if (((PEViewWriter) _viewWriter).Is32Bit)
                RelayField(name, relativeOffset, (uint) value, sizeof(int));
            else
                RelayField(name, relativeOffset, value, sizeof(long));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField<T>(string name, int relativeOffset, VA<T> value) where T : IViewable, IValue
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField<T>(string name, int relativeOffset, VA<T[]> value) where T : IViewable, IValue
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSmallVAPointerField<T>(string name, int relativeOffset, VA<T> value) where T : IViewable, IValue
        {
            WriteField(name, relativeOffset, (int) value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLargeVAPointerField<T>(string name, int relativeOffset, VA<T> value) where T : IViewable, IValue
        {
            WriteField(name, relativeOffset, (long) value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSmallVAPointerField<T>(string name, int relativeOffset, VA<T[]> value) where T : IViewable, IValue
        {
            WriteField(name, relativeOffset, (int) value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, int relativeOffset, VA<long> value, ViewKind valueKind)
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, int relativeOffset, VA<ulong> value, ViewKind valueKind)
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, int relativeOffset, VA<ulong[]> value, ViewKind valueKind)
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, int relativeOffset, VA<int[]> value, ViewKind valueKind)
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAAnsiNullTerminatedField(string name, int relativeOffset, VA<string> value)
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAAnsiNullTerminatedField(string name, int relativeOffset, VA<AnsiString> value)
        {
            WritePointerField(name, relativeOffset, value.ListedAddress);

            _viewWriter.VerifyXRef(value);
        }

        #endregion
        #region RVA

        //The fact that the target is a pointer is irrelevant; it's still just an RVA
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAPointerField(string name, int relativeOffset, RVA<long> value)
        {
            WriteField(name, relativeOffset, value.ListedOffset);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAField(string name, int relativeOffset, RVA<ulong[]> value)
        {
            WriteField(name, relativeOffset, value.ListedOffset);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAAnsiNullTerminatedField(string name, int relativeOffset, RVA<string> value)
        {
            WriteField(name, relativeOffset, value.ListedOffset);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAAnsiNullTerminatedField(string name, int relativeOffset, RVA<AnsiString> value)
        {
            WriteField(name, relativeOffset, value.ListedOffset);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAField<T>(string name, int relativeOffset, RVA<T> value) where T : IViewable, IValue
        {
            WriteField(name, relativeOffset, value.ListedOffset);

            _viewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAField<T>(string name, int relativeOffset, RVA<T[]> value) where T : IViewable, IValue
        {
            WriteField(name, relativeOffset, value.ListedOffset);

            _viewWriter.VerifyXRef(value);
        }

        #endregion
        #region Typedefs

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, CV_typ_t value) =>
            RelayField(name, relativeOffset, value, sizeof(int));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, CV_typ16_t value) =>
            RelayField(name, relativeOffset, value, sizeof(short));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, PN value) =>
            RelayField(name, relativeOffset, value, sizeof(int));

        //Sometimes this is 32-bit; it's up to the caller to say what they want
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, SN value) =>
            RelayField(name, relativeOffset, value, sizeof(short));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, SN value, int size) =>
            RelayField(name, relativeOffset, value, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, IMOD value) =>
            RelayField(name, relativeOffset, value, sizeof(short));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, ISECT value) =>
            RelayField(name, relativeOffset, value, sizeof(short));

        #endregion
        #region Enums

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField<T>(string name, int relativeOffset, T value, int size) where T : Enum =>
            RelayField(name, relativeOffset, value, size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        #endregion
        #region Structs

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, Guid guid) =>
            RelayField(name, relativeOffset, guid, 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, Timestamp value) =>
            RelayField(name, relativeOffset, value, sizeof(int));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, CV_lvar_attr value) =>
            RelayField(name, relativeOffset, value, sizeof(int) + sizeof(short) + sizeof(short));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, CV_RANGEATTR value) =>
            RelayField(name, relativeOffset, value, sizeof(short));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, CV_GENERIC_FLAG value) =>
            RelayField(name, relativeOffset, value, sizeof(short));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, CV_SEPCODEFLAGS value) =>
            RelayField(name, relativeOffset, value, sizeof(int));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, int relativeOffset, CV_LVAR_ADDR_RANGE value) =>
            RelayField(name, relativeOffset, value, sizeof(int) + sizeof(short) + sizeof(short));
        #region Arrays
        #region Byte[]

        public void WriteField(string name, int relativeOffset, NativeSpan<byte> value)
        {
            //A field with a length of 0 will calculate itself as having a negative size (since if it starts at 0 and is 2 large it ends at 1)
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length);
        }

        public void WriteField(string name, int relativeOffset, byte[] value)
        {
            Debug.Assert(value.Length > 0);
            RelayField(name, relativeOffset, value, value.Length);
        }

        #endregion
        #region Int16[]

        public void WriteField(string name, int relativeOffset, NativeSpan<short> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 2);
        }

        public void WriteField(string name, int relativeOffset, NativeSpan<ushort> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 2);
        }

        public void WriteField(string name, int relativeOffset, ushort[] value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 2);
        }

        #endregion
        #region Int32[]

        public void WriteField(string name, int relativeOffset, NativeSpan<int> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 4);
        }

        public void WriteField(string name, int relativeOffset, int[] value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 4);
        }

        #endregion
        #region String[]

        public void WriteUtf8NullTerminatedField(string name, int relativeOffse, string[] value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            var size = 0;

            foreach (var item in value)
                size += item.Length + 1;

            RelayField(name, relativeOffse, value, size);
        }

        #endregion
        #region Typedefs[]

        public void WriteField(string name, int relativeOffset, NativeSpan<PN> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 4);
        }

        public void WriteField(string name, int relativeOffset, PN[] value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 4);
        }

        public void WriteField(string name, int relativeOffset, NativeSpan<CV_typ_t> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * sizeof(int));
        }

        public void WriteField(string name, int relativeOffset, CV_typ_t[] value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 4);
        }

        public void WriteField(string name, int relativeOffset, NativeSpan<CV_typ16_t> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * sizeof(short));
        }

        public void WriteField(string name, int relativeOffset, NativeSpan<CV_ItemId> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * sizeof(int));
        }

        public void WriteField(string name, int relativeOffset, CV_ItemId[] value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, value.Length * 4);
        }

        #endregion
        #region Structs

        public void WriteField(string name, int relativeOffset, NativeSpan<CV_LVAR_ADDR_GAP> value)
        {
            if (value.Length == 0)
            {
                //The caller should not be asking us to write this if it's empty, because this will mess up their child count
                throw new IndexOutOfRangeException();
            }

            RelayField(name, relativeOffset, value, (sizeof(short) + sizeof(short)) * value.Length);
        }

        #endregion
        #endregion
        #region Ecma
        #region Heap

        internal void WriteStringHeapIndex(string name, int relativeOffset, StringIndex index)
        {
            if (((PEViewWriter) _viewWriter).MetadataReader.StringIndexSize == 4)
                WriteField(name, relativeOffset, (int) index);
            else
                WriteField(name, relativeOffset, (ushort) index);
        }

        internal void WriteBlobHeapIndex(string name, int relativeOffset, BlobIndex index)
        {
            if (((PEViewWriter) _viewWriter).MetadataReader.BlobIndexSize == 4)
                WriteField(name, relativeOffset, (int) index);
            else
                WriteField(name, relativeOffset, (ushort) index);
        }

        internal void WriteBlobHeapIndex(string name, int relativeOffset, DocumentNameBlobIndex index)
        {
            if (((PEViewWriter) _viewWriter).MetadataReader.BlobIndexSize == 4)
                WriteField(name, relativeOffset, (int) index);
            else
                WriteField(name, relativeOffset, (ushort) index);
        }

        internal void WriteGuidHeapIndex(string name, int relativeOffset, GuidIndex index)
        {
            if (((PEViewWriter) _viewWriter).MetadataReader.GuidIndexSize == 4)
                WriteField(name, relativeOffset, (int) index);
            else
                WriteField(name, relativeOffset, (ushort) index);
        }

        #endregion
        #region Coded

        internal void WriteTypeDefOrRefIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.TypeDefOrRefSize);

        internal void WriteHasConstantIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.HasConstantSize);

        internal void WriteHasCustomAttributeIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.HasCustomAttributeSize);

        internal void WriteHasFieldMarshalIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.HasFieldMarshalSize);

        internal void WriteHasDeclSecurityIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.HasDeclSecuritySize);

        internal void WriteMemberRefParentIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.MemberRefParentSize);

        internal void WriteHasSemanticsIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.HasSemanticsSize);

        internal void WriteMethodDefOrRefIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.MethodDefOrRefSize);

        internal void WriteMemberForwardedIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.MemberForwardedSize);

        internal void WriteImplementationIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.ImplementationSize);

        internal void WriteCustomAttributeTypeIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.CustomAttributeTypeSize);

        internal void WriteResolutionScopeIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.ResolutionScopeSize);

        internal void WriteTypeOrMethodDefIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.TypeOrMethodDefSize);

        //Portable PDB

        internal void WriteHasCustomDebugInformationIndex(string name, int relativeOffset, int value) =>
            WriteIndex(name, relativeOffset, value, ((PEViewWriter) _viewWriter).MetadataReader.HasCustomDebugInformationSize);

        private void WriteIndex(string name, int relativeOffset, int index, int indexSize)
        {
            if (indexSize == 4)
                WriteField(name, relativeOffset, index);
            else
                WriteField(name, relativeOffset, (ushort) index);
        }

        internal void WriteSimpleIndex(string name, int relativeOffset, int value, TableKind kind)
        {
            var size = ((PEViewWriter) _viewWriter).MetadataReader.GetSimpleIndexSize(kind);

            WriteIndex(name, relativeOffset, value, size);
        }

        #endregion
        #endregion
        #region String (RawValue)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInlineAnsiNullTerminated(RawValue<AnsiString> value) =>
            RelayInline(value.Offset, value.Value, value.Value.Length + 1, ViewKind.String);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInlineSymString(RawValue<SymString> value) =>
            RelayInline(value.Offset, value.Value, value.Value.Length + 1, ViewKind.String);

        #endregion
        #endregion
        #region ByteBlob

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByteBlob(int relativeOffset, int size) =>
            _viewWriter.WriteByteBlob(_parentOffset, relativeOffset, size, ref this);

        #endregion
        #region StructField

        internal void WriteStructField<T>(string name, int relativeOffset, T value) where T : unmanaged, IViewable =>
            _viewWriter.WriteStructField(name, _parentOffset, relativeOffset, value, ref this);
        internal void WriteStructField<T>(string name, T value) where T : IViewableValue =>
            _viewWriter.WriteStructField(name, value, ref this);

        internal void WriteStructField<T>(string name, T[] value) where T : IViewableValue =>
            _viewWriter.WriteStructField(name, value, ref this);

        #endregion
        #region BitField

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitField(string name, int relativeOffset, byte value, int size, int bits) =>
            RelayBitField(name, relativeOffset, value, size, bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitField(string name, int relativeOffset, short value, int size, int bits) =>
            RelayBitField(name, relativeOffset, value, size, bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitField(string name, int relativeOffset, ushort value, int size, int bits) =>
            RelayBitField(name, relativeOffset, value, size, bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitField(string name, int relativeOffset, int value, int size, int bits) =>
            RelayBitField(name, relativeOffset, value, size, bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitField(string name, int relativeOffset, uint value, int size, int bits) =>
            RelayBitField(name, relativeOffset, value, size, bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitField(string name, int relativeOffset, bool value, int size, int bits)
        {
            //Don't know if it could be possible to have a bool that occupies 2 bytes, so make the caller think about what the size of the field is instead of just assuming all bools are 1 byte
            Debug.Assert(bits == 1, $"Writing a bool that is supposed to occupy {bits} bits is not implemented");

            RelayBitField(name, relativeOffset, (byte) (value ? 1 : 0), size, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitField<T>(string name, int relativeOffset, T value, int size, int bits) =>
            RelayBitField(name, relativeOffset, value, size, bits);

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RelayField<T>(string name, int relativeOffset, T value, int size) =>
            _viewWriter.WriteField(name, _parentOffset, relativeOffset, value, size, ref this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RelayBitField<T>(string name, int relativeOffset, T value, int size, int bits) =>
            _viewWriter.WriteBitField(name, _parentOffset, relativeOffset, value, size, bits, ref this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RelayInline<T>(int valueOffset, T value, int size, ViewKind kind) =>
            _viewWriter.WriteValue(_parentOffset, valueOffset - _parentOffset, value, size, kind, ref this);
    }
}
