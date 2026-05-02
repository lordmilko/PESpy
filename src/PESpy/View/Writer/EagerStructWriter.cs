using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.PDB;
using static PESpy.View.ViewWriter;
using Enum = System.Enum;

namespace PESpy.View
{
    internal ref struct EagerStructWriter
    {
        private StructWriter structWriter;
        private int currentFieldOffset;

        public int Size => currentFieldOffset;

        private PooledList<IView> items;

        public bool Is32Bit() => ((PEViewWriter) structWriter.ViewWriter).Is32Bit;

        internal EagerStructWriter(StructWriter structWriter)
        {
            this.structWriter = structWriter;
            currentFieldOffset = default;
            items = new PooledList<IView>();
        }

        #region Byte

        public void WriteField(string name, byte value, FieldViewFlags flags = default)
        {
            structWriter.WriteField(name, currentFieldOffset, value, flags);

            if (structWriter.Field != null)
                items.Add(structWriter.Field); //Either all fields should be written, or none, so we don't clear the field after reading

            currentFieldOffset += sizeof(byte);
        }

        #endregion
        #region Int16

        public void WriteField(string name, short value)
        {
            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field); //Either all fields should be written, or none, so we don't clear the field after reading

            currentFieldOffset += sizeof(short);
        }

        public void WriteField(string name, ushort value)
        {
            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field); //Either all fields should be written, or none, so we don't clear the field after reading

