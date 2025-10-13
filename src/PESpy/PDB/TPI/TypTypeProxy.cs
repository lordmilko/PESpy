using System;
using System.Diagnostics;
using System.Text;

namespace PESpy.PDB
{
    class TypTypeProxy
    {
        private TypType typType;

        public TypTypeProxy(TypType typType)
        {
            this.typType = typType;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value => ObjectTypTypeDispatcher.Instance.Dispatch(typType);

        public static string DebuggerDisplay(TypType typType)
        {
            var builder = new StringBuilder();
            builder.Append("[").Append(typType.leaf).Append("]");

            var value = ObjectTypTypeDispatcher.Instance.Dispatch(typType);

            var defaultStr = typType.leaf.ToString();

            var str = value.ToString();

            if (defaultStr != str)
            {
                builder.Append(" ");
                builder.Append(str);
            }

            return builder.ToString();
        }
    }
}
