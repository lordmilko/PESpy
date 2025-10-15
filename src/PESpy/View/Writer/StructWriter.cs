using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.PDB;
using Enum = System.Enum;
using SN = PESpy.PDB.SN;

namespace PESpy.View
{
    public partial class ViewWriter
    {
        internal ref struct StructWriter
        {
            private string structName;
            private ViewKind kind;
            private int startOffset;
            private int currentOffset;
            private ViewWriter viewWriter;
            private List<IView> fields;
            private bool shouldAdd;

#if DEBUG
            private HashSet<long> globalFields => viewWriter.globalFields;
#endif

            public int Size => currentOffset - startOffset;

            internal StructWriter(string name, int startOffset, ViewKind kind, ViewWriter viewWriter, bool shouldAdd)
            {
                structName = name;
                this.startOffset = startOffset;
                this.kind = kind;
                currentOffset = startOffset;
                this.viewWriter = viewWriter;
                fields = viewWriter.RentList();
                this.shouldAdd = shouldAdd;
            }

            internal StructWriter(int startOffset, ViewWriter viewWriter)
            {
                this.startOffset = startOffset;
                currentOffset = startOffset;
                this.viewWriter = viewWriter;
                fields = viewWriter.RentList();

                kind = default;
                shouldAdd = default;
                structName = default;
            }

            public void WriteField(string name, byte value) =>
                WriteFieldInternal(name, value, sizeof(byte));

            #region Int16

            /// <summary>
            /// Writes an <see cref="IFieldView"/> to a <see cref="StructView"/>.
            /// </summary>
            /// <param name="name">The name of the field to write.</param>
            /// <param name="value">The value to use in the field.</param>
            public void WriteField(string name, short value) =>
                WriteFieldInternal(name, value, sizeof(short));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, ushort value) =>
                WriteFieldInternal(name, value, sizeof(short));

            #endregion
            #region Int32

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, int value) =>
                WriteFieldInternal(name, value, sizeof(int));

