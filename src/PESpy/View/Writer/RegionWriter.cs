using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.PDB;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    public partial class ViewWriter
    {
        internal ref struct RegionWriter
        {
            private string regionName;
            private ViewKind regionKind;
            private RawOffset startOffset;
            private RawOffset currentOffset;
            private ViewWriter viewWriter;
            private List<IView> views;
            private bool global;
            private ViewKind scope;
            private bool shouldAdd;

            internal RegionWriter(RawOffset offset, string name, ViewKind kind, ViewWriter viewWriter, bool global, ViewKind scope, bool shouldAdd)
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

            public void WriteValue(int value) =>
                WriteValueInternal(value, sizeof(int));

            public void WriteValue(uint value) =>
                WriteValueInternal(value, sizeof(int));

            public void WriteValue(RawOffset offset, Guid value, ViewKind kind = ViewKind.Value)
            {
                Debug.Assert(currentOffset == offset);

                WriteValueInternal(value, 16, kind);
            }

            public void WriteValue<T>(T value) where T : IViewable
            {
                var startIndex = views.Count;

                Push();

                value.WriteView(viewWriter);

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;

                Pop();
            }

            public void WriteValues<T>(T[]? value) where T : IViewable
            {
                if (value == null)
                    return;

                var startIndex = views.Count;

                Push();

                for (var i = 0; i < value.Length; i++)
                    value[i].WriteView(viewWriter);

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;

                Pop();
            }

            public void WriteAnsiNullTerminatedValue(RVA<string> value)
            {
                WriteValueInternal((int) value.ListedOffset, sizeof(int));

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, ViewKind.String);
            }

#if PEFAST
            public void WriteAnsiNullTerminatedValue(RVA<PCSTR> value)
            {
                WriteValueInternal((int) value.ListedOffset, sizeof(int));

                if (value.IsValid)
                    viewWriter.WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, ViewKind.String);
            }
#endif

            public void WriteUTF8NullTerminatedValue(RawOffset offset, string value, ViewKind kind)
            {
                Debug.Assert(currentOffset == offset);

                WriteValueInternal(value, value.Length + 1, kind);
            }

            public void WriteValues(ushort[] value)
            {
                for (var i = 0; i < value.Length; i++)
                    WriteValueInternal(value[i], sizeof(short));
            }

            public void WriteValues(PN[] value)
            {
                for (var i = 0; i < value.Length; i++)
                    WriteValueInternal(value[i], sizeof(int));
            }

            public void WriteUnique<T>(T[]? value) where T : IViewable, IValue
            {
                if (value == null)
                    return;

                var startIndex = views.Count;

                Push();

                viewWriter.WriteUniqueGlobal(value);

                for (var i = startIndex; i < views.Count; i++)
                    currentOffset += views[i].Size;

                Pop();
            }

            private void WriteValueInternal<T>(T value, int size, ViewKind kind = ViewKind.Value)
            {
                views.Add(new ValueView<T>(currentOffset, value, size, kind));
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
                if (views.Count == 0)
                    return;

                if (shouldAdd)
                {
                    var regionView = new LogicalRegionView(startOffset, regionName, views.ToArray(), regionKind, (int) (currentOffset - startOffset));

                    viewWriter.AddView(regionView);    
                }
                
                viewWriter.ReturnList(views);

                if (global)
                    viewWriter.Pop(); //Remove the global scope
            }
        }
    }
}
