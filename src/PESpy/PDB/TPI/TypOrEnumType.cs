using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    //For more info on types, see the comments at the top of TypType.cs

    public class TypOrEnumTypeDebugView
    {
        private TypOrEnumType type;

        public TypOrEnumTypeDebugView(TypOrEnumType type)
        {
            this.type = type;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value
        {
            get
            {
                var val = type.Value;

                if (val is TypType t)
                    return TypTypeProxy.GetValue(t);

                return (TYPE_ENUM_e) val;
            }
        }
    }

    [DebuggerDisplay("[{typeId.ToString(\"X\"),nq}] {ToString(),nq}")]
    [DebuggerTypeProxy(typeof(TypOrEnumTypeDebugView))]
    public readonly unsafe struct TypOrEnumType
    {
        private readonly long parent;
        private readonly int typeId;

        public TypType? TypTyp
        {
            get
            {
                if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
                    return default;

                var pdb = SymbolMemoryTracker.GetPDB(parent);

                return pdb!.GetTypTypeFromIndex(typeId);
            }
        }

        public static implicit operator CV_typ_t(TypOrEnumType type) => type.typeId;

        public TYPE_ENUM_e? PrimitiveType
        {
            get
            {
                if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
                    return ((TYPE_ENUM_e) typeId);

                return default;
            }
        }

        public object Value
        {
            get
            {
                //How do you map a TI to a TYPTYPE? I _think_ what happens is that precForTi calls TPI1::fLoadRecBlk
                //which utilizes information in bufTiOff. And bufTiOff seems to come from the snHash. I think when you have
                //this hash, you can do fast lookup from TI to TYPTYPE. fInitTiToPrecMap seems to be called when the version
                //is earlier than impv70 which builds up a manual array that maps each type record to a TYPTYPE.
                if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
                    return ((TYPE_ENUM_e) typeId);

                var pdb = SymbolMemoryTracker.GetPDB(parent);

                return pdb!.GetTypTypeFromIndex(typeId);
            }
        }

        internal TypOrEnumType(byte* parent, int typeId)
        {
            this.parent = (long) parent;
            this.typeId = typeId;
        }

        public override string ToString() => Value.ToString();
    }
}
