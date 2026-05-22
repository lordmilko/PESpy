using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    //II.23.3 (p293)
    internal readonly struct CustomAttributeDecoder<TType>
    {
        private readonly ModelHeap _modelHeap;
        private readonly ICustomAttributeTypeProvider<TType>? _provider;

        internal CustomAttributeDecoder(ModelHeap modelHeap, ICustomAttributeTypeProvider<TType>? provider)
        {
            _modelHeap = modelHeap;
            _provider = provider;
        }

        //If a provider was provided, this method is guaranteed to return true
        internal bool TryDecodeValue(CodedIndex ctorIndex, BlobIndex valueIndex, out CustomAttributeValue<TType> value)
        {
            BlobIndex sigIndex;
            BlobIndex typeSpecSig = default;

            switch (ctorIndex.TableKind)
            {
                case TableKind.MethodDef:
                    var methodDef = _modelHeap.MethodDefTable![ctorIndex];
                    sigIndex = methodDef.Signature;
                    break;

                case TableKind.MemberRef:
                    var memberRef = _modelHeap.MemberRefTable![ctorIndex];
                    sigIndex = memberRef.Signature;

                    if (memberRef.Class.TableKind == TableKind.TypeSpec)
                    {
                        var typeSpec = _modelHeap.TypeSpecTable![memberRef.Class];
                        typeSpecSig = typeSpec.Signature;
                    }

                    break;

                default:
                    throw new BadImageFormatException();
            }

            var methodSigReader = sigIndex.GetReader();
            var valueSigReader = valueIndex.GetReader();

            /* CustomAttrib
             * ============
             *                 ______________                        ____________
             *                /              \                      /            \
             *               v                \                    v              \
             * --> Prolog -------> FixedArg --------> NumNamed -------> NamedArg -------->
             *             \                     ^               \                   ^
             *              \___________________/                 \_________________/
             */

            var prolog = valueSigReader.ReadUInt16();

            if (prolog != 1)
                throw new BadImageFormatException();

            //The member sig should either be a II.23.2.1 MethodDefSig or a II.23.2.2 MethodRefSig

            //If it's a MethodDef sig, the first byte stores HASTHIS, EXPLICITTHIS and the calling convention.
            //The type is not allowed to be generic, which means the next byte after this should be ParamCount.
            //If it's a MethodRefSig, the first byte stores HASTHIS, EXPLICITTHIS and VARARG. So in both
            //scenarios, the next byte is ParamCount

            var flags = methodSigReader.ReadByte();

            var paramCount = methodSigReader.ReadCompressedInteger();
            var returnType = methodSigReader.ReadCorElementType();

            if (returnType != CorElementType.Void)
                throw new BadImageFormatException();

            ByteReader genericSigReader = default;

            if (typeSpecSig.Offset != 0)
            {
                genericSigReader = typeSpecSig.GetReader();

                var corElementType = genericSigReader.ReadCorElementType();

                //Check for a TypeSpec in the form
                //GENERICINST (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type
                if (corElementType == CorElementType.GenericInst)
                {
                    corElementType = genericSigReader.ReadCorElementType();

                    if (corElementType != CorElementType.Class && corElementType != CorElementType.ValueType)
                        throw new BadImageFormatException();

                    var token = genericSigReader.ReadToken();

                    //genericSigReader now points to GenArgCount
                }
                else
                {
                    genericSigReader = default;
                }
            }

            if (!TryDecodeFixedArgs(ref methodSigReader, ref valueSigReader, paramCount, genericSigReader, out var fixedArgs))
            {
                value = default;
                return false;
            }

            if (!TryDecodeNamedArgs(ref valueSigReader, out var namedArgs))
            {
                value = default;
                return false;
            }

            value = new CustomAttributeValue<TType>(fixedArgs, namedArgs);
            return true;
        }

        private bool TryDecodeFixedArgs(
            ref ByteReader methodSigReader,
            ref ByteReader valueSigReader,
            int paramCount,
            ByteReader genericSigReader,
            out CustomAttributeTypedArgument<TType>[] args)
        {
            /*
             * FixedArg
             * ========
             *
             *           If not SZARRAY
             * --------------------------------> Elem ---------------->
             *    |                                              ^
             *    |                                              |
             *    | if SZARRAY                __________         |
             *    |                          /          \        |
             *    v                         v            \       |
             *    --------> NumElem -----------> Elem ---------->|
             *
             * We handle this entire logic in DecodeElem. If it's an array,
             * we'll return a CustomAttributeTypedArgument[] encapsulating NumElem values
             */

            if (paramCount == 0)
            {
                args = Array.Empty<CustomAttributeTypedArgument<TType>>();
                return true;
            }

            args = new CustomAttributeTypedArgument<TType>[paramCount];

            for (var i = 0; i < paramCount; i++)
            {
                if (!TryDecodeFixedArgType(ref methodSigReader, genericSigReader, out var argTypeInfo))
                    return false;

                if (!TryDecodeElem(ref valueSigReader, argTypeInfo, out args[i]))
                    return false;
            }

            return true;
        }

        private bool TryDecodeNamedArgs(ref ByteReader valueSigReader, out CustomAttributeNamedArgument<TType>[] args)
        {
            /*
             * NamedArg
             * ========
             *
             * ------> FIELD -----------> FieldOrPropType --> FieldOrPropName --> FixedArg -->
             *   |                  ^
             *   |                  |
             *    ---> PROPERTY --->|
             */

            var numNamed = valueSigReader.ReadUInt16();

            if (numNamed == 0)
            {
                args = Array.Empty<CustomAttributeNamedArgument<TType>>();
                return true;
            }

            args = new CustomAttributeNamedArgument<TType>[numNamed];

            for (var i = 0; i < numNamed; i++)
            {
                var serializationType = valueSigReader.ReadSerializationType();

                if (serializationType != CorSerializationType.SERIALIZATION_TYPE_FIELD && serializationType != CorSerializationType.SERIALIZATION_TYPE_PROPERTY)
                    throw new BadImageFormatException();

                if (!TryDecodeFieldOrPropType(ref valueSigReader, out var info))
                    return false;

                var name = valueSigReader.ReadSerString();

                if (!TryDecodeElem(ref valueSigReader, info, out var arg))
                    return false;

                args[i] = new CustomAttributeNamedArgument<TType>(name, serializationType, arg.Type, arg.Value);
            }

            return true;
        }

        private bool TryDecodeElem(ref ByteReader valueSigReader, ArgTypeInfo info, out CustomAttributeTypedArgument<TType> arg)
        {
            /*
             * Elem
             * ====
             *
             *             simple or enum
             * -----------------------------------> val ----------------->
             *      |                                              ^
             *      |                                              |
             *      |      string or type                          |
             *      | ----------------------------> SerString ---->|
             *      |                                              |
             *      |                                              |
             *      |      boxed value type                        |
             *       ----> FieldOrPropType -------> Val ---------->|
             */

            if (info.SerializationType == CorSerializationType.SERIALIZATION_TYPE_TAGGED_OBJECT)
            {
                if (!TryDecodeFieldOrPropType(ref valueSigReader, out info))
                {
                    arg = default;
                    return false;
                }
            }

            object? value = null;

            switch (info.SerializationType)
            {
                case CorSerializationType.SERIALIZATION_TYPE_BOOLEAN:
                    value = valueSigReader.ReadBoolean();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_CHAR:
                    value = valueSigReader.ReadChar();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_U1:
                    value = valueSigReader.ReadByte();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_U2:
                    value = valueSigReader.ReadUInt16();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_U4:
                    value = valueSigReader.ReadUInt32();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_U8:
                    value = valueSigReader.ReadUInt64();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_I1:
                    value = valueSigReader.ReadSByte();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_I2:
                    value = valueSigReader.ReadUInt16();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_I4:
                    value = valueSigReader.ReadInt32();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_I8:
                    value = valueSigReader.ReadInt64();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_R4:
                    value = valueSigReader.ReadSingle();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_R8:
                    value = valueSigReader.ReadDouble();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_STRING:
                    value = valueSigReader.ReadSerString().ToString(); //It's going to be boxed, anyway, so just make it a full string
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_TYPE:
                    var typeName = valueSigReader.ReadSerString();

                    if (_provider != null)
                        value = _provider.GetTypeFromSerializedName(typeName);
                    else
                        value = typeName;
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_SZARRAY:
                    if (!TryDecodeArrayArg(ref valueSigReader, info, out var array))
                    {
                        arg = default;
                        return false;
                    }

                    value = array;
                    break;

                default:
                    throw new BadImageFormatException();
            }

            arg = new CustomAttributeTypedArgument<TType>(info.Type, value);
            return true;
        }

        private struct ArgTypeInfo
        {
            public TType Type;
            public TType ElementType;
            public CorSerializationType SerializationType;
            public CorSerializationType ElementSerializationType; //If we're an array
        }

        private bool TryDecodeFixedArgType(ref ByteReader methodSigReader, ByteReader genericSigReader, out ArgTypeInfo info)
        {
            var corElementType = methodSigReader.ReadCorElementType();

            /* Per II.23.3 (see the diagram in DecodeElem), the type of an elememt may be one of the following
             * 
             * - simple (bool, char, float, double, sbyte, short, int, long, byte, ushort, uint, or ulong)
             * - an enum
             * - string
             * - type
             * - a boxed FieldOrPropType (bool, char, sbyte, byte, short, ushort, int, uint, lon, ulong, float, double, string)
             */

            info = new ArgTypeInfo
            {
                SerializationType = (CorSerializationType) corElementType
            };

            switch (corElementType)
            {
                case CorElementType.Boolean:
                case CorElementType.Char:
                case CorElementType.U1:
                case CorElementType.U2:
                case CorElementType.U4:
                case CorElementType.U8:
                case CorElementType.I1:
                case CorElementType.I2:
                case CorElementType.I4:
                case CorElementType.I8:
                case CorElementType.R4:
                case CorElementType.R8:
                case CorElementType.String: //While string may not be "primitive" per se, it is "simple" in that it's well known
                    if (_provider != null)
                        info.Type = _provider.GetType(corElementType);
                    break;

                case CorElementType.Object:
                    //Something has been boxed
                    info.SerializationType = CorSerializationType.SERIALIZATION_TYPE_TAGGED_OBJECT;
                    if (_provider != null)
                        info.Type = _provider.GetType(corElementType);
                    break;

                case CorElementType.ValueType:
                case CorElementType.Class:
                    var token = methodSigReader.ReadToken();

                    //You can't not provide a provider when you're dealing with something that
                    //may be a type or an enum; we need to know what to set the serialization type
                    //to for when we actually try and read the value
                    if (_provider == null)
                        return false;

                    info.Type = GetTypeFromToken(token);
                    info.SerializationType = _provider.IsSystemType(info.Type)
                        ? CorSerializationType.SERIALIZATION_TYPE_TYPE
                        : (CorSerializationType) _provider.GetUnderlyingEnumType(info.Type);
                    break;

                case CorElementType.SZArray:
                    if (_provider == null)
                        return false;

                    if (!TryDecodeFixedArgType(ref methodSigReader, genericSigReader, out var elementInfo))
                        return false;

                    info.ElementType = elementInfo.Type;
                    info.ElementSerializationType = elementInfo.SerializationType;
                    info.Type = _provider.GetSZArrayType(info.ElementType);
                    break;

                case CorElementType.Var:
                    if (genericSigReader.Length == 0)
                        throw new BadImageFormatException();

                    var parameterIndex = methodSigReader.ReadCompressedInteger();
                    var numGenericParameters = genericSigReader.ReadCompressedInteger();

                    if (parameterIndex >= numGenericParameters)
                        throw new BadImageFormatException();

                    //Skip over all of the types in the TypeSpec signature until we get to the type we're actually interested in
                    while (parameterIndex > 0)
                    {
                        SkipType(ref genericSigReader);
                        parameterIndex--;
                    }

                    return TryDecodeFixedArgType(ref genericSigReader, default, out info);

                default:
                    throw new BadImageFormatException();
            }

            return true;
        }

        private bool TryDecodeArrayArg(ref ByteReader valueSigReader, ArgTypeInfo info, out CustomAttributeTypedArgument<TType>[]? array)
        {
            var count = valueSigReader.ReadInt32();

            if (count == -1)
            {
                array = null;
                return true;
            }

            if (count == 0)
            {
                array = Array.Empty<CustomAttributeTypedArgument<TType>>();
                return true;
            }

            if (count < 0)
                throw new BadImageFormatException();

            var elementInfo = new ArgTypeInfo
            {
                Type = info.ElementType,
                SerializationType = info.ElementSerializationType
            };

            array = new CustomAttributeTypedArgument<TType>[count];

            for (var i = 0; i < count; i++)
            {
                if (!TryDecodeElem(ref valueSigReader, elementInfo, out array[i]))
                    return false;
            }

            return true;
        }

        private bool TryDecodeFieldOrPropType(ref ByteReader valueSigReader, out ArgTypeInfo info)
        {
            //This is mostly the same as DecodeFixedArgType, except there's special handling for type/enum, and items are dispatched to name-specific methods rather than fixed-specific methods

            info = new ArgTypeInfo
            {
                SerializationType = valueSigReader.ReadSerializationType()
            };

            switch (info.SerializationType)
            {
                case CorSerializationType.SERIALIZATION_TYPE_BOOLEAN:
                case CorSerializationType.SERIALIZATION_TYPE_CHAR:
                case CorSerializationType.SERIALIZATION_TYPE_U1:
                case CorSerializationType.SERIALIZATION_TYPE_U2:
                case CorSerializationType.SERIALIZATION_TYPE_U4:
                case CorSerializationType.SERIALIZATION_TYPE_U8:
                case CorSerializationType.SERIALIZATION_TYPE_I1:
                case CorSerializationType.SERIALIZATION_TYPE_I2:
                case CorSerializationType.SERIALIZATION_TYPE_I4:
                case CorSerializationType.SERIALIZATION_TYPE_I8:
                case CorSerializationType.SERIALIZATION_TYPE_R4:
                case CorSerializationType.SERIALIZATION_TYPE_R8:
                case CorSerializationType.SERIALIZATION_TYPE_STRING: //While string may not be "primitive" per se, it is "simple" in that it's well known
                    if (_provider != null)
                        info.Type = _provider.GetType((CorElementType) info.SerializationType);
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_TYPE:
                    if (_provider != null)
                        info.Type = _provider.GetSystemType();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_TAGGED_OBJECT:
                    if (_provider != null)
                        info.Type = _provider.GetType(CorElementType.Object);
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_SZARRAY:
                    if (!TryDecodeFieldOrPropType(ref valueSigReader, out var elementInfo))
                        return false;

                    info.ElementType = elementInfo.Type;
                    info.ElementSerializationType = elementInfo.SerializationType;

                    //Even without a provider we can still read the element type; we just can't create it

                    if (_provider != null)
                        info.Type = _provider.GetSZArrayType(info.ElementType);
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_ENUM:
                    //If we don't have a provider, we can't get the underlying type at all

                    if (_provider == null)
                        return false;

                    var typeName = valueSigReader.ReadSerString();
                    info.Type = _provider.GetTypeFromSerializedName(typeName);
                    info.SerializationType = (CorSerializationType) _provider.GetUnderlyingEnumType(info.Type);
                    break;

                default:
                    throw new BadImageFormatException();
            }

            return true;
        }

        private TType GetTypeFromToken(mdToken token)
        {
            return token.Type switch
            {
                CorTokenType.mdtTypeDef => _provider.GetTypeDef(_modelHeap, (mdTypeDef) token),
                CorTokenType.mdtTypeRef => _provider.GetTypeRef(_modelHeap, (mdTypeRef) token)
            };
        }

        public static void SkipType(ref ByteReader reader)
        {
            var corElementType = reader.ReadCorElementType();

            switch (corElementType)
            {
                case CorElementType.Boolean:
                case CorElementType.Char:
                case CorElementType.U1:
                case CorElementType.U2:
                case CorElementType.U4:
                case CorElementType.U8:
                case CorElementType.I1:
                case CorElementType.I2:
                case CorElementType.I4:
                case CorElementType.I8:
                case CorElementType.R4:
                case CorElementType.Object:
                case CorElementType.String:
                case CorElementType.Void:
                case CorElementType.TypedByRef:
                    return;

                case CorElementType.Ptr:
                case CorElementType.ByRef:
                case CorElementType.Pinned:
                case CorElementType.SZArray:
                    SkipType(ref reader);
                    break;

                case CorElementType.FnPtr:
                    throw new NotImplementedException();

                case CorElementType.Array:
                    SkipType(ref reader);
                    reader.ReadCompressedInteger(); //rank
                    var numBounds = reader.ReadCompressedInteger();

                    //Read all the bounds
                    for (var i = 0; i < numBounds; i++)
                        reader.ReadCompressedInteger();

                    var numLowerBounds = reader.ReadCompressedInteger();

                    //Read all the lower bounds
                    for (var i = 0; i < numLowerBounds; i++)
                    {
                        reader.ReadCompressedInteger();
                    }
                    return;

                case CorElementType.CModReqd:
                case CorElementType.CModOpt:
                    reader.ReadToken();
                    SkipType(ref reader);
                    break;

                case CorElementType.GenericInst:
                    SkipType(ref reader);
                    var numGenericArgs = reader.ReadCompressedInteger();

                    for (var i = 0; i < numGenericArgs; i++)
                        SkipType(ref reader);

                    break;

                case CorElementType.Var:
                    reader.ReadCompressedInteger();
                    break;

                case CorElementType.Class:
                case CorElementType.ValueArray:
                    SkipType(ref reader);
                    break;

                default:
                    throw new BadImageFormatException();
            }
        }
    }
}
