using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.PDB;

namespace PESpy.View
{
    public partial class ViewWriter
    {
        internal ref struct RegionWriter
        {
            private string regionName;
            private ViewKind regionKind;
            private long startOffset;
            private long currentOffset;
            private ViewWriter viewWriter;
            private List<IView> views;
            private bool global;
            private ViewKind scope;
            private bool shouldAdd;

            internal RegionWriter(long offset, string name, ViewKind kind, ViewWriter viewWriter, bool global, ViewKind scope, bool shouldAdd)
            {
                regionName = name;
                regionKind = kind;
                startOffset = offset;
                currentOffset = offset;
                this.viewWriter = viewWriter;
                views = viewWriter.RentList();
                this.global = global;
                this.scope = scope;
                this.shouldAdd = shouldAdd;
            }

            public void WriteValue(int value, ViewKind kind) =>
                WriteValueInternal(value, sizeof(int), kind);

            public void WriteValue(uint value, ViewKind kind) =>
                WriteValueInternal(value, sizeof(int), kind);

            public void WriteValue(long offset, Guid value, ViewKind kind)
            {
                Debug.Assert(currentOffset == offset);

                WriteValueInternal(value, 16, kind);
            }

            public void WriteValue<T>(T value) where T : IViewable
            {
                var startIndex = views.Count;

                Push();

                var oldFromRegion = viewWriter.FromRegion;

                viewWriter.FromRegion = false;

                try
                {
                    if (global)
                    {
                        //Include everything in the region
                        viewWriter.FromRegion = true;
                        value.WriteGlobals(viewWriter);
                    }
                    else
                    {
                        //Don't include globals in the region

                        value.WriteGlobals(viewWriter);

                        viewWriter.FromRegion = true;
                    }

                    var result = value.WriteStruct(viewWriter);

                    if (result != null)
                        views.Add(result);
                }
                finally
                {
                    viewWriter.FromRegion = oldFromRegion;
                }

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;

                Pop();
            }

            public void WriteValues<T>(T[]? value) where T : IViewable
            {
                if (value == null)
                    return;

                var startIndex = views.Count;

                for (var i = 0; i < value.Length; i++)
                {
                    var item = value[i];

                    WriteValueElement(item);
                }

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;
            }

            public void WriteValues<TLightweightList, TEnumerator, TElement>(TLightweightList? value)
                where TLightweightList : ILightweightList<TEnumerator, TElement>
                where TEnumerator : IEnumerator<TElement>
                where TElement : IViewable, IValue
            {
                if (value == null)
                    return;

                var startIndex = views.Count;

                var enumerator = value.GetEnumerator();

                while (enumerator.MoveNext())
                    WriteValueElement(enumerator.Current);

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;
            }

            private void WriteValueElement<T>(T item) where T : IViewable
            {
                var oldFromRegion = viewWriter.FromRegion;

                viewWriter.FromRegion = false;

                try
                {
                    item.WriteGlobals(viewWriter);

                    viewWriter.FromRegion = true;

                    var result = item.WriteStruct(viewWriter);

                    if (result != null)
                        views.Add(result);
                }
                finally
                {
                    viewWriter.FromRegion = oldFromRegion;
                }
            }

            //The region just encapsulates the _RVA_ of the string. The string itself is located _outside_ of the region
            public void WriteAnsiNullTerminatedValue(RVA<AnsiString> value, ViewKind kind)
            {
                WriteValueInternal((int) value.ListedOffset, sizeof(int), kind);
            }

            public void WriteInlineAnsiNullTerminatedValue(RawValue<AnsiString> value, ViewKind kind)
            {
                WriteValueInternal(value.Value, value.Value.Length + 1, kind);
            }

            public void WriteUTF8NullTerminatedValue(long offset, string value, ViewKind kind)
            {
                Debug.Assert(currentOffset == offset);

                WriteValueInternal(value, value.Length + 1, kind);
            }

            public void WriteUTF8NullTerminatedValue(long offset, Utf8String value, ViewKind kind)
            {
                Debug.Assert(currentOffset == offset);

                WriteValueInternal(value, value.Length + 1, kind);
            }

            public void WriteValues(ushort[] value, ViewKind kind)
            {
                for (var i = 0; i < value.Length; i++)
                    WriteValueInternal(value[i], sizeof(short), kind);
            }

            public void WriteValues(PN[] value)
            {
                for (var i = 0; i < value.Length; i++)
                    WriteValueInternal(value[i], sizeof(int), ViewKind.PN);
            }

            public void WriteUnique<T>(T[]? value) where T : IViewable, IValue
            {
                if (value == null)
                    return;

                var startIndex = views.Count;

                Push();

                viewWriter.WriteRegionUniqueGlobal(value);

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;

                Pop();
            }

            internal void WriteUnique<TLightweightList, TEnumerator, TElement>(TLightweightList? value)
                where TLightweightList : ILightweightList<TEnumerator, TElement>
                where TEnumerator : IEnumerator<TElement>
                where TElement : IViewable, IValue
            {
                if (value == null)
                    return;

                var startIndex = views.Count;

                Push();

                viewWriter.WriteRegionUniqueGlobal<TLightweightList, TEnumerator, TElement>(value);

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;

                Pop();
            }

            private void WriteValueInternal<T>(T value, int size, ViewKind kind)
            {
                var result = viewWriter.NewValue(currentOffset, value, size, kind, fromRegion: true);

                if (result != null)
                    views.Add(result);

                currentOffset += size;
            }

            private void Push()
            {
                if (scope != 0)
                    viewWriter.PushScope(views, scope);
                else
                    viewWriter.Push(views);
            }

            private void Pop()
            {
                if (scope != 0)
                    viewWriter.PopScope();
                else
                    viewWriter.Pop();
            }

            public void Dispose()
            {
                if (views.Count > 0)
                {
                    if (shouldAdd)
                    {
                        var regionView = new LogicalRegionView(startOffset, regionName, views.ToArray(), viewWriter, regionKind, (int) (currentOffset - startOffset));

                        viewWriter.AddView(regionView);
                    }
                }

                viewWriter.ReturnList(views);

                if (global)
                    viewWriter.Pop(); //Remove the global scope

                viewWriter.ExitRegion();
            }
        }
    }
}
