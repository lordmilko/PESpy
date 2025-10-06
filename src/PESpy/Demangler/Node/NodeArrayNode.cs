using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        class NodeArrayNodeDebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Node[] Items { get; }

            public NodeArrayNodeDebugView(NodeArrayNode nodeArrayNode)
            {
                Items = nodeArrayNode.rentedNodes.Take(nodeArrayNode.Count).ToArray();
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(NodeArrayNodeDebugView))]
        public class NodeArrayNode : Node
        {
            internal Node[] rentedNodes;
            internal int count;

            public int Count => count;

            public NodeArrayNode() : base(NodeKind.NodeArray)
            {
            }

            public Node this[int index]
            {
                get
                {
                    if (index < 0 || index > Count)
                        throw new IndexOutOfRangeException();

                    return rentedNodes[index];
                }
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                Output(ref builder, flags, ",");
            }

            internal void Output(ref Utf8StringBuilder builder, UNDNAME flags, string separator)
            {
                if (Count == 0)
                    return;

                rentedNodes[0].Output(ref builder, flags);

                for (var i = 1; i < Count; i++)
                {
                    builder.Append(separator);
                    rentedNodes[i].Output(ref builder, flags);
                }
            }

            internal void Output(ref Utf8StringBuilder builder, UNDNAME flags, string separator, int count)
            {
                count = Math.Min(count, Count);

                if (count == 0)
                    return;

                rentedNodes[0].Output(ref builder, flags);

                for (var i = 1; i < count; i++)
                {
                    builder.Append(separator);
                    rentedNodes[i].Output(ref builder, flags);
                }
            }

            public override void Reset()
            {
                //If Count == 0 then the array we got from the PooledList was null, and we substituted it with Array.Empty<T>(),
                //meaning that we don't need to return that array
                if (Count > 0)
                {
                    ArrayPool<Node>.Shared.Return(rentedNodes);
                    rentedNodes = default;

                    count = 0;
                }
            }
        }
    }
}
