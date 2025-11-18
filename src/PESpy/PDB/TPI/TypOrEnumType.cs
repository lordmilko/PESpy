using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using static ClrDebug.PDB.PdbExtensions;

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

                //We might be forced to box here, but the caller needs to know what type to cast to, so we need to make it a single type
                if (val is TypType)
                    return val; //Don't create a new box

                return (TYPE_ENUM_e) val;
            }
        }
    }

    //Encapsulates a type ID that either represents a value from TYPE_ENUM_e
    //(if the type ID is <1000) or a TypType that describes the type
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(TypOrEnumTypeDebugView))]
    public readonly unsafe struct TypOrEnumType
    {
        private string DebuggerDisplay()
        {
            if (IsCrossScopeReference)
                return $"[CrossScope] 0x{typeId.ToString("X")}";

            return $"[{typeId.ToString("X")}] {ToString()}";
        }

        private const ulong IPI_BIT = ((ulong) 1 << 63);

        //I thought I could be smart and store whether or not the type is IPI in the high bit of
        //the typeId, but it turns out that doesn't work, because I've seen an InlineSiteSym2
        //with an inlinee of 0x80000004. So plan B: store IPI in the high bit of the address instead!
        private readonly ulong parent;
        private readonly int typeId; //We store whether the type is IPI in the top bit

        public TypType? TypTyp
        {
            get
            {
                var id = typeId;

                if (PdbExtensions.CV_IS_PRIMITIVE(id))
                    return default;

                var accessor = SymbolMemoryTracker.GetAccessor((long) (parent & ~IPI_BIT));

                if ((parent & IPI_BIT) != 0)
                    return accessor!.GetTypTypeFromIndex((CV_ItemId) id);
                else
                    return accessor!.GetTypTypeFromIndex((CV_typ_t) id);
            }
        }

        /* If the top most bit of a CV_ItemId is set, that indicates it's a cross scope reference. The CrossScopeId struct can
         * be used to work with the value, splitting out the high bit. When DIA tries to lookup the type information for a type index,
         * in msdia140!resolveCrossScopeIndex it bails out if the type index is not negative (i.e. it's checking whether the high bit is set).
         *
         * When you have a CV_ItemId with a value like 0x80000004, this represents a "virtual" ID that references the target of the 4th resolved
         * cross scope import in the current module's scope. From poking around in the debugger, it seems that virtual cross scope references start at 0,
         * and that they also seem to count against cross scope imports that don't have the high bit set. Thus, if we have cross scope imports
         *
         *     0x80001234 - high bit set
         *     0x00001111 - high bit not set
         *     0x80004567 - high bit set
         *     0x80008989 - high bit set
         *     0x80004242 - high bit set
         *
         * it seems that after finding the cross scope export whose local ID is 1234, the result of resolving 0x80001234 will get a virtual type index of 0x8000000
         * and the result of resolving 0x80004242 will get a virtual type index of 80000004
         */

        public bool IsCrossScopeReference => (((uint) typeId) & 0x80000000) != 0;

        public CV_ItemId LocalId => (uint) typeId & ~0x80000000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CV_typ_t(TypOrEnumType type) => type.typeId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CV_typ16_t(TypOrEnumType type) => type.typeId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CV_ItemId(TypOrEnumType type) => type.typeId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(TypOrEnumType type) => type.typeId;

        public TYPE_ENUM_e? PrimitiveType
        {
            get
            {
                //I don't think we're meant to use CV_TYPE here, as that extracts out the CV_type_e.
                //I believe that the primitive TYPE_ENUM_e values are basically a bitmask that encodes
                //all of the various bits of information
                if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
                    return ((TYPE_ENUM_e) typeId);

                return default;
            }
        }

        public CV_prmode_e? PointerMode
        {
            get
            {
                //Must be a primitive type first
                if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
                    return PdbExtensions.CV_MODE((TYPE_ENUM_e) typeId);

                return null;
            }
        }

        public CV_type_e? TypeIndicator
        {
            get
            {
                if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
                    return PdbExtensions.CV_TYPE((TYPE_ENUM_e) typeId);

                return null;
            }
        }

        //The subtype depends on the value in TypeIndicator:
        //CV_special_e, CV_special2_e, CV_integral_e, CV_real_e, CV_int_e
        public int? SubType
        {
            get
            {
                if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
                    return PdbExtensions.CV_SUBT((TYPE_ENUM_e) typeId);

                return null;
            }
        }

        public object Value
        {
            get
            {
                //How do you map a TI to a TYPTYPE? I _think_ what happens is that precForTi calls TPI1::fLoadRecBlk
                //which utilizes information in bufTiOff. And bufTiOff seems to come from the snHash. I think when you have
                //this hash, you can do fast lookup from TI to TYPTYPE. fInitTiToPrecMap seems to be called when the version
                //is earlier than impv70 which builds up a manual array that maps each type record to a TYPTYPE. Furthermore, the meaning
                //of the top-most bit may be whether the ID represents a "FuncId"
                var id = typeId;

                if (TryGetTypType(out var typType))
                    return typType;

                return ((TYPE_ENUM_e) id);
            }
        }

        public bool TryGetTypType(out TypType typType, ICodeViewAccessor? accessor = null)
        {
            var id = typeId;

            if (PdbExtensions.CV_IS_PRIMITIVE(id))
            {
                typType = default;
                return false;
            }

            accessor ??= SymbolMemoryTracker.GetAccessor((long) (parent & ~IPI_BIT));

            if ((parent & IPI_BIT) != 0)
                typType = accessor!.GetTypTypeFromIndex((CV_ItemId) id);
            else
                typType = accessor!.GetTypTypeFromIndex((CV_typ_t) id);

            return true;
        }

        internal TypOrEnumType(byte* parent, CV_typ16_t typeId) : this(parent, (CV_typ_t) (int) typeId)
        {
        }

        internal TypOrEnumType(byte* parent, CV_typ_t typeId)
        {
            this.parent = (ulong) parent;
            this.typeId = (int) typeId;
        }

        internal TypOrEnumType(byte* parent, CV_ItemId typeId)
        {
            this.parent = (ulong) parent | IPI_BIT;
            this.typeId = (int) (uint) typeId;
        }

        public override string ToString()
        {
            if (IsCrossScopeReference)
                return $"0x{typeId.ToString("X")}";

            if (PdbExtensions.CV_IS_PRIMITIVE(typeId))
            {
                using var builder = new ValueStringBuilder();

                var type = (TYPE_ENUM_e) typeId;

                builder.Append(type.ToString());

                builder.Append(", Mode = ");
                builder.Append(type.CV_MODE().ToString());

                var cvType = type.CV_TYPE();
                builder.Append(", CvType = ");
                builder.Append(cvType.ToString());

                var subType = type.CV_SUBT();

                builder.Append(", SubType = ");

                switch (cvType)
                {
                    case CV_type_e.CV_SPECIAL:
                        builder.Append(((CV_special_e) subType).ToString());
                        break;

                    case CV_type_e.CV_SPECIAL2:
                        builder.Append(((CV_special2_e) subType).ToString());
                        break;

                    case CV_type_e.CV_SIGNED:
                    case CV_type_e.CV_UNSIGNED:
                    case CV_type_e.CV_BOOLEAN:
                        builder.Append(((CV_integral_e) subType).ToString());
                        break;

                    case CV_type_e.CV_REAL:
                    case CV_type_e.CV_COMPLEX:
                        builder.Append(((CV_real_e) subType).ToString());
                        break;

                    case CV_type_e.CV_INT:
                        builder.Append(((CV_int_e) subType).ToString());
                        break;
                }

                return builder.ToString();
            }

            //Code from "Value" copied here to avoid boxing

            if (TryGetTypType(out var typType))
                return StringTypTypeDispatcher.Instance.Dispatch(typType);

            return ((TYPE_ENUM_e) typeId).ToString();
        }
    }
}