            public void Write7BitField(string name, int value, int size) =>
                WriteFieldInternal(name, value, size);

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, uint value) =>
                WriteFieldInternal(name, value, sizeof(int));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, Timestamp value) =>
                WriteFieldInternal(name, value, sizeof(int));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, long value) =>
                WriteFieldInternal(name, value, sizeof(long));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, ulong value) =>
                WriteFieldInternal(name, value, sizeof(long));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, float value) =>
                WriteFieldInternal(name, value, sizeof(float));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, double value) =>
                WriteFieldInternal(name, value, sizeof(double));

            public void WriteField(string name, PN value) =>
                WriteFieldInternal(name, value, sizeof(uint));

            public void WriteField(string name, SN value) =>
                WriteFieldInternal(name, value, sizeof(ushort));

            public void WriteField(string name, SN value, int size) =>
                WriteFieldInternal(name, value, size);

            public void WriteField(string name, IMOD value) =>
                WriteFieldInternal(name, value, sizeof(ushort));

            public void WriteField(string name, ISECT value) =>
                WriteFieldInternal(name, value, sizeof(ushort));

            public void WriteValue(LEAF_ENUM_e value, int size) =>
                WriteValue(currentOffset, value, size, ViewKind.Value);

            public unsafe void WriteNumericData(string name, byte* pValue)
            {
                var leaf = *(LEAF_ENUM_e*) pValue;

                if (leaf < LEAF_ENUM_e.LF_NUMERIC) //0x8000
                {
                    //The data does not contain a special leaf
                    WriteField(name, (ushort) leaf);
                    return;
                }

                WriteValue(leaf, sizeof(ushort));

                pValue += sizeof(ushort);

                switch (leaf) //LF_NUMERIC and LF_CHAR are both defined as 0x8000, but LF_NUMERIC is the semantic item that indicates "this is the beginning of the special kind range"
                {
                    case LEAF_ENUM_e.LF_CHAR:
                        WriteField(name, *pValue);
                        break;

                    case LEAF_ENUM_e.LF_SHORT:
                        WriteField(name, *(short*) pValue);
                        break;

                    case LEAF_ENUM_e.LF_USHORT:
                        WriteField(name, *(ushort*) pValue);
                        break;

                    case LEAF_ENUM_e.LF_LONG:
                        WriteField(name, *(int*) pValue);
                        break;

                    case LEAF_ENUM_e.LF_ULONG:
                        WriteField(name, *(uint*) pValue);
                        break;

                    case LEAF_ENUM_e.LF_REAL32:
                    case LEAF_ENUM_e.LF_REAL64:
                    case LEAF_ENUM_e.LF_REAL80:
                    case LEAF_ENUM_e.LF_REAL128:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_QUADWORD:
                        WriteField(name, *(long*) pValue);
                        break;

                    case LEAF_ENUM_e.LF_UQUADWORD:
                        WriteField(name, *(ulong*) pValue);
                        break;

                    case LEAF_ENUM_e.LF_REAL48:
                    case LEAF_ENUM_e.LF_COMPLEX32:
                    case LEAF_ENUM_e.LF_COMPLEX64:
                    case LEAF_ENUM_e.LF_COMPLEX80:
                    case LEAF_ENUM_e.LF_COMPLEX128:
                    case LEAF_ENUM_e.LF_VARSTRING:
                    case LEAF_ENUM_e.LF_OCTWORD:
                    case LEAF_ENUM_e.LF_UOCTWORD:
                    case LEAF_ENUM_e.LF_DECIMAL:
                    case LEAF_ENUM_e.LF_DATE:
                    case LEAF_ENUM_e.LF_UTF8STRING:
                    case LEAF_ENUM_e.LF_REAL16:
                        throw new NotImplementedException();

                    default:
                        throw new NotImplementedException();
                }
            }

            #endregion
            #region Enum

            public void WriteField<T>(string name, T value, int size) where T : Enum =>
                WriteFieldInternal(name, value, size);

            #endregion
            #region Array

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, NativeSpan<byte> value)
            {
                //A field with a length of 0 will calculate itself as having a negative size (since if it starts at 0 and is 2 large it ends at 1)
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length);
            }

            public void WriteField(string name, byte[] value)
            {
                Debug.Assert(value.Length != 0);

                WriteFieldInternal(name, value, value.Length);
            }

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, NativeSpan<short> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 2);
            }

            public void WriteField(string name, NativeSpan<ushort> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 2);
            }

            public void WriteField(string name, ushort[] value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 2);
            }

            public void WriteField(string name, NativeSpan<PN> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 4);
            }

            public void WriteField(string name, NativeSpan<int> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 4);
            }

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, int[] value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 4);
            }

            public void WriteField(string name, PN[] value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 4);
            }

            public void WriteField(string name, CV_typ_t[] value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 4);
            }

            public void WriteField(string name, CV_ItemId[] value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 4);
            }

            public void WriteUTF8NullTerminatedField(string name, string[] value)
            {
                if (value.Length == 0)
                    return;

                var size = 0;

                foreach (var item in value)
                    size += item.Length + 1;

                WriteFieldInternal(name, value, size);
            }

            #endregion
            #region Pointer

            public void WritePointerField(string name, long value)
            {
                if (((PEViewWriter) viewWriter).Is32Bit)
                    WriteFieldInternal(name, (int) value, sizeof(int));
                else
                    WriteFieldInternal(name, value, sizeof(long));
            }

            public void WritePointerField(string name, ulong value)
            {
                if (((PEViewWriter) viewWriter).Is32Bit)
                    WriteFieldInternal(name, (uint) value, sizeof(int));
                else
                    WriteFieldInternal(name, value, sizeof(long));
            }

            public void WriteVAPointerField<T>(string name, VA<T> value) where T : IViewable, IValue
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                //Assert that the global has already been written
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteVAPointerField<T>(string name, VA<T[]> value) where T : IViewable, IValue
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                //Assert that the global has already been written
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteSmallVAPointerField<T>(string name, VA<T> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedAddress);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteLargeVAPointerField<T>(string name, VA<T> value) where T : IViewable, IValue
            {
                WriteField(name, (long) value.ListedAddress);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteSmallVAPointerField<T>(string name, VA<T[]> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedAddress);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteVAPointerField(string name, VA<long> value, ViewKind valueKind)
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteVAPointerField(string name, VA<ulong> value, ViewKind valueKind)
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteVAPointerField(string name, VA<ulong[]> value, ViewKind valueKind)
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteVAPointerField(string name, VA<int[]> value, ViewKind valueKind)
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            #endregion
            #region String

            public void WriteAnsiNullTerminatedField(string name, string value)
            {
                WriteFieldInternal(name, value, value.Length + 1);
            }

            public void WriteAnsiNullTerminatedField(string name, AnsiString value)
            {
                WriteFieldInternal(name, value, value.Length + 1);
            }

            public void WriteAnsiFixedLengthField(string name, FixedAnsiString value)
            {
                WriteFieldInternal(name, value, value.Length);
            }

            public unsafe void WriteLengthPrefixedAnsiField(string name, FixedAnsiString value)
            {
                WriteValue(currentOffset, (byte) value.Length, 1, ViewKind.String);
                WriteFieldInternal(name, value, value.Length);
            }

            public void WriteUTF8NullTerminatedField(string name, string value)
            {
                WriteFieldInternal(name, value, value.Length + 1);
            }

            public void WriteUTF8NullTerminatedField(string name, Utf8String value)
            {
                WriteFieldInternal(name, value, value.Length + 1);
            }

            public void WriteUTF8FixedLengthField(string name, FixedUtf8String value)
            {
                WriteFieldInternal(name, value, value.Length);
            }

            public void WriteUTF16NullTerminatedField(string name, string value)
            {
                WriteFieldInternal(name, value, (value.Length + 1) * 2);
            }

            public void WriteUTF16NullTerminatedField(string name, Utf16String value)
            {
                WriteFieldInternal(name, value, (value.Length + 1) * 2);
            }

            public void WriteNullTerminatedField(string name, NullTerminatedString value)
            {
                switch (value.Kind)
                {
                    case StringKind.ANSI:
                    case StringKind.UTF8:
                        WriteFieldInternal(name, value, value.Length + 1);
                        break;

                    default:
                        Debug.Assert(value.Kind == StringKind.UTF16);
                        WriteFieldInternal(name, value, (value.Length + 1) * 2);
                        break;
                }
            }

            public void WriteNullPaddedAnsiField(string name, FixedAnsiString value, int length) => WriteFieldInternal(name, value, length);

            public void WriteNullPaddedUTF8Field(string name, string value, int length) => WriteFieldInternal(name, value, length);

            public void WriteNullPaddedUTF8Field(string name, FixedUtf8String value, int length) => WriteFieldInternal(name, value, length);

            public void WriteSymStringField(string name, SymString value) => WriteFieldInternal(name, value, value.Length + 1);

            public void WriteUTF16Field(string name, string value, int numChars)
            {
                WriteFieldInternal(name, value, numChars * 2);
            }

            public void WriteUTF16Field(string name, FixedUtf16String value, int numChars)
            {
                WriteFieldInternal(name, value.ToString(), numChars * 2);
            }

            #endregion
            #region RVA

            public void WriteRVAPointerField(string name, RVA<long> value)
            {
                WriteField(name, (int) value.ListedOffset);

#if DEBUG
                if (value.IsValid && value.ListedOffset != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedOffset));
                }
