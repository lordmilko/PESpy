using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    internal readonly struct CustomAttributeDecoder<TType>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly ICustomAttributeTypeProvider<TType>? provider;

        internal CustomAttributeDecoder(CompressedModelHeap compressedModelHeap, ICustomAttributeTypeProvider<TType> provider)
        {
            this.compressedModelHeap = compressedModelHeap;
            this.provider = provider;
        }

        public CustomAttributeValue<TType> DecodeValue(CodedIndex ctorIndex, BlobIndex valueIndex)
        {
            BlobIndex sigIndex;

            switch (ctorIndex.TableKind)
            {
                case TableKind.MethodDef:
                case TableKind.MemberRef:
                    var memberRef = compressedModelHeap.MemberRefTable[ctorIndex.RowId];
                    sigIndex = memberRef.Signature;

                    if (memberRef.Class.TableKind == TableKind.TypeSpec)
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
            var fixedArgs = DecodeFixedArgs(ref methodSigReader, ref valueSigReader, paramCount);
            var namedArgs = DecodeNamedArgs(ref valueSigReader);

            return new CustomAttributeValue<TType>(fixedArgs, namedArgs);
        }

        private CustomAttributeTypedArgument<TType>[] DecodeFixedArgs(
            ref ByteReader methodSigReader,
            ref ByteReader valueSigReader,
            int paramCount)
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
                return Array.Empty<CustomAttributeTypedArgument<TType>>();

            var args = new CustomAttributeTypedArgument<TType>[paramCount];

            for (var i = 0; i < paramCount; i++)
            {
                var argTypeInfo = DecodeFixedArgType(ref methodSigReader);
                args[i] = DecodeElem(ref valueSigReader, argTypeInfo);
            }

            return args;
        }

        private CustomAttributeNamedArgument<TType>[] DecodeNamedArgs(ref ByteReader valueSigReader)
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
                return Array.Empty<CustomAttributeNamedArgument<TType>>();

            var args = new CustomAttributeNamedArgument<TType>[numNamed];

            for (var i = 0; i < numNamed; i++)
            {
                var serializationType = valueSigReader.ReadSerializationType();

                if (serializationType != CorSerializationType.SERIALIZATION_TYPE_FIELD && serializationType != CorSerializationType.SERIALIZATION_TYPE_PROPERTY)
                    throw new BadImageFormatException();

                var info = DecodeFieldOrPropType(ref valueSigReader);
                var name = valueSigReader.ReadSerString();

                var arg = DecodeElem(ref valueSigReader, info);
                args[i] = new CustomAttributeNamedArgument<TType>(name, serializationType, arg.Type, arg.Value);
            }

            return args;
        }

        private CustomAttributeTypedArgument<TType> DecodeElem(ref ByteReader valueSigReader, ArgTypeInfo info)
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
                info = DecodeFieldOrPropType(ref valueSigReader);

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
                    value = valueSigReader.ReadSerString();
                    break;

                case CorSerializationType.SERIALIZATION_TYPE_TYPE:
                    var typeName = valueSigReader.ReadSerString();

                    if (provider != null)
                        value = provider.GetTypeFromSerializedName(typeName);
                    else
                        value = typeName;
                    break;
                default:
                    throw new BadImageFormatException();
            }

            return new CustomAttributeTypedArgument<TType>(info.Type, value);
        }

        private struct ArgTypeInfo
        {
            public TType Type;
            public TType ElementType;
            public CorSerializationType SerializationType;
            public CorSerializationType ElementSerializationType; //If we're an array
        }

        private ArgTypeInfo DecodeFixedArgType(ref ByteReader methodSigReader)
        {
            var corElementType = methodSigReader.ReadCorElementType();

            /* Per II.23.3 (see the diagram in DecodeElem), the type of an elememt may be one of the following
             * 
             * - simple (bool, char, float, double, sbyte, short, int, long, byte, ushort, uint, or ulong)
             * - an enum
             * - string
             * - type
             * - a boxed FieldorPropType (bool, char, sbyte, byte, short, ushort, int, uint, lon, ulong, float, double, string)
             */

            var info = new ArgTypeInfo
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
                    if (provider != null)
                        info.Type = provider.GetType(corElementType);
                    break;

                case CorElementType.Object:
                    //Something has been boxed
                    info.SerializationType = CorSerializationType.SERIALIZATION_TYPE_TAGGED_OBJECT;
                    if (provider != null)
                        info.Type = provider.GetType(corElementType);
                    break;

                case CorElementType.ValueType:
                case CorElementType.Class:
                    var token = methodSigReader.ReadToken();

                    //You can't not provide a provider when you're dealing with something that
                    //may be a type or an enum; we need to know what to set the serialization type
                    //to for when we actually try and read the value
                    info.Type = GetTypeFromToken(token);
                    info.SerializationType = provider.IsSystemType(info.Type)
                        ? CorSerializationType.SERIALIZATION_TYPE_TYPE
                        : (CorSerializationType) provider.GetUnderlyingEnumType(info.Type);
                    break;
        private ArgTypeInfo DecodeFieldOrPropType(ref ByteReader valueSigReader)
        {
            //This is mostly the same as DecodeFixedArgType, except there's special handling for type/enum, and items are dispatched to name-specific methods rather than fixed-specific methods

            var info = new ArgTypeInfo
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
                    if (provider != null)
                        info.Type = provider.GetType((CorElementType) info.SerializationType);
                    break;
                default:
                    throw new BadImageFormatException();
            }

            return info;
        }

        private TType GetTypeFromToken(mdToken token)
        {
            return token.Type switch
            {
                CorTokenType.mdtTypeDef => provider.GetTypeDef((mdTypeDef) token),
                CorTokenType.mdtTypeRef => provider.GetTypeRef((mdTypeRef) token)
            };
        }
    }
}
