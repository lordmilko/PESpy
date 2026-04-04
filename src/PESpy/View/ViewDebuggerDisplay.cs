using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace PESpy.View
{
    class ViewDebuggerDisplay
    {
        //Visual Studio seems to be slower at evaluating debugger display values when there's a complex expression inline, vs simply calling a function.
        //I'm not sure if maybe it's trying to emulate the debugger display expression, or is creating a complex function to then compile. We centralize all
        //of our debugger display expressions here, which also allows easily subbing out expressions for debugging performance issues

        public static string Asm(IAsmView view)
        {
            var builder = new StringBuilder();
            WriteRange(builder, view);
            builder.Append(view.Name);

            return builder.ToString();
        }

        public static string BitField<T>(BitFieldView<T> view)
        {
            var builder = new StringBuilder();
            WriteRange(builder, view);
            builder.Append(view.Name);
            builder.Append(":");
            builder.Append(view.Bits);
            builder.Append(" = ");
            builder.Append(view.Value);

            return builder.ToString();
        }

        public static string ByteBlob(ByteBlobView view)
        {
            var builder = new StringBuilder();
            WriteRange(builder, view);
            builder.Append("[").Append(view.Kind).Append("]");

            builder.Append($" Bytes ({view.Bytes.Length})");

            return builder.ToString();
        }

        public static string Field<T>(FieldView<T> view)
        {
            var builder = new StringBuilder();

            WriteRange(builder, view);

            builder.Append(view.Name);

            builder.Append(" = ");

            if (view.Value is IStructView s)
            {
                builder.Append(s.Name);

                if (s.Name == "IMAGE_DATA_DIRECTORY")
                {
                    var virtualAddress = (int) ((IFieldView) s.Children[0]).Value;
                    var size = (int) ((IFieldView) s.Children[1]).Value;

                    if (virtualAddress == 0 && size == 0)
                        builder.Append(" (Empty)");
                    else
                        builder.Append(" (0x").Append(virtualAddress.ToString("X")).Append("-0x").Append((virtualAddress + size).ToString("X")).Append(")");
                }
            }
            else
            {
                if (view.Value!.GetType().IsArray)
                    builder.Append("[").Append(string.Join(",", ((IEnumerable) view.Value).Cast<object>())).Append("]");
                else
                {
                    if (view.Value is int i)
                        builder.Append("0x").Append(i.ToString("X"));
                    else if (view.Value is uint u)
                        builder.Append("0x").Append(u.ToString("X"));
                    else if (view.Value is long l)
                        builder.Append("0x").Append(l.ToString("X"));
                    else if (view.Value is ulong ul)
                        builder.Append("0x").Append(ul.ToString("X"));
                    else
                        builder.Append(view.Value);
                }
            }

            if (view is SplitFieldView<T>)
                builder.Append(" (Split)");

            return builder.ToString();
        }

        public static string Header(HeaderView view)
        {
            var builder = new StringBuilder();
            WriteRange(builder, view);
            builder.Append("Header");

            return builder.ToString();
        }

        public static string LogicalRegion(LogicalRegionView view)
        {
            var builder = new StringBuilder();

            WriteRange(builder, view);

            if (view.Kind == ViewKind.DataDirectory)
                builder.Append("[Directory] ");

            builder.Append(view.Name);

            if (view.Children.Count > 0)
            {
                if (view.Children[0] is IStructView st)
                {
                    var first = st.Name;

                    if ((view.Children.Count > 1 && view.Children.All(v => v is IStructView s && s.Name == first))) //In PDBs we force all values to be in a page region, but we don't need to show (1) if there's just 1 child in that case, since it's not a repeating group
                        builder.Append(" (").Append(view.Children.Count).Append(")");
                    else if (first == "IMAGE_IMPORT_BY_NAME")
                        builder.Append(" (").Append(view.Children.Count(v => v is IStructView s && s.Name == Strings.IMAGE_IMPORT_BY_NAME || v is IValueView { Value: string })).Append(")");
                }
                else if (view.Kind == ViewKind.Strings || view.Kind == ViewKind.StringPoolHeap)
                {
                    if (view.Children.All(v => v is IValueView || v is ByteBlobView { Kind: ViewKind.Padding } || v is ByteBlobView { Kind: ViewKind.CC }))
                        builder.Append(" (").Append(view.Children.OfType<IValueView>().Count()).Append(")");
                }
                else
                    builder.Append(" (").Append(view.Children.Count()).Append(")");
            }

            return builder.ToString();
        }

        public static string Overlay(OverlayView view)
        {
            var builder = new StringBuilder();
            WriteRange(builder, view);
            builder.Append("Overlay");

            builder.Append(" (").Append(FormatBytes(view.Size)).Append(")");

            return builder.ToString();
        }

        public static string File(FileView view)
        {
            if (view.Kind == ViewKind.PEFile)
            {
                if (view.Offset == 0)
                {
                    if (view.Name != null)
                        return $"[{view.ViewMode}] {view.Name} ({view.Children.Count})";

                    return $"[{view.ViewMode}] Count = {view.Children.Count}";
                }

                var builder = new StringBuilder();
                WriteRange(builder, view);
                builder.Append(view.Name);
                return builder.ToString();
            }

            return $"Count = {view.Children.Count}";
        }

        public static string Section(SectionView view)
        {
            var builder = new StringBuilder();
            WriteRange(builder, view);
            builder.Append(view.Name);

            builder.Append(" (").Append(FormatBytes(view.Size)).Append(")");

            return builder.ToString();
        }

        internal static string FormatBytes(double bytes)
        {
            if (bytes < 1024)
                return bytes.ToString("N2") + " B";

            var kb = bytes / 1024;

            if (kb < 1024)
                return kb.ToString("N2") + " KB";

            var mb = kb / 1024;

            if (mb < 1024)
                return mb.ToString("N2") + " MB";

            var gb = mb / 1024;

            return gb.ToString("N2") + " GB";
        }

        #region Struct

        public static string Struct(IStructView view)
        {
            var builder = new StringBuilder();
            WriteRange(builder, view);

            builder.Append(view.Name);

            var fields = view.Children.OfType<IFieldView>().ToArray();

            TryWriteStructName(view, fields, builder);

            if (view is ISplitView)
                builder.Append(" (Split)");

            return builder.ToString();
        }

        private static void TryWriteStructName(IStructView view, IFieldView[] fields, StringBuilder builder)
        {
            if (view.Name == "IMAGE_THUNK_DATA")
            {
                WriteImageThunkDataName(fields[0], builder);
                return;
            }

            if (view.Name == "OMFDirEntry" || view.Name == "dnt")
            {
                var subSection = fields.First(f => f.Name == "SubSection");
                builder.Append(" ").Append(subSection.Value);
                return;
            }

            if (view.TryGetEnhancedName(out var enhancedName))
            {
                builder.Append(" ").Append(enhancedName);
                return;
            }

            var nameField = fields.FirstOrDefault(f => f.Name == "Name" || f.Name == "name")?.Value;

            if (nameField != null)
            {
                builder.Append(" ").Append(nameField);
                return;
            }

            var valueField = fields.FirstOrDefault(f => f.Name == "Value")?.Value;

            if (valueField != null)
            {
                if (valueField is string s)
                    valueField = $"\"{s}\"";

                builder.Append(" ").Append(valueField);
                return;
            }

            var typeField = fields.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, "Type"))?.Value;

            if (typeField != null && typeField.GetType().IsEnum)
            {
                builder.Append(" (").Append(typeField).Append(")");
                return;
            }

            //Many enum values share the same type, so its useful to show the type
            var rectypField = fields.FirstOrDefault(f => f.Name == "rectyp")?.Value;

            if (rectypField != null)
            {
                builder.Append(" ").Append(rectypField);
                return;
            }

            //Each leaf pretty much has its own type so its not useful to show the leaf type
        }

        private static void WriteImageThunkDataName(IFieldView field, StringBuilder builder)
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
                    builder.Append(" 0x").Append(i.ToString("X"));
            }
            else if (field.Value is uint ui)
            {
                if (ui == 0)
                {
                    builder.Append(" 0");
                    needName = false;
                }
                else
                    builder.Append(" 0x").Append(ui.ToString("X"));
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
                    builder.Append(" 0x").Append(((ulong) field.Value).ToString("X"));
            }

            if (needName)
                builder.Append(" (").Append(field.Name).Append(")");
        }

        #endregion

        public static string StructField(StructFieldView view)
        {
            var builder = new StringBuilder();

            WriteRange(builder, view);

            builder.Append(view.FieldName);
            builder.Append(" (");
            builder.Append(view.StructName);
            builder.Append(")");

            return builder.ToString();
        }

        public static string StructArrayField(StructArrayFieldView view)
        {
            var builder = new StringBuilder();

            WriteRange(builder, view);

            builder.Append(view.FieldName);
            builder.Append(" (");
            builder.Append(view.StructName);
            builder.Append("[])");

            return builder.ToString();
        }

        public static string Value<T>(ValueView<T> view)
        {
            string value;

            var builder = new StringBuilder();
            WriteRange(builder, view);

            if (view.Kind == ViewKind.ExDllCharacteristics)
            {
                value = view.Kind.ToString();
            }
            else
            {
                switch (view.Kind)
                {
                    case ViewKind.String:
                    case ViewKind.Metadata_String:
                    case ViewKind.Metadata_UserString:
                    case ViewKind.Metadata_Guid:
                        break;

                    default:
                        builder.Append("[").Append(view.Kind).Append("] "); //todo: not sure which types/kinds to include when doing this?
                        break;
                }

                value = view.Value!.ToString();
            }

            if (view.Value is string)
                builder.Append("\"").Append(view.Value).Append("\"");
            else if (view.Value is long l)
                builder.Append("0x").Append(l.ToString("X"));
            else if (view.Value is ulong ul)
                builder.Append("0x").Append(ul.ToString("X"));
            else
                builder.Append(value);

            if (view is SplitValueView<T>)
                builder.Append(" (Split)");

            return builder.ToString();
        }

        private static void WriteRange(StringBuilder builder, IView view)
        {
            builder.Append("0x").Append(view.Offset.ToString("X"));
            builder.Append("-");
            builder.Append("0x").Append((view.Offset + view.Size - 1).ToString("X"));
            builder.Append(" | ");
        }
    }
}
