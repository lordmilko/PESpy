using System;
using System.Diagnostics;
using System.Text;

namespace PESpy.PDB
{
    class TypTypeProxy
    {
        private LfEasy easy;

        public TypTypeProxy(TypType typType)
        {
            this.easy = (LfEasy) typType;
        }

        public TypTypeProxy(LfEasy easy)
        {
            this.easy = easy;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value
        {
            get
            {
                var value = ObjectTypTypeDispatcher.Instance.Dispatch(easy);

                //Protect against a recursive lookup loop in the debugger
                if (value is LfEasy t)
                {
                    return new
                    {
                        t.leaf
                    };
                }

                return value;
            }
        }

        public static string DebuggerDisplay(TypType typType) => DebuggerDisplay((LfEasy) typType);

        public static string DebuggerDisplay(LfEasy easy)
        {
            var builder = new StringBuilder();
            builder.Append("[").Append(easy.leaf).Append("]");

            var value = ObjectTypTypeDispatcher.Instance.Dispatch(easy);

            var defaultStr = easy.leaf.ToString();

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
