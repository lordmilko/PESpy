using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    //This type is a combination of System.Reflection.Metadata's SignatureDecoder and the original decoders I've written
    //over the years prior to SRM, with specific references to the ECMA-335 spec, and using unmanaged API enum types
    internal readonly struct SignatureDecoder<TType, TGenericContext>
    {
        private readonly ISignatureTypeProvider<TType, TGenericContext> _provider;
        private readonly TGenericContext _genericContext;
        private readonly CompressedModelHeap _heap;

        internal SignatureDecoder(
            ISignatureTypeProvider<TType, TGenericContext> provider,
            TGenericContext genericContext,
            CompressedModelHeap heap)
        {
            _provider = provider;
            _genericContext = genericContext;
            _heap = heap;
        }

        public TType DecodeType(ref ByteReader reader)
        {
            var corElementType = reader.ReadCorElementType();

            return DecodeType(ref reader, corElementType);
        }

        public TType DecodeType(ref ByteReader reader, CorElementType corElementType)
        {
            TType elementType;
            int index;

            //Sometimes the type is prefixed by custom modifiers, sometimes not. So ostensibly we'll be reading the type
            //as per II.23.2.12 however if we see there's custom modifiers we'll need to specially handle those

            switch (corElementType)
            {
                //There are two kinds of types: Type as defined by II.23.2.12 and "other types"
                //that may be used in certain circumstances. e.g. Void cannot be used as the type of a parameter,
                //but can be used as part of a return type. Pinned is a type of constraint that can only be used
                //in conjunction with local variables

                case CorElementType.Void:
                case CorElementType.TypedByRef:
                    return _provider.GetType(corElementType);

                case CorElementType.ByRef:
                    elementType = DecodeType(ref reader);
                    return _provider.GetByRefType(elementType);

                case CorElementType.Pinned:
                    elementType = DecodeType(ref reader);
                    return _provider.GetPinnedType(elementType);

                case CorElementType.CModOpt:
                    return DecodeModifiedType(ref reader, isRequired: false);

                case CorElementType.CModReqd:
                    return DecodeModifiedType(ref reader, isRequired: true);

                //All other types below are "normal" types that can appear anywhere

                #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

                case CorElementType.Boolean:
                case CorElementType.Char:
                case CorElementType.I1:
                case CorElementType.U1:
                case CorElementType.I2:
                case CorElementType.U2:
                case CorElementType.I4:
                case CorElementType.U4:
                case CorElementType.I8:
                case CorElementType.U8:
                case CorElementType.R4:
                case CorElementType.R8:
                case CorElementType.I:
                case CorElementType.U:
                    return _provider.GetType(corElementType);

                #endregion
                #region ARRAY Type ArrayShape (general array, see §II.23.2.13)

                case CorElementType.Array:
                    return DecodeArrayType(ref reader);

                #endregion
                #region CLASS TypeDefOrRefOrSpecEncoded | VALUETYPE TypeDefOrRefOrSpecEncoded

                case CorElementType.Class:
                case CorElementType.ValueType:
                    return DecodeTokenType(ref reader);

                #endregion
                #region FNPTR MethodDefSig | FNPTR MethodRefSig

                case CorElementType.FnPtr:
                    var methodSig = DecodeMethodSignature(ref reader);
                    return _provider.GetFunctionPointerType(methodSig);

                #endregion
                #region GENERICINST (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type*

                case CorElementType.GenericInst:
                    //The next element is "CorElementType.Class" or "CorElementType.ValueType" and then the token
                    //of that associated type. This gives us the generic type definition
                    var genericTypeDefinition = DecodeType(ref reader);
                    var genericArgs = DecodeTypeSequence(ref reader);
                    return _provider.GetGenericInstantiation(genericTypeDefinition, genericArgs);

                #endregion
                #region MVAR number | VAR number

                case CorElementType.MVar:
                    index = reader.ReadCompressedInteger();
                    return _provider.GetGenericMethodParameter(_genericContext, index);

                case CorElementType.Var:
                    index = reader.ReadCompressedInteger();
                    return _provider.GetGenericTypeParameter(_genericContext, index);

                #endregion
                #region OBJECT | STRING

                case CorElementType.Object:
                case CorElementType.String:
                    return _provider.GetType(corElementType);

                #endregion
                #region PTR CustomMod* Type | PTR CustomMod* VOID

                case CorElementType.Ptr:
                    elementType = DecodeType(ref reader);
                    return _provider.GetPointerType(elementType);

                #endregion
                #region SZARRAY CustomMod* Type (single dimensional, zero-based array i.e., vector)

                case CorElementType.SZArray:
                    elementType = DecodeType(ref reader);
                    return _provider.GetSZArrayType(elementType);

                #endregion

                default:
                    throw new BadImageFormatException();
            }
        }

        //II.23.2.1 MethodDefSig
        //II.23.2.2 MethodRefSig
        //II.23.2.3 StandAloneMethodSig
        //II.23.2.5 PropertySig (essentially a special variation of method sig; ultimately describes the getter)
        public MethodSignature<TType> DecodeMethodSignature(ref ByteReader reader)
        {
            /* MethodDef
             * ==========
             * 
             * --> HASTHIS ---> EXPLICITTHIS ------|-> DEFAULT ------------------->|
             * \            \_________________^ ^  |                               |
             *  \______________________________/   |-> VARARG  ------------------->|
             *                                     |                               |
             *                                     |-> GENERIC -> GenParamCount -->|
             *                                                                     |
             *  |<-----------------------------------------------------------------|
             *  |                                    _________
             *  |                                   /         \
             *  v                                  v           \
             *  -> ParamCount ---> RetType  ---------> Param ----------->
             *                                 \                   ^
             *                                  \_________________/
             *                                  
             * StandAloneMethod
             * ================
             * 
             * -----> HASTHIS ------> EXPLICITTHIS ------------> DEFAULT ---------> ParamCount ------
             *   \               \                   ^   ^  |                 ^                      |
             *    \               \_________________/   /   |--> VARARGS ---> |                      |
             *     \___________________________________/    |                 |                      |
             *                                              |--> C ---------> |                      |
             *                                              |                 |                      |
             *                                              |--> STDCALL ---> |                      |
             *                                              |                 |                      |
             *                                              |--> THISCALL --> |                      |
             *                                              |                 |                      |
             *                                              |--> FASTCALL --> |                      |
             *                                                                                       |
             *     __________________________________________________________________________________|
             *    |
             *    |                      _____________                         ____________
             *    |                     /             \                       /            \
             *    |                    v               \                     v              \
             *     ------ RetType -------> Param -------------> SENTINEL --------> PARAM --------->
             *                       \               ^     \                                   ^
             *                        \_____________/       \_________________________________/
             */

            //The first byte of the Signature holds bits for HASTHIS, EXPLICITTHIS and calling convention (DEFAULT,
            //VARARG, or GENERIC). These are OR'ed together.

            var callingConv = (CorCallingConvention) reader.ReadByte();

            var genericParameterCount = 0;

            if ((callingConv & CorCallingConvention.GENERIC) != 0)
                genericParameterCount = reader.ReadCompressedInteger();

            var parameterCount = reader.ReadCompressedInteger();
            var returnType = DecodeType(ref reader);

            int requiredParameterCount;
            TType[] parameterTypes;

            if (parameterCount == 0)
            {
                requiredParameterCount = 0;
                parameterTypes = Array.Empty<TType>();
            }
            else
            {
                using var parameters = new PooledList<TType>(parameterCount);

                var i = 0;

                for (; i < parameterCount; i++)
                {
                    var typeCode = reader.ReadCorElementType();

                    if (typeCode == CorElementType.Sentinel)
                        break;

                    parameters.Add(DecodeType(ref reader, typeCode));
                }

                requiredParameterCount = i;

                for (; i < parameterCount; i++)
                {
                    parameters.Add(DecodeType(ref reader));
                }

                parameterTypes = parameters.ToArray();
            }

            return new MethodSignature<TType>(callingConv, returnType, requiredParameterCount, genericParameterCount, parameterTypes);
        }

        //II.23.2.15 MethodSpec
        public TType[] DecodeMethodSpecificationSignature(ref ByteReader reader)
        {
            var callConv = (CorCallingConvention) reader.ReadByte();

            if (callConv != CorCallingConvention.GENERICINST)
                throw new BadImageFormatException();

            return DecodeTypeSequence(ref reader);
        }

        //II.23.2.6 LocalVarSig
        public TType[] DecodeLocalSignature(ref ByteReader reader)
        {
            /*
             *                               __________________________________________________________________________
             *                              /   _________________________________________                              \
             *                             /   /                                         \                              \
             *                            v   v                                           \                              \
             * --> LOCAL_SIG ---> Count ------------> CustomMod ---------> Constraint ----------> BYREF -------> Type ------>
             *                              |    \                 ^   \                ^    \             ^           ^
             *                              |     \_______________/     \______________/      \___________/            |
             *                              |                                                                          |
             *                              |                                                                          |
             *                               -------------------> TYPEDBYREF ----------------------------------------->|
             * 
             * 
             */

            var callingConv = (CorCallingConvention) reader.ReadByte();

            if (callingConv != CorCallingConvention.LOCAL_SIG)
                throw new BadImageFormatException();

            return DecodeTypeSequence(ref reader);
        }

        //II.23.2.4 FieldSig
        public TType DecodeFieldSignature(ref ByteReader reader)
        {
            /*             ______________
             *            /              \
             *           v                \
             * FIELD ------> CustomMod --------> Type -->
             *         \                     ^
             *          \___________________/
             */

            var callConv = (CorCallingConvention) reader.ReadByte();

            if (callConv != CorCallingConvention.FIELD)
                throw new BadImageFormatException();

            return DecodeType(ref reader);
        }

        private TType[] DecodeTypeSequence(ref ByteReader reader)
        {
            var count = reader.ReadCompressedInteger();

            if (count == 0)
                throw new BadImageFormatException();

            var types = new TType[count];

            for (var i = 0; i < types.Length; i++)
                types[i] = DecodeType(ref reader);

            return types;
        }

        private TType DecodeArrayType(ref ByteReader reader)
        {
            //When creating a multidimensional array via Array.CreateInstance you can specify the number of "lengths" and the number of "lower bounds"
            //of an array. This information then applies to the created instance. The information is not stored in the type. I don't understand why
            //ECMA-335 metadata stores this information if we can't do anything with it, but I guess the bottom line is as far as our goal of creating a MetadataType
            //goes, we can just ignore this info?

            var elementType = DecodeType(ref reader);
            var rank = reader.ReadCompressedInteger();
            var numSizes = reader.ReadCompressedInteger();

            int[] sizes;
            int[] lowerBounds;

            if (numSizes > 0)
            {
                sizes = new int[numSizes];

                for (var i = 0; i < numSizes; i++)
                    sizes[i] = reader.ReadCompressedInteger();
            }
            else
                sizes = Array.Empty<int>();

            var numLowerBounds = reader.ReadCompressedInteger();

            if (numLowerBounds > 0)
            {
                lowerBounds = new int[numLowerBounds];

                for (var i = 0; i < numLowerBounds; i++)
                    lowerBounds[i] = reader.ReadCompressedInteger();
            }
            else
                lowerBounds = Array.Empty<int>();

            var arrayShape = new ArrayShape(rank, sizes, lowerBounds);
            return _provider.GetArrayType(elementType, arrayShape);
        }

        private TType DecodeModifiedType(ref ByteReader reader, bool isRequired)
        {
            var modifier = DecodeTokenType(ref reader);
            var unmodifiedType = DecodeType(ref reader);

            return _provider.GetModifiedType(modifier, unmodifiedType, isRequired);
        }

        private TType DecodeTokenType(ref ByteReader reader)
        {
            var token = reader.ReadToken();

            if (token.IsNil)
                throw new BadImageFormatException();

            switch (token.Type)
            {
                case CorTokenType.mdtTypeDef:
                    return _provider.GetTypeDef(_heap, (mdTypeDef) token);

                case CorTokenType.mdtTypeRef:
                    return _provider.GetTypeRef(_heap, (mdTypeRef) token);

                case CorTokenType.mdtTypeSpec:
                    return _provider.GetTypeSpec(_heap, (mdTypeSpec) token, _genericContext);

                default:
                    throw new BadImageFormatException();
            }
        }
    }
}
