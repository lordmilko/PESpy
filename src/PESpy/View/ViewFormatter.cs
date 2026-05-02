using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PESpy.View
{
    [Flags]
    public enum ViewFormatFlags
    {
        None,
        Range = 1
    }

    public static class ViewFormatter
    {
        public static string Format(IView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                Format(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void Format(IView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            switch (view.ImplKind)
            {
                case ViewImplKind.Asm:
                    FormatAsm((IAsmView) view, flags, ref builder);
                    break;

                case ViewImplKind.BitField:
                    FormatBitField((IBitFieldView) view, flags, ref builder);
                    break;

                case ViewImplKind.ByteBlob:
                    FormatByteBlob((ByteBlobView) view, flags, ref builder);
                    break;

                case ViewImplKind.Field:
                    FormatField((IFieldView) view, flags, ref builder);
                    break;

                case ViewImplKind.Header:
                    FormatHeader((HeaderView) view, flags, ref builder);
                    break;

                case ViewImplKind.LogicalRegion:
                    FormatLogicalRegion((LogicalRegionView) view, flags, ref builder);
                    break;

                case ViewImplKind.Overlay:
                    FormatOverlay((OverlayView) view, flags, ref builder);
                    break;

                case ViewImplKind.File:
                    FormatFile((FileView) view, flags, ref builder);
                    break;

                case ViewImplKind.Section:
                    FormatSection((SectionView) view, flags, ref builder);
                    break;

                case ViewImplKind.Struct:
                    FormatStruct((StructView) view, flags, ref builder);
                    break;

                case ViewImplKind.StructField:
                    FormatStructField((StructFieldView) view, flags, ref builder);
                    break;

                case ViewImplKind.StructArrayField:
                    FormatStructArrayField((StructArrayFieldView) view, flags, ref builder);
                    break;

                case ViewImplKind.Value:
                    FormatValue((IValueView) view, flags, ref builder);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(ViewImplKind)} '{view.ImplKind}'");
            }
        }

        #region Asm

        public static string FormatAsm(IAsmView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatAsm(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }


        public static void FormatAsm(IAsmView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);
            builder.Append(view.Name ?? "<Code>");
        }

        #endregion
        #region BitField

        public static string FormatBitField(IBitFieldView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatBitField(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static string FormatBitField<TValue>(BitFieldView<TValue> view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatBitField(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatBitField(IBitFieldView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            FormatBitFieldInternal(view, flags, ref builder);
            builder.Append(view.Value.ToString());
        }

        public static void FormatBitField<T>(BitFieldView<T> view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            FormatBitFieldInternal(view, flags, ref builder);

            var value = view.Value;

            if (typeof(T) == typeof(byte))
            {
                var val = Unsafe.As<T, byte>(ref value);
                builder.Append(val);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                var val = Unsafe.As<T, sbyte>(ref value);
                builder.Append(val);
            }
            else if (typeof(T) == typeof(short))
            {
                var val = Unsafe.As<T, short>(ref value);
                builder.Append(val);
            }
            else if (typeof(T) == typeof(ushort))
            {
                var val = Unsafe.As<T, ushort>(ref value);
                builder.Append(val);
            }
            else if (typeof(T) == typeof(int))
            {
                var val = Unsafe.As<T, int>(ref value);
                builder.Append(val);
            }
            else if (typeof(T) == typeof(uint))
            {
                var val = Unsafe.As<T, uint>(ref value);
                builder.Append(val);
            }
            else if (typeof(T) == typeof(long))
            {
                var val = Unsafe.As<T, long>(ref value);
                builder.Append(val);
            }
            else if (typeof(T) == typeof(ulong))
            {
                var val = Unsafe.As<T, ulong>(ref value);
                builder.Append(val);
            }
            else
                builder.Append(view.Value.ToString()); //We wouldn't be writing bool, so this is likely an enum
        }

        public static void FormatBitFieldInternal(IBitFieldView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);
            builder.Append(view.Name);
            builder.Append(':');
            builder.Append(view.Bits);
            builder.Append(" = ");
        }

        #endregion
        #region ByteBlob

        public static string FormatByteBlob(ByteBlobView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatByteBlob(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatByteBlob(ByteBlobView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            builder.Append('[');
            builder.Append(view.Kind.ToString());
            builder.Append(']');

            if (view.Name.Length > 0)
            {
                builder.Append(' ');
                builder.Append(view.Name);
            }
            else
            {
                builder.Append(" Bytes (");
                builder.Append(view.Bytes.Length);
                builder.Append(')');
            }
        }

        #endregion
        #region Field<T>

        public static string FormatField<T>(FieldView<T> view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatField(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatField<T>(FieldView<T> view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            builder.Append(view.Name);
            builder.Append(" = ");

            var value = view.Value;

            ValueToString(value, ref builder, true);

            if (view is ISplitView)
                builder.Append(" (Split)");
        }

        #endregion
        #region Field

        public static string FormatField(IFieldView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatField(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatField(IFieldView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            builder.Append(view.Name);
            builder.Append(" = ");

            var value = view.Value;

            ValueToString(value, ref builder, true);

            if (view is ISplitView)
                builder.Append(" (Split)");
        }

        #endregion
        #region Header

        public static string FormatHeader(HeaderView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatHeader(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatHeader(HeaderView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);
            builder.Append("Header");
        }

        #endregion
        #region LogicalRegion

        public static string FormatLogicalRegion(LogicalRegionView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatLogicalRegion(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatLogicalRegion(LogicalRegionView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            if (view.Kind == ViewKind.DataDirectory)
                builder.Append("[Directory] ");

            builder.Append(view.Name);
            builder.Append(" (");
            builder.Append(view.Children.Count);
            builder.Append(')');
        }

        #endregion
        #region Overlay

        public static string FormatOverlay(OverlayView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatOverlay(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatOverlay(OverlayView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            builder.Append("Overlay (");
            builder.AppendSize(view.Size);
            builder.Append(')');
        }

        #endregion
        #region File

        public static string FormatFile(FileView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatFile(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatFile(FileView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            if (view.Kind == ViewKind.PEFile)
            {
                if (view.Offset == 0)
                {
                    if (view.Name != null)
                    {
                        builder.Append('[');
                        builder.Append(view.ViewMode.ToString());
                        builder.Append("] ");
                        builder.Append(view.Name);
                        builder.Append(" (");
                        builder.Append(view.Children.Count);
                        builder.Append(')');
                    }
                    else
                    {
                        builder.Append('[');
                        builder.Append(view.ViewMode.ToString());
                        builder.Append("] Count = ");
                        builder.Append(view.Children.Count);
                    }
                }
                else
                {
                    WriteRange(view, flags, ref builder);
                    builder.Append(view.Name);
                }
            }
            else
            {
                builder.Append("Count = ");
                builder.Append(view.Children.Count);
            }
        }

        #endregion
        #region Section

        public static string FormatSection(SectionView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatSection(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatSection(SectionView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);
            builder.Append(view.Name);

            builder.Append(" (");
            builder.AppendSize(view.Size);
            builder.Append(')');
        }

        #endregion
        #region Struct

        public static string FormatStruct(StructView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatStruct(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatStruct(StructView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            builder.Append(view.Name);

            TryWriteStructName(view, ref builder);

            if (view is ISplitView)
                builder.Append(" (Split)");
        }

        private static void TryWriteStructName(IStructView view, ref ValueStringBuilder builder)
        {
            if (view.Name == "IMAGE_THUNK_DATA")
            {
                WriteImageThunkDataName((IFieldView) view.Children[0], ref builder);
                return;
            }

            var fields = view.Children.OfType<IFieldView>().ToDictionary(v => v.Name, v => v);

            if (view.Name == "OMFDirEntry" || view.Name == "dnt")
            {
                var subSection = fields["SubSection"];
                builder.Append(' ');
                builder.Append(subSection.Value.ToString());
                return;
            }

            if (view.Name == "UNWIND_CODE")
            {
                var unwindOp = fields["UnwindOp"];
                builder.Append(" (");
                builder.Append(unwindOp.Value.ToString());
                builder.Append(")");
                return;
            }

            if (view.TryGetEnhancedName(out var enhancedName))
            {
                builder.Append(" ");
                builder.Append(enhancedName);
                return;
            }

            if (fields.TryGetValue("Name", out var nameField) || fields.TryGetValue("name", out nameField))
            {
                builder.Append(' ');
                builder.Append(nameField.Value.ToString());
                return;
            }

            if (fields.TryGetValue("Value", out var valueField))
            {
                var value = valueField.Value;

                if (value is string s)
                    value = $"\"{s}\"";

                builder.Append(' ');
                builder.Append(value.ToString());
                return;
            }

            if ((fields.TryGetValue("Type", out var typeField) || fields.TryGetValue("type", out typeField)) && typeField.Value.GetType().IsEnum)
            {
                builder.Append(" (");
                builder.Append(typeField.Value.ToString());
                builder.Append(')');
                return;
            }

            //Many enum values share the same type, so its useful to show the type
            if (fields.TryGetValue("rectyp", out var rectypField))
            {
                builder.Append(' ');
                builder.Append(rectypField.Value.ToString());
                return;
            }

            //Each leaf pretty much has its own type so its not useful to show the leaf type
        }

        private static void WriteImageThunkDataName(IFieldView field, ref ValueStringBuilder builder)
        {
            var needName = true;

            if (field.Value is int i)
            {
                if (i == 0)
                {
                    builder.Append(" 0");
                    needName = false;
                }
                else
                {
                    builder.Append(" 0x");
                    builder.AppendHex((uint) i);
                }
            }
            else if (field.Value is uint ui)
            {
                if (ui == 0)
                {
                    builder.Append(" 0");
                    needName = false;
                }
                else
                {
                    builder.Append(" 0x");
                    builder.AppendHex(ui);
                }
            }
            else
            {
                var ul = (ulong) field.Value;

                if (ul == 0)
                {
                    builder.Append(" 0");
                    needName = false;
                }
                else
                {
                    builder.Append(" 0x");
                    builder.AppendHex(ul);
                }
            }

            if (needName)
            {
                builder.Append(" (");
                builder.Append(field.Name);
                builder.Append(')');
            }
        }

        #endregion
        #region StructField

        public static string FormatStructField(StructFieldView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatStructField(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatStructField(StructFieldView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            builder.Append(view.FieldName);
            builder.Append(" (");
            builder.Append(view.StructName);
            builder.Append(')');
        }

        #endregion
        #region StructArrayField

        public static string FormatStructArrayField(StructArrayFieldView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatStructArrayField(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatStructArrayField(StructArrayFieldView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            builder.Append(view.FieldName);
            builder.Append(" (");
            builder.Append(view.StructName);
            builder.Append("[])");
        }

        #endregion
        #region Value<T>

        public static string FormatValue<T>(ValueView<T> view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatValue(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatValue<T>(ValueView<T> view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            var value = view.Value;

            FormatValueName(view.Name, value, ref builder);
            FormatValueKind(view.Kind, ref builder);

            var valueStart = builder.Length;

            ValueToString(value, ref builder, smallHexNumbers: true);

            if (view is ISplitView)
                builder.Append(" (Split)");

            builder.Replace("\0".AsSpan(), "\\0".AsSpan(), valueStart, builder.Length - valueStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FormatValueName<T>(FixedUtf8String name, T value, ref ValueStringBuilder builder)
        {
            if (name.Length > 0)
            {
                if (typeof(T) == typeof(string))
                {
                    var str = Unsafe.As<T, string>(ref value);

                    if (str != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(AnsiString))
                {
                    var str = (FixedUtf8String) Unsafe.As<T, AnsiString>(ref value);

                    if (str != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(FixedAnsiString))
                {
                    var str = (FixedUtf8String) Unsafe.As<T, FixedAnsiString>(ref value);

                    if (str != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(Utf8String))
                {
                    var str = (FixedUtf8String) Unsafe.As<T, Utf8String>(ref value);

                    if (str != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(FixedUtf8String))
                {
                    var str = Unsafe.As<T, FixedUtf8String>(ref value);

                    if (str != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(Utf16String))
                {
                    var str = Unsafe.As<T, Utf16String>(ref value);

                    if (str.ToString() != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(FixedUtf16String))
                {
                    var str = Unsafe.As<T, FixedUtf16String>(ref value);

                    if (str.ToString() != name  )
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(NullTerminatedString))
                {
                    var str = Unsafe.As<T, NullTerminatedString>(ref value);

                    if (str.ToString() != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
                else if (typeof(T) == typeof(SymString))
                {
                    var str = Unsafe.As<T, SymString>(ref value);

                    if (str != name)
                    {
                        builder.Append(name);
                        builder.Append(" = ");
                    }
                }
            }
        }

        private static void FormatValueKind(ViewKind kind, ref ValueStringBuilder builder)
        {
            switch (kind)
            {
                case ViewKind.ExDllCharacteristics:
                    builder.Append(' ');
                    builder.Append(kind.ToString());
                    break;

                case ViewKind.String:
                case ViewKind.Metadata_String:
                case ViewKind.Metadata_UserString:
                case ViewKind.Metadata_Guid:
                    break;

                default:
                    builder.Append(" [");
                    builder.Append(kind.ToString());
                    builder.Append("] ");
                    break;
            }
        }

        private static void ValueToString<T>(T value, ref ValueStringBuilder builder, bool smallHexNumbers)
        {
            if (typeof(T).IsArray)
            {
                builder.Append('[');

                var array = (Array) (object) value;

                for (var i = 0; i < array.Length; i++)
                {
                    builder.Append(array.GetValue(i).ToString());

                    if (i < array.Length - 1)
                        builder.Append(",");
                }

                builder.Append(']');
            }
            else if (typeof(T) == typeof(string))
            {
                var str = Unsafe.As<T, string>(ref value);

                builder.Append("\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(AnsiString))
            {
                var str = (FixedUtf8String) Unsafe.As<T, AnsiString>(ref value);

                builder.Append("\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(FixedAnsiString))
            {
                var str = (FixedUtf8String) Unsafe.As<T, FixedAnsiString>(ref value);

                builder.Append("\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(Utf8String))
            {
                var str = (FixedUtf8String) Unsafe.As<T, Utf8String>(ref value);

                builder.Append("\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(FixedUtf8String))
            {
                var str = Unsafe.As<T, FixedUtf8String>(ref value);

                builder.Append("\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(Utf16String))
            {
                var str = Unsafe.As<T, Utf16String>(ref value);

                builder.Append("L\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(FixedUtf16String))
            {
                var str = Unsafe.As<T, FixedUtf16String>(ref value);

                builder.Append("L\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(NullTerminatedString))
            {
                var str = Unsafe.As<T, NullTerminatedString>(ref value);

                if (str.Kind == StringKind.UTF16)
                    builder.Append("L\"");
                else
                    builder.Append("\"");

                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(SymString))
            {
                var str = Unsafe.As<T, SymString>(ref value);

                builder.Append("\"");
                builder.Append(str);
                builder.Append("\"");
            }
            else if (typeof(T) == typeof(byte))
            {
                var val = Unsafe.As<T, byte>(ref value);

                if (smallHexNumbers)
                {
                    builder.Append("0x");
                    builder.AppendHex(val);
                }
                else
                    builder.Append(val);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                var val = Unsafe.As<T, sbyte>(ref value);

                if (smallHexNumbers)
                {
                    builder.Append("0x");
                    builder.AppendHex((byte) val);
                }
                else
                    builder.Append(val);
            }
            else if (typeof(T) == typeof(short))
            {
                var val = Unsafe.As<T, short>(ref value);

                if (smallHexNumbers)
                {
                    builder.Append("0x");
                    builder.AppendHex((ushort) val);
                }
                else
                    builder.Append(val);
            }
            else if (typeof(T) == typeof(ushort))
            {
                var val = Unsafe.As<T, ushort>(ref value);

                if (smallHexNumbers)
                {
                    builder.Append("0x");
                    builder.AppendHex(val);
                }
                else
                    builder.Append(val);
            }
            else if (typeof(T) == typeof(int))
            {
                var val = Unsafe.As<T, int>(ref value);

                if (smallHexNumbers)
                {
                    builder.Append("0x");
                    builder.AppendHex((uint) val);
                }
                else
                    builder.Append(val);
            }
            else if (typeof(T) == typeof(uint))
            {
                var val = Unsafe.As<T, uint>(ref value);

                if (smallHexNumbers)
                {
                    builder.Append("0x");
                    builder.AppendHex(val);
                }
                else
                    builder.Append(val);
            }
            else if (typeof(T) == typeof(long))
            {
                var val = Unsafe.As<T, long>(ref value);

                builder.Append("0x");
                builder.AppendHex((ulong) val);
            }
            else if (typeof(T) == typeof(ulong))
            {
                var val = Unsafe.As<T, ulong>(ref value);

                builder.Append("0x");
                builder.AppendHex(val);
            }
            else
            {
                builder.Append(value.ToString());
            }
        }

        #endregion
        #region Value

        public static string FormatValue(IValueView view, ViewFormatFlags flags = ViewFormatFlags.Range)
        {
            var builder = new ValueStringBuilder();

            try
            {
                FormatValue(view, flags, ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static void FormatValue(IValueView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            WriteRange(view, flags, ref builder);

            var value = view.Value;

            FormatValueName(view.Name, value, ref builder);
            FormatValueKind(view.Kind, ref builder);

            var valueStart = builder.Length;

            ValueToString(value, ref builder, smallHexNumbers: false);

            if (view is ISplitView)
                builder.Append(" (Split)");

            builder.Replace("\0".AsSpan(), "\\0".AsSpan(), valueStart, builder.Length - valueStart);
        }

        private static void FormatValueName(FixedUtf8String name, object value, ref ValueStringBuilder builder)
        {
            if (name.Length > 0)
            {
                var type = value.GetType();

                var shouldAddName = type.Name switch
                {
                    nameof(String)               => ((string) value) != name,
                    nameof(AnsiString)           => (FixedUtf8String) (AnsiString) value != name,
                    nameof(FixedAnsiString)      => (FixedUtf8String) (FixedAnsiString) value != name,
                    nameof(Utf8String)           => (FixedUtf8String) (Utf8String) value != name,
                    nameof(FixedUtf8String)      => (FixedUtf8String) value != name,
                    nameof(SymString)            => (SymString) value != name,
                    nameof(Utf16String)          => value.ToString() != name,
                    nameof(FixedUtf16String)     => value.ToString() != name,
                    nameof(NullTerminatedString) => value.ToString() != name,
                    _ => true
                };

                if (shouldAddName)
                {
                    builder.Append(name);
                    builder.Append(" = ");
                }
            }
        }

        private static void ValueToString(object value, ref ValueStringBuilder builder, bool smallHexNumbers)
        {
            switch (value)
            {
                case string v1:
                    builder.Append("\"");
                    builder.Append(v1);
                    builder.Append("\"");
                    break;

                case AnsiString v2:
                    builder.Append("\"");
                    builder.Append((FixedUtf8String) v2);
                    builder.Append("\"");
                    break;

                case FixedAnsiString v3:
                    builder.Append("\"");
                    builder.Append(v3);
                    builder.Append("\"");
                    break;

                case Utf8String v4:
                    builder.Append("\"");
                    builder.Append((FixedUtf8String) v4);
                    builder.Append("\"");
                    break;

                case FixedUtf8String v5:
                    builder.Append("\"");
                    builder.Append(v5);
                    builder.Append("\"");
                    break;

                case Utf16String v6:
                    builder.Append("L\"");
                    builder.Append(v6);
                    builder.Append("\"");
                    break;

                case FixedUtf16String v7:
                    builder.Append("L\"");
                    builder.Append(v7);
                    builder.Append("\"");
                    break;

                case NullTerminatedString v8:
                    if (v8.Kind == StringKind.UTF16)
                        builder.Append("L\"");
                    else
                        builder.Append("L\"");

                    builder.Append(v8);
                    builder.Append("\"");
                    break;

                case SymString v9:
                    builder.Append("L\"");
                    builder.Append(v9);
                    builder.Append("\"");
                    break;

                case byte v10:
                    if (smallHexNumbers)
                    {
                        builder.Append("0x");
                        builder.AppendHex(v10);
                    }
                    else
                        builder.Append(v10);

                    break;

                case sbyte v11:
                    if (smallHexNumbers)
                    {
                        builder.Append("0x");
                        builder.AppendHex((byte) v11);
                    }
                    else
                        builder.Append(v11);

                    break;

                case short v12:
                    if (smallHexNumbers)
                    {
                        builder.Append("0x");
                        builder.AppendHex((ushort) v12);
                    }
                    else
                        builder.Append(v12);

                    break;

                case ushort v13:
                    if (smallHexNumbers)
                    {
                        builder.Append("0x");
                        builder.AppendHex(v13);
                    }
                    else
                        builder.Append(v13);

                    break;

                case int v14:
                    if (smallHexNumbers)
                    {
                        builder.Append("0x");
                        builder.AppendHex((uint) v14);
                    }
                    else
                        builder.Append(v14);

                    break;

                case uint v15:
                    if (smallHexNumbers)
                    {
                        builder.Append("0x");
                        builder.AppendHex(v15);
                    }
                    else
                        builder.Append(v15);

                    break;

                case long v16:
                    builder.Append("0x");
                    builder.AppendHex((ulong) v16);
                    break;

                case ulong v17:
                    builder.Append("0x");
                    builder.AppendHex(v17);
                    break;

                default:
                    builder.Append(value.ToString());
                    break;
            }
        }

        #endregion

        public static void WriteRange(IView view, ViewFormatFlags flags, ref ValueStringBuilder builder)
        {
            if ((flags & ViewFormatFlags.Range) == 0)
                return;

            builder.Append("0x");
            builder.AppendHex((uint) view.Offset);
            builder.Append('-');
            builder.Append("0x");
            builder.AppendHex((uint) (view.Offset + view.Size - 1));
            builder.Append(" | ");
        }
    }
}
