using System.Diagnostics;
using System.Text;

namespace PESpy
{
    internal class OldSymTypeProxy
    {
        private OldSymType oldSymType;

        public OldSymTypeProxy(OldSymType oldSymType)
        {
            this.oldSymType = oldSymType;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value
        {
            get
            {
                if (oldSymType == default)
                    return "<null>";

                var result = ObjectOldSymTypeDispatcher.Instance.Dispatch(oldSymType);

                if (result is OldSymType t)
                {
                    //Protect against a recursive lookup loop in the debugger
                    return new
                    {
                        t.reclen,
                        t.rectyp
                    };
                }

                return result;
            }
        }

        public static string DebuggerDisplay(OldSymType oldSymType)
        {
            if (oldSymType == default)
                return "<null>";

            var builder = new StringBuilder();
            builder.Append("[").Append(oldSymType.rectyp).Append("]");

            var value = ObjectOldSymTypeDispatcher.Instance.Dispatch(oldSymType);

            var defaultStr = oldSymType.rectyp.ToString();

            var str = value.ToString();

            if (defaultStr != str)
                builder.Append(" ").Append(str);

            return builder.ToString();
        }
    }
}
