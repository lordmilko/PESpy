using System;
using System.Diagnostics;
using System.Text;
using ClrDebug.PDB;

namespace PESpy.PDB
{

    class SymTypeProxy
    {
        private SymType symType;

        public SymTypeProxy(SymType symType)
        {
            this.symType = symType;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value
        {
            get
            {
                var result = ObjectSymTypeDispatcher.Instance.Dispatch(symType);

                if (result is SymType t)
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

        public static string DebuggerDisplay(SymType symType)
        {
            var builder = new StringBuilder();
            builder.Append("[").Append(symType.rectyp).Append("]");

            static void AppendPublicName(SymString name, StringBuilder builder)
            {
                var demangled = Demangler.ParseString(name);

                builder.Append(" ").Append(demangled);

                if (name == demangled)
                    return;

                builder.Append(" <- ").Append(name.ToString());
            }

            switch (symType.rectyp)
            {
                case SYM_ENUM_e.S_PUB16:
                    AppendPublicName(((DataSym16) symType).name, builder);
                    break;

                case SYM_ENUM_e.S_PUB32_16t:
                    AppendPublicName(((DataSym3216t) symType).name, builder);
                    break;

                case SYM_ENUM_e.S_PUB32_ST:
                case SYM_ENUM_e.S_PUB32:
                    AppendPublicName(((PubSym32) symType).name, builder);
                    break;

                default:
                    var value = ObjectSymTypeDispatcher.Instance.Dispatch(symType);

                    var defaultStr = symType.rectyp.ToString();

                    var str = value.ToString();

                    if (defaultStr != str)
                        builder.Append(" ").Append(str);
                    break;
            }

            return builder.ToString();
        }
    }
}