#endif
            }

            public void WriteRVAField(string name, RVA<ulong[]> value)
            {
                WritePointerField(name, (int) value.ListedOffset);

#if DEBUG
                if (value.IsValid && value.ListedOffset != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedOffset));
                }
#endif
            }

            public void WriteRVAAnsiNullTerminatedField(string name, RVA<string> value)
            {
                WritePointerField(name, (int) value.ListedOffset);

#if DEBUG
                if (value.IsValid && value.ListedOffset != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedOffset));
                }
#endif
            }

            public void WriteVAAnsiNullTerminatedField(string name, VA<string> value)
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                if (value.IsValid && value.ListedAddress != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteVAAnsiNullTerminatedField(string name, VA<AnsiString> value)
            {
                WritePointerField(name, value.ListedAddress);

#if DEBUG
                if (value.IsValid && value.ListedAddress != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedAddress));
                }
#endif
            }

            public void WriteRVAAnsiNullTerminatedField(string name, RVA<AnsiString> value)
            {
                WriteField(name, (int) value.ListedOffset);

#if DEBUG
                if (value.IsValid && value.ListedOffset != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedOffset), $"Did not write field '{name}' in WriteGlobals");
                }
#endif
            }

            public void WriteRVAField<T>(string name, RVA<T> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedOffset);

