using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClrDebug.PDB;
using PESpy.PDB;
using Enum = System.Enum;
using SN = PESpy.PDB.SN;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    public partial class ViewWriter
    {
        internal ref struct StructWriter
        {
            private string structName;
            private ViewKind kind;
            private RawOffset startOffset;
            private RawOffset currentOffset;
            private ViewWriter viewWriter;
            private List<IView> fields;
            private bool shouldAdd;

            public RawOffset Size => currentOffset - startOffset;

            internal StructWriter(string name, RawOffset startOffset, ViewKind kind, ViewWriter viewWriter, bool shouldAdd)
            {
                structName = name;
                this.startOffset = startOffset;
                this.kind = kind;
                currentOffset = startOffset;
                this.viewWriter = viewWriter;
                fields = viewWriter.RentList();
                this.shouldAdd = shouldAdd;
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

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, uint value) =>
                WriteFieldInternal(name, value, sizeof(int));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, long value) =>
                WriteFieldInternal(name, value, sizeof(long));

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, ulong value) =>
                WriteFieldInternal(name, value, sizeof(long));

            public void WriteField(string name, PN value) =>
                WriteFieldInternal(name, value, sizeof(uint));

            public void WriteField(string name, SN value) =>
                WriteFieldInternal(name, value, sizeof(ushort));

            public void WriteField(string name, IMOD value) =>
                WriteFieldInternal(name, value, sizeof(ushort));

            public void WriteField(string name, ISECT value) =>
                WriteFieldInternal(name, value, sizeof(ushort));

            #endregion
            #region Enum

            public void WriteField<T>(string name, T value, int size) where T : Enum =>
                WriteFieldInternal(name, value, size);

            #endregion
            #region Array

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, Span<byte> value)
            {
                //A field with a length of 0 will calculate itself as having a negative size (since if it starts at 0 and is 2 large it ends at 1)
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value.ToArray(), value.Length);
            }

            /// <inheritdoc cref="WriteField(string, short)"/>
            public void WriteField(string name, Span<short> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value.ToArray(), value.Length * 2);
            }

            public void WriteField(string name, ushort[] value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value, value.Length * 2);
            }

            public void WriteField(string name, Span<int> value)
            {
                if (value.Length == 0)
                    return;

                WriteFieldInternal(name, value.ToArray(), value.Length * 4);
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
                WriteField(name, value.ListedAddress);

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.Value);
            }

            public void WriteSmallVAPointerField<T>(string name, VA<T> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedAddress);

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.Value);
            }

            public void WriteSmallVAPointerField<T>(string name, VA<T[]> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedAddress);

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.Value);
            }

            public void WriteVAPointerField(string name, VA<long> value, ViewKind valueKind)
            {
                WriteField(name, value.ListedAddress);

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, sizeof(long), valueKind);
            }

            public void WriteVAPointerField(string name, VA<ulong> value, ViewKind valueKind)
            {
                WriteField(name, value.ListedAddress);

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, sizeof(long), valueKind);
            }

            public void WriteVAPointerField(string name, VA<long[]> value, ViewKind valueKind)
            {
                WriteField(name, value.ListedAddress);

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(long), valueKind);
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

            public void WriteUTF8NullTerminatedField(string name, string value)
            {
                WriteFieldInternal(name, value, value.Length + 1);
            }

            public void WriteUTF16NullTerminatedField(string name, string value)
            {
                WriteFieldInternal(name, value, (value.Length + 1) * 2);
            }

            public void WriteNullPaddedUTF8Field(string name, string value, int length) => WriteFieldInternal(name, value, length);

#if PEFAST
            public void WriteNullPaddedUTF8Field(string name, Utf8String value, int length) => WriteFieldInternal(name, value, length);
#endif

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

                if (value.IsValid && value.ListedOffset != 0)
                {
                    if (((PEViewWriter) viewWriter).Is32Bit)
                        viewWriter.WriteGlobal(value.ActualOffset, (int) value.Value, sizeof(int), default);
                    else
                        viewWriter.WriteGlobal(value.ActualOffset, value.Value, sizeof(long), default);
                }
            }

            public void WriteRVAField(string name, RVA<ulong[]> value)
            {
                WriteField(name, (int) value.ListedOffset);

                if (value.IsValid && value.ListedOffset != 0)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(long), default);
            }

            public void WriteRVAAnsiNullTerminatedField(string name, RVA<string> value)
            {
                WriteField(name, (int) value.ListedOffset);

                if (value.IsValid && value.ListedOffset != 0)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, ViewKind.String);
            }

            public void WriteVAAnsiNullTerminatedField(string name, VA<string> value)
            {
                WriteField(name, value.ListedAddress);

                if (value.IsValid && value.ListedAddress != 0)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, ViewKind.String);
            }