            currentFieldOffset += sizeof(ushort);
        }

        #endregion
        #region Int32

        public void WriteField(string name, int value, FieldViewFlags flags = default)
        {
            structWriter.WriteField(name, currentFieldOffset, value, flags);

            if (structWriter.Field != null)
                items.Add(structWriter.Field); //Either all fields should be written, or none, so we don't clear the field after reading

            currentFieldOffset += sizeof(int);
        }

        #endregion
        #region Int64

        #endregion
        #region Float / Double

        #endregion
        #region Strings
        #region Ansi
        #endregion
        #region Utf8
        #endregion
        #region Utf16

        public void WriteUtf16NullTerminatedField(string name, Utf16String value)
        {
            structWriter.WriteUtf16NullTerminatedField(name, currentFieldOffset, value);

            var result = structWriter.Field;

            if (result != null)
            {
                items.Add(result);

                //Don't calculate the length again
                currentFieldOffset += result.Size;
            }
            else
                currentFieldOffset += (value.Length + 1) * 2;
        }

        public void WriteUtf16FixedLengthField(string name, FixedUtf16String value, int length)
        {
            structWriter.WriteUtf16FixedLengthField(name, currentFieldOffset, value, length);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += length * 2;
        }

        #endregion
        #endregion
        #region Pointer

        public void WritePointerField(string name, long value, FieldViewFlags flags = default)
        {
            if (((PEViewWriter) structWriter.ViewWriter).Is32Bit)
            {
                structWriter.WriteField(name, currentFieldOffset, (int) value, flags);

                currentFieldOffset += sizeof(int);
            }
            else
            {
                structWriter.WriteField(name, currentFieldOffset, value, flags);
                currentFieldOffset += sizeof(long);
            }

            if (structWriter.Field != null)
                items.Add(structWriter.Field);
        }

        public void WritePointerField(string name, ulong value)
        {
            if (((PEViewWriter) structWriter.ViewWriter).Is32Bit)
            {
                structWriter.WriteField(name, currentFieldOffset, (uint) value);
                currentFieldOffset += sizeof(uint);
            }
            else
            {
                structWriter.WriteField(name, currentFieldOffset, value);
                currentFieldOffset += sizeof(ulong);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField<T>(string name, VA<T> value) where T : IViewable, IValue
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField<T>(string name, VA<T[]> value) where T : IViewable, IValue
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSmallVAPointerField<T>(string name, VA<T> value) where T : IViewable, IValue
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLargeVAPointerField<T>(string name, VA<T> value) where T : IViewable, IValue
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSmallVAPointerField<T>(string name, VA<T[]> value) where T : IViewable, IValue
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, VA<long> value, ViewKind valueKind)
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, VA<ulong> value, ViewKind valueKind)
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, VA<ulong[]> value, ViewKind valueKind)
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVAPointerField(string name, VA<NativeSpan<int>> value, ViewKind valueKind)
        {
            WritePointerField(name, value.ListedAddress, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        public void WriteVAAnsiNullTerminatedField(string name, VA<string> value)
        {
            throw new NotImplementedException();
        }

        public void WriteVAAnsiNullTerminatedField(string name, VA<AnsiString> value)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region RVA

        public void WriteRVAPointerField(string name, RVA<long> value)
        {
            throw new NotImplementedException();
        }

        public void WriteRVAField(string name, RVA<ulong[]> value)
        {
            throw new NotImplementedException();
        }

        public void WriteRVAAnsiNullTerminatedField(string name, RVA<string> value)
        {
            throw new NotImplementedException();
        }

        public void WriteRVAAnsiNullTerminatedField(string name, RVA<AnsiString> value)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAField<T>(string name, RVA<T> value) where T : IViewable, IValue
        {
            WriteField(name, value.ListedOffset, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRVAField<T>(string name, RVA<T[]> value) where T : IViewable, IValue
        {
            WriteField(name, value.ListedOffset, FieldViewFlags.Address);

            structWriter.ViewWriter.VerifyXRef(value);
        }

        #endregion
        #region Typedefs

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(string name, CV_ItemId value)
        {
            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += sizeof(int);
        }

        #endregion
        #region Enums

        public void WriteField<T>(string name, T value, int size) //Generic constraint removed because we need to be able to write compressed integers in FuncInfo4
        {
            structWriter.WriteField(name, currentFieldOffset, value, size);

            if (structWriter.Field != null)
                items.Add(structWriter.Field); //Either all fields should be written, or none, so we don't clear the field after reading

            currentFieldOffset += size;
        }

        public void WriteValue(LEAF_ENUM_e value, int size)
        {
            structWriter.WriteValue(currentFieldOffset, value, size);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += size;
        }

        #endregion
        #region Structs

        public void WriteField(string name, Timestamp value)
        {
            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += sizeof(int);
        }

        public void WriteField(string name, BinaryAnnotationList value)
        {
            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += value.RawLength;
        }

        #endregion
        #region Arrays
        #region Byte[]

        public void WriteField(string name, NativeSpan<byte> value)
        {
            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += value.Length;
        }

        #endregion
        #region Int16[]

        public void WriteField(string name, NativeSpan<ushort> value)
        {
            if (value.Length == 0)
                return;

            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += value.Length * sizeof(ushort);
        }

        public void WriteField(string name, ushort[] value)
        {
            if (value.Length == 0)
                return;

            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += value.Length * sizeof(ushort);
        }

        #endregion
        #region Int32[]

        public void WriteField(string name, NativeSpan<int> value)
        {
            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += value.Length * sizeof(int);
        }

        #endregion
        #region String[]
        #endregion
        #region Typedefs[]

        public void WriteField(string name, PN[] value)
        {
            if (value.Length == 0)
                return;

            structWriter.WriteField(name, currentFieldOffset, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += value.Length * sizeof(int);
        }

        #endregion
        #region Structs
        #endregion
        #endregion
        #region Ecma

        #endregion
        #region Special Formats

        #endregion
        #region Inline

        public void WriteInline<T>(T value) where T : IViewableValue
        {
#if DEBUG
            //If we're a PDBViewWriter, a child struct may have computed its own chunk.AbsoluteOffset as being in a different chunk than the previous field
            //in the parent struct. As such, we can't assert that the field is sequential
            if (structWriter.ViewWriter is not PDBViewWriter)
            {
                var expectedOffset = structWriter.ParentOffset + currentFieldOffset;
                structWriter.ViewWriter.TryGetViewOffset(value.Offset, out var offset);
                Debug.Assert(offset == expectedOffset);
            }
#endif

            var viewWriter = structWriter.ViewWriter;

            var result = value.WriteStruct(viewWriter);

            if (result != null)
            {
                items.Add(result);
                currentFieldOffset += result.Size;
            }
        }

        public void WriteUnmanagedInline<T>(T value) where T : unmanaged, IViewable
        {
            var viewWriter = structWriter.ViewWriter;

            var oldOffset = viewWriter.UnmanagedOffset;
            viewWriter.UnmanagedOffset = structWriter.ParentOffset + currentFieldOffset;

            var result = value.WriteStruct(viewWriter);

            if (result != null)
            {
                items.Add(result);
                currentFieldOffset += result.Size;
            }

            viewWriter.UnmanagedOffset = oldOffset;
        }

        public void WriteUnmanagedInline<TLightweightList, TEnumerator, TElement>(TLightweightList value)
            where TLightweightList : ILightweightList<TEnumerator, TElement>
            where TEnumerator : IEnumerator<TElement>
            where TElement : IViewable
        {
            var viewWriter = structWriter.ViewWriter;

            var oldOffset = viewWriter.UnmanagedOffset;
            viewWriter.UnmanagedOffset = structWriter.ParentOffset + currentFieldOffset;

            var enumerator = value.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;

                var result = item.WriteStruct(viewWriter);

                if (result != null)
                {
                    items.Add(result);
                    currentFieldOffset += result.Size;
                    viewWriter.UnmanagedOffset += result.Size;
                }
            }

            viewWriter.UnmanagedOffset = oldOffset;
        }

        public void WriteInline<T>(T[] value) where T : IViewableValue
        {
            var viewWriter = structWriter.ViewWriter;

            for (var i = 0; i < value.Length; i++)
            {
                var result = value[i].WriteStruct(viewWriter);

                if (result != null)
                {
                    items.Add(result);
                    currentFieldOffset += result.Size;
                }
            }
        }

        public void WriteInline<T>(RVA<T> value, ViewKind kind) where T : IViewableValue
        {
            throw new NotImplementedException();
        }

        public unsafe void WriteInline<T>(RawValue<T> value, ViewKind kind) where T : unmanaged
        {
            Debug.Assert(structWriter.ParentOffset + currentFieldOffset == value.Offset);
            items.Add(new ValueView<T>(value.Offset, value.Value, sizeof(T), kind, structWriter.ViewWriter._fileAccessor));
            currentFieldOffset += sizeof(T);
        }

        public void WriteInlineAnsiNullTerminated(RawValue<AnsiString> value)
        {
            Debug.Assert(structWriter.ParentOffset + currentFieldOffset == value.Offset);

            structWriter.WriteInlineAnsiNullTerminated(value);

            if (structWriter.Field != null)
            {
                items.Add(structWriter.Field);
                currentFieldOffset += structWriter.Field.Size;
            }
            else
                currentFieldOffset += value.Value.Length + 1;
        }

        public void WriteInlineAnsiNullTerminated(RawValue<string> value)
        {
            Debug.Assert(structWriter.ParentOffset + currentFieldOffset == value.Offset);

            structWriter.WriteInlineAnsiNullTerminated(value);

            if (structWriter.Field != null)
            {
                items.Add(structWriter.Field);
                currentFieldOffset += structWriter.Field.Size;
            }
            else
                currentFieldOffset += value.Value.Length + 1;
        }

        public void WriteInlineAnsiNullTerminated(AnsiString value) =>
            throw new NotImplementedException();

        public void WriteInlineFixedAnsiString(FixedAnsiString value) =>
            throw new NotImplementedException();

        public void WriteInlineUtf16NullTerminated(FixedUtf16String value, int size)
        {
            structWriter.WriteInlineUtf16NullTerminated(structWriter.ParentOffset + currentFieldOffset, value);

            if (structWriter.Field != null)
            {
                items.Add(structWriter.Field);
                currentFieldOffset += structWriter.Field.Size;
            }
            else
                currentFieldOffset += value.Length;
        }

        public unsafe void WriteInlineLengthPrefixedAnsiString(RawValue<FixedUtf8String> value) =>
            throw new NotImplementedException();

        public void WriteInlineAnsiNullTerminated(RawValue<AnsiString>[] value) =>
            throw new NotImplementedException();

        public void WriteInlineUtf8NullTerminated(RawValue<Utf8String> value)
        {
            structWriter.WriteInlineUtf8NullTerminated(value);

            if (structWriter.Field != null)
            {
                items.Add(structWriter.Field);
                currentFieldOffset += structWriter.Field.Size;
            }
            else
                currentFieldOffset += value.Value.Length + 1;
        }

        public unsafe void WriteInlineUtf8NullTerminated(RawValue<FixedUtf8String> value) =>
            throw new NotImplementedException();

        public void WriteInlineUtf8NullTerminated(RawValue<Utf8String>[] value)
        {
            foreach (var item in value)
                WriteInlineUtf8NullTerminated(item);
        }

        public void WriteInlineSymString(RawValue<SymString> value)
        {
            structWriter.WriteInlineSymString(value);

            if (structWriter.Field != null)
            {
                items.Add(structWriter.Field);
                currentFieldOffset += structWriter.Field.Size;
            }
            else
                currentFieldOffset += value.Value.Length + 1;
        }

        #endregion
        #region ByteBlob

        public void WriteByteBlob(int size)
        {
            structWriter.WriteByteBlob(currentFieldOffset, size);

            if (structWriter.Field != null)
            {
                items.Add(structWriter.Field);
                currentFieldOffset += structWriter.Field.Size;
            }
            else
                currentFieldOffset += size;
        }

        #endregion
        #region StructField

        //We have the caller pass in the size of the field just in case the writer doesn't actually want to allocate an object for whatever it's doing
        public void WriteStructField<T>(string name, T value, int size) where T : IViewableValue
        {
            structWriter.WriteStructField(name, value);

            if (structWriter.Field != null)
                items.Add(structWriter.Field);

            currentFieldOffset += size;
        }

        #endregion
        #region BitField

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe BitFieldWriter WriteBitFields<TSize>(int numFields) where TSize : unmanaged =>
            WriteBitFields(numFields, sizeof(TSize));

        internal unsafe BitFieldWriter WriteBitFields(int numFields, int size)
        {
            //Our child writer can't store a reference to us (and even though we're both ref structs, it seems to me that trying to assign ourselves still creates a copy).
            //So pre-emptively increase the number of bytes written; our child writer will then assert that the specified number of bytes is what was written
            var off = structWriter.ParentOffset + currentFieldOffset;
            currentFieldOffset += size;
            var buffer = items.GetBuffer(numFields);
            return new BitFieldWriter(off, buffer, size, structWriter.ViewWriter._fileAccessor);
        }

        #endregion

        internal unsafe void WritePagedValue(int startRelativeOffset, PagedMemoryBlock block, SymTypeList value)
        {
            throw new NotImplementedException();
        }

        //Should only be used for OBJ files
        public unsafe void WriteValue(int offset, SymTypeList value)
        {
            var viewWriter = structWriter.ViewWriter;

            var dispatcher = viewWriter.SymTypeDispatcher;

            ref var items = ref this.items;

            var startOffset = offset;

            var oldOffset = viewWriter.UnmanagedOffset;
            viewWriter.UnmanagedOffset = offset;

            foreach (var item in value)
            {
                var view = dispatcher.Dispatch(item);
                items.Add(view);
                offset += view.Size;
                viewWriter.UnmanagedOffset = offset;
            }

            currentFieldOffset += viewWriter.UnmanagedOffset - startOffset;

            viewWriter.UnmanagedOffset = oldOffset;
        }

        public bool NeedAlignment(int target, out int required)
        {
            var size = Size;
            var alignedSize = ((int) size + (target - 1)) & (~(target - 1));

            required = alignedSize - size;
            return required != 0;
        }

        public void Align(int target)
        {
            if (NeedAlignment(target, out var required))
            {
                var globalOffset = structWriter.ParentOffset + currentFieldOffset;
                var views = structWriter.ViewWriter.CreateByteBlob(ref globalOffset, required);
                currentFieldOffset = globalOffset - structWriter.ParentOffset;
                items.AddRange(views);
            }
        }

        public void AlignMax(int target, int structLength)
        {
            var required = structLength - Size;

            //The length may or may not be aligned, the nature of that alignment may or may not be even.
            //e.g. the length could be 59 bytes and 58 were used, so if you align to 60 you've now overcorrected!
            if (required != 0)
            {
                var globalOffset = structWriter.ParentOffset + currentFieldOffset;
                var views = structWriter.ViewWriter.CreateByteBlob(ref globalOffset, required);
                currentFieldOffset = globalOffset - structWriter.ParentOffset;
                items.AddRange(views);
            }
        }

        public void Pad(int length)
        {
            var globalOffset = structWriter.ParentOffset + currentFieldOffset;
            var views = structWriter.ViewWriter.CreateByteBlob(ref globalOffset, length);
            currentFieldOffset = globalOffset - structWriter.ParentOffset;
            items.AddRange(views);
        }

        public IView[] ToArray() => items.ToArray();

        [Conditional("DEBUG")]
        public void VerifyLength(int length)
        {
            Debug.Assert(currentFieldOffset == length, $"Length of struct was not correct");
        }

        public void Dispose()
        {
            items.Dispose();
        }
    }
}