#if DEBUG
                if (value.IsValid)
                {
                    Debug.Assert(globalFields.Contains(value.ListedOffset));
                }
#endif
            }

            public void WriteRVAField<T>(string name, RVA<T[]> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedOffset);

#if DEBUG
                if (value.IsValid && value.ListedOffset != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedOffset));
                }
#endif
            }

            #endregion

            public void WriteField(string name, CV_lvar_attr value) =>
                WriteFieldInternal(name, value, sizeof(int) + sizeof(short) + sizeof(short));

            public void WriteField(string name, CV_RANGEATTR value) =>
                WriteFieldInternal(name, value, sizeof(short));

            public void WriteField(string name, CV_GENERIC_FLAG value) =>
                WriteFieldInternal(name, value, sizeof(short));

            public void WriteField(string name, CV_SEPCODEFLAGS value) =>
                WriteFieldInternal(name, value, sizeof(int));

            public void WriteField(string name, CV_LVAR_ADDR_RANGE value) =>
                WriteFieldInternal(name, value, sizeof(int) + sizeof(short) + sizeof(short));

            public void WriteField(string name, NativeSpan<CV_LVAR_ADDR_GAP> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, (sizeof(short) + sizeof(short)) * value.Length);
            }

            public void WriteField(string name, NativeSpan<CV_typ_t> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * sizeof(int));
            }

            public void WriteField(string name, NativeSpan<CV_ItemId> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * sizeof(int));
            }

            public void WriteField(string name, NativeSpan<CV_typ16_t> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * sizeof(short));
            }

            public void WriteField(string name, BinaryAnnotationList value)
            {
                throw new NotImplementedException();
            }

            public void WriteByteBlob(int offset, int size)
            {
                fields.AddRange(viewWriter.CreateByteBlob(ref offset, size));
                currentOffset += size;
            }

            public void WriteField(string name, Guid guid) =>
                WriteFieldInternal(name, guid, 16);

            /// <summary>
            /// Writes an <see cref="IStructView"/> inside of a <see cref="FieldView{T}"/>.<para/>
            /// This contrasts with <see cref="WriteInline{T}(T)"/> which writes an <see cref="IStructView"/> directly
            /// without a containing <see cref="FieldView{T}"/>.
            /// </summary>
            /// <typeparam name="T">The type of structure to write.</typeparam>
            /// <param name="name">The name of the field to write.</param>
            /// <param name="value">The structure to encapsulate in the field.</param>
            public void WriteStructField<T>(string name, T value) where T : IViewable
            {
                var oldOffset = viewWriter.UnmanagedOffset;
                viewWriter.UnmanagedOffset = currentOffset;
                var view = (IStructView) value.WriteStruct(viewWriter)!;
                viewWriter.UnmanagedOffset = oldOffset;

                WriteFieldInternal(name, view, view.Size);
            }

            public void WriteStructField<T>(string name, T[] value) where T : IViewable
            {
                var oldOffset = viewWriter.UnmanagedOffset;
                viewWriter.UnmanagedOffset = currentOffset;

                using var results = new PooledList<IView>();

                var size = 0;

                for (var i = 0; i < value.Length; i++)
                {
                    ref var item = ref value[i];

                    var result = (IStructView) item.WriteStruct(viewWriter)!;

                    if (result != null)
                    {
                        size += result.Size;
                        results.Add(result);
                    }
                }

                viewWriter.UnmanagedOffset = oldOffset;

                WriteFieldInternal(name, results.ToArray(), size);
            }

            public void WriteInline<T>(T value) where T : IViewable
            {
                //If this is a PDB File, you can have an outer struct with 3 fields: A, B, C. A is an int, so uses
                //the chunk of the parent. B. is a complex struct so gets a new chunk all of its own, and then C
                //is also an int. If A crosses a page boundary, B's Offset will report the location of the new page,
                //whereas C won't, because it's using the parent struct's chunk. C will only get its correct offset
                //during splitting
                Debug.Assert(viewWriter is PDBViewWriter || ((IValue) value).Offset == currentOffset);

                var startIndex = fields.Count;

                var oldOffset = viewWriter.UnmanagedOffset;
                viewWriter.UnmanagedOffset = currentOffset;
                var result = value.WriteStruct(viewWriter);

                if (result != null)
                    fields.Add(result);

                viewWriter.UnmanagedOffset = oldOffset;

                var fieldsSize = 0;

                for (var i = startIndex; i < fields.Count; i++)
                    fieldsSize += fields[i].Size;

                currentOffset += fieldsSize;
            }

            public void WriteInline<T>(T[] value) where T : IViewable
            {
                var startIndex = fields.Count;

                viewWriter.Push(fields);

                var oldOffset = viewWriter.UnmanagedOffset;

                for (var i = 0; i < value.Length; i++)
                {
                    viewWriter.UnmanagedOffset = currentOffset;
                    value[i].WriteView(viewWriter);
                    currentOffset += fields[startIndex + i].Size;
            public void WriteInline(in GuardCFFunctionTable value)
            public void WriteInline<T>(RVA<T> value, ViewKind kind) where T : IViewable, IValue
            {
                fields.Add(new ValueView<int>(currentOffset, value.ListedOffset, sizeof(int), kind));
                currentOffset += sizeof(int);

#if DEBUG
                if (value.IsValid && value.ListedOffset != 0)
                {
                    Debug.Assert(globalFields.Contains(value.ListedOffset));
                }
#endif
            }

            public void WriteInline<TParent, TChild>(in TParent value) where TParent : IEnumerable<TChild> where TChild : IViewable
            {
                var startIndex = fields.Count;
                var oldOffset = viewWriter.UnmanagedOffset;

                foreach (var item in value)
                {
                    viewWriter.UnmanagedOffset = currentOffset;
                    var result = ((IViewable) item).WriteStruct(viewWriter);

                    if (result != null)
                    {
                        fields.Add(result);
                        currentOffset += result.Size;
                    }
                }

                viewWriter.UnmanagedOffset = oldOffset;

                viewWriter.Pop();
            }

            public void WriteInline<T>(NativeSpan<T> value) where T : unmanaged, IViewable
            {
                var startIndex = fields.Count;

                viewWriter.Push(fields);

                var oldOffset = viewWriter.UnmanagedOffset;

                for (var i = 0; i < value.Length; i++)
                {
                    viewWriter.UnmanagedOffset = currentOffset;
                    var child = value[i].WriteStruct(viewWriter);

                    if (child != null)
                    {
                        fields.Add(child);
                        currentOffset += child.Size;
                    }
                }

                viewWriter.UnmanagedOffset = oldOffset;

                viewWriter.Pop();
            }

            public unsafe void WriteInline<T>(RawValue<T> value, ViewKind kind) where T : unmanaged
            {
                fields.Add(new ValueView<T>(value.Offset, value.Value, sizeof(T), kind));
                currentOffset += sizeof(T);
            }

            public void WriteInlineAnsiNullTerminated(RawValue<string> value)
            {
                var size = value.Value.Length + 1;
                fields.Add(new ValueView<string>(value.Offset, value.Value, size, ViewKind.String));
                currentOffset += size;
            }

            public void WriteInlineAnsiNullTerminated(RawValue<string>[] value)
            {
                foreach (var item in value)
                    WriteInlineAnsiNullTerminated(item);
            }

            public void WriteInlineAnsiNullTerminated(RawValue<AnsiString> value)
            {
                var size = value.Value.Length + 1;
                Debug.Assert(currentOffset == value.Offset);
                fields.Add(new ValueView<AnsiString>(value.Offset, value.Value, size, ViewKind.String));
                currentOffset += size;
            }

            public void WriteInlineAnsiNullTerminated(AnsiString value)
            {
                var size = value.Length + 1;
                fields.Add(new ValueView<AnsiString>(currentOffset, value, size, ViewKind.String));
                currentOffset += size;
            }

            public void WriteInlineFixedAnsiString(FixedAnsiString value)
            {
                var size = value.Length;
                fields.Add(new ValueView<FixedAnsiString>(currentOffset, value, size, ViewKind.String));
                currentOffset += size;
            }

            public void WriteInlineUtf16NullTerminated(FixedUtf16String value, int size)
            {
                fields.Add(new ValueView<FixedUtf16String>(currentOffset, value, size, ViewKind.String));
                currentOffset += size;
            }

            //We're pretending we're UTF8 because a newer version uses UTF8 but we're actually ANSI
            public unsafe void WriteInlineLengthPrefixedAnsiString(RawValue<FixedUtf8String> value)
            {
                Debug.Assert(currentOffset == value.Offset); //We only pass the inner string to WriteInlineFixedAnsiString, so our offset bookkeeping better line up!
                WriteValue(value.Offset, (byte) value.Value.Length, 1, ViewKind.String);
                WriteInlineFixedAnsiString(new FixedAnsiString(value.Value.Value, value.Value.Length)); //+1 for the prefixed length
            }

            public void WriteInlineAnsiNullTerminated(RawValue<AnsiString>[] value)
            {
                foreach (var item in value)
                    WriteInlineAnsiNullTerminated(item);
            }

            public void WriteInlineUtf8NullTerminated(RawValue<Utf8String> value)
            {
                var size = value.Value.Length + 1;
                fields.Add(new ValueView<Utf8String>(value.Offset, value.Value, size, ViewKind.String));
                currentOffset += size;
            }

            //For when it's meant to be null terminated but we've had to convert it to fixed e.g. because older versions require fixed so we're pretending we're fixed too
            public unsafe void WriteInlineUtf8NullTerminated(RawValue<FixedUtf8String> value)
            {
                var size = value.Value.Length + 1;
                fields.Add(new ValueView<Utf8String>(value.Offset, new Utf8String(value.Value.Value), size, ViewKind.String));
                currentOffset += size;
            }

            public void WriteInlineUtf8NullTerminated(RawValue<Utf8String>[] value)
            {
                foreach (var item in value)
                    WriteInlineUtf8NullTerminated(item);
            }

            public unsafe BitFieldWriter WriteBitFields<TSize>() where TSize : unmanaged
            {
                //Our child writer can't store a reference to us (and even though we're both ref structs, it seems to me that trying to assign ourselves still creates a copy).
                //So pre-emptively increase the number of bytes written; our child writer will then assert that the specified number of bytes is what was written
                var off = currentOffset;
                var bytes = sizeof(TSize);
                currentOffset += bytes;
                return new BitFieldWriter(off, fields, bytes);
            }

            /// <summary>
            /// Creates a writer around a synthetic structure that encapsulates two or more bitfield values.
            /// </summary>
            /// <param name="name">The name to give the synthetic structure.</param>
            /// <param name="kind">The kind of the synthetic structure.</param>
            /// <returns>A writer that creates a synthetic structure around two or more bitfield values.</returns>
            public unsafe StructBitFieldWriter WriteStructBitField<TSize>(FixedUtf8String name, ViewKind kind) where TSize : unmanaged
            {
                //Our child writer can't store a reference to us (and even though we're both ref structs, it seems to me that trying to assign ourselves still creates a copy).
                //So pre-emptively increase the number of bytes written; our child writer will then assert that the specified number of bytes is what was written
                var off = currentOffset;
                var bytes = sizeof(TSize);
                currentOffset += bytes;
                return new StructBitFieldWriter(name, off, kind, fields, bytes, viewWriter);
            }

            private void WriteFieldInternal<T>(string name, T value, int size)
            {
                //We will be writing a lot of primative values (Int16's, Int32's, etc). We do not want each value to be boxed,
                //as that will cause a large number of (duplicated) allocations. The CLR does not know that the number "2" has been
                //boxed before, so you'll have a lot of wasted memory for boxes storing the same value.

                fields.Add(new FieldView<T>(currentOffset, name, value, size));
                currentOffset += size;
            }

            public void WriteValue<T>(int offset, in T value, int size, ViewKind kind)
            {
                fields.Add(new ValueView<T>(offset, value, size, kind));
                currentOffset += size;
            }

            #region Paged

            /* We have an array of something that is known to exist at a given offset and is not wrapped in an IValue. We want to list the individual values separately
             * in the output, however the array itself may have spanned multiple pages. We will therefore do the math in figuring out which page each value starts in.
             * In the case where a given value extends past the end of a given page, this is OK: during merging we will detect this and convert the value into a split value */

            public unsafe void WritePagedValue(int startRelativeOffset, PagedMemoryBlock block, SymTypeList value)
            {
                using var p = viewWriter.CreatePagedWriter(startRelativeOffset, block, false);

                foreach (var item in value)
                    p.WriteValue(item, SymType.GetSymbolLength(item, value.symbolAccessor), ViewKind.SymType);
            }

            #endregion

            //Should only be used for OBJ files
            public unsafe void WriteValue(int offset, SymTypeList value)
            {
                var written = 0;

                foreach (var item in value)
                {
                    var totalLength = SymType.GetSymbolLength(item, value.symbolAccessor);
                    fields.Add(new ValueView<SymType>(offset + written, item, totalLength, ViewKind.SymType));
                    written += totalLength;
                }

                currentOffset += written;
            }

            public bool NeedAlignment(int target, out int required)
            {
                var size = Size;
                var alignedSize = ((int) size + (target - 1)) & (~(target - 1));

                required = alignedSize - size;
                return required != 0;
            }

            //There is a difference between aligning to a addresses and simply aligning the size.
            //A value might be on an unaligned address but still need to have an aligned size
            public void Align(int target)
            {
                if (NeedAlignment(target, out var required))
                {
                    var views = viewWriter.CreateByteBlob(ref currentOffset, required);
                    fields.AddRange(views);
                }
            }

            public void AlignMax(int target, int structLength)
            {
                //The length may or may not be aligned, the nature of that alignment may or may not be even.
                //e.g. the length couldbe 59 bytes and 58 were used, so if you align to 60 you've now overcorrected!
                if (NeedAlignment(target, out var required))
                {
                    required = Math.Min(required, structLength - Size);
                    var views = viewWriter.CreateByteBlob(ref currentOffset, required);
                    fields.AddRange(views);
                }
            }

            public void Pad(int length)
            {
                var views = viewWriter.CreateByteBlob(ref currentOffset, length);
                fields.AddRange(views);
            }

            [Conditional("DEBUG")]
            public void VerifyLength(int length)
            {
                Debug.Assert(Size == length, $"Length of {structName} was not correct");
            }

            public IView[] ToArray() => fields.ToArray();

            public void Dispose()
            {
                if (shouldAdd)
                {
                    var structView = new StructView(startOffset, structName, fields.ToArray(), Size, kind);

                    viewWriter.AddView(structView);    
                }
                
                viewWriter?.ReturnList(fields);
            }
        }
    }
}