#if PEFAST
            public void WriteRVAAnsiNullTerminatedField(string name, RVA<AnsiString> value)
            {
                WriteField(name, (int) value.ListedOffset);

                if (value.IsValid && value.ListedOffset != 0)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, ViewKind.String);
            }
#endif

            public void WriteRVAField<T>(string name, RVA<T> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedOffset);

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.Value);
            }

            public void WriteRVAField<T>(string name, RVA<T[]> value) where T : IViewable, IValue
            {
                WriteField(name, (int) value.ListedOffset);

                if (value.IsValid && value.ListedOffset != 0)
                    viewWriter.WriteGlobal(value.Value);
            }

            #endregion

            public void WriteField(string name, Guid guid) =>
                WriteFieldInternal(name, guid, 16);

            /// <summary>
            /// Writes a <see cref="StructView"/> inside of a <see cref="FieldView{T}"/>.<para/>
            /// This contrasts with <see cref="WriteInline{T}(T)"/> which writes a <see cref="StructView"/> directly
            /// without a containing <see cref="FieldView{T}"/>.
            /// </summary>
            /// <typeparam name="T">The type of structure to write.</typeparam>
            /// <param name="name">The name of the field to write.</param>
            /// <param name="value">The structure to encapsulate in the field.</param>
            public void WriteStructField<T>(string name, T value) where T : IViewable
            {
                var view = (StructView) viewWriter.WriteIntercepted(value);

                WriteFieldInternal(name, view, view.Size);
            }

            public void WriteInline<T>(T value) where T : IViewable
            {
                var startIndex = fields.Count;

                viewWriter.Push(fields);
                value.WriteView(viewWriter);

                for (var i = startIndex; i < fields.Count; i++)
                    currentOffset += fields[i].Size;

                viewWriter.Pop();
            }

            public void WriteInline<T>(T[] value) where T : IViewable
            {
                var startIndex = fields.Count;

                viewWriter.Push(fields);

                for (var i = 0; i < value.Length; i++)
                    value[i].WriteView(viewWriter);

                for (var i = startIndex; i < fields.Count; i++)
                    currentOffset += fields[i].Size;

                viewWriter.Pop();
            }

            public void WriteInlineAnsiNullTerminated(RawValue<string> value)
            {
                var size = value.Value.Length + 1;
                fields.Add(new ValueView<string>(value.Offset, value.Value, size, ViewKind.Value));
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
                fields.Add(new ValueView<AnsiString>(value.Offset, value.Value, size, ViewKind.Value));
                currentOffset += size;
            }

            public BitFieldWriter WriteBitFields<TSize>()
            {
                //Our child writer can't store a reference to us (and even though we're both ref structs, it seems to me that trying to assign ourselves still creates a copy).
                //So pre-emptively increase the number of bytes written; our child writer will then assert that the specified number of bytes is what was written
                var off = currentOffset;
                var bytes = Marshal.SizeOf<TSize>();
                currentOffset += bytes;
                return new BitFieldWriter(off, fields, bytes);
            }

            /// <summary>
            /// Creates a writer around a synthetic structure that encapsulates two or more bitfield values.
            /// </summary>
            /// <param name="name">The name to give the synthetic structure.</param>
            /// <param name="kind">The kind of the synthetic structure.</param>
            /// <returns>A writer that creates a synthetic structure around two or more bitfield values.</returns>
            public StructBitFieldWriter WriteStructBitField<TSize>(string name, ViewKind kind)
            {
                //Our child writer can't store a reference to us (and even though we're both ref structs, it seems to me that trying to assign ourselves still creates a copy).
                //So pre-emptively increase the number of bytes written; our child writer will then assert that the specified number of bytes is what was written
                var off = currentOffset;
                var bytes = Marshal.SizeOf<TSize>();
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

            public bool NeedAlignment(int target, out int required)
            {
                var alignedOffset = ((int) currentOffset + (target - 1)) & (~(target - 1));

                required = alignedOffset - currentOffset;
                return required != 0;
            }

            public void Align(int target)
            {
                if (NeedAlignment(target, out var required))
                {
                    var views = viewWriter.CreateByteBlob(ref currentOffset, required);
                    fields.AddRange(views);
                }
            }

            [Conditional("DEBUG")]
            public void VerifyLength(int length)
            {
                Debug.Assert(Size == length, $"Length of {structName} was not correct");
            }

            public void Dispose()
            {
                if (shouldAdd)
                {
                    var structView = new StructView(startOffset, structName, fields.ToArray(), Size, kind);

                    viewWriter.AddView(structView);    
                }
                
                viewWriter.ReturnList(fields);
            }
        }
    }
}
