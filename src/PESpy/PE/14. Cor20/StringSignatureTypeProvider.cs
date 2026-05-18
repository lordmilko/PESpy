using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    internal class StringSignatureTypeProvider : ISignatureTypeProvider<string, GenericParamList>
    {
        public static readonly StringSignatureTypeProvider Instance = new();

        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            throw new NotImplementedException();
        }

        public string GetGenericMethodParameter(GenericParamList genericContext, int index)
        {
            if (genericContext.Count != 0)
                return genericContext[index].ToString();

            //Per II.7.1 do ! for a generic type parameter and !! for a generic method parameter
            return $"!!{index}";
        }

        public string GetGenericTypeParameter(GenericParamList genericContext, int index)
        {
            if (genericContext.Count != 0)
                return genericContext[index].ToString();

            //Per II.7.1 do ! for a generic type parameter and !! for a generic method parameter
            return $"!{index}";
        }

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
        {
            throw new NotImplementedException();
        }

        public string GetPinnedType(string elementType)
        {
            throw new NotImplementedException();
        }

        public string GetTypeSpec(ModelHeap heap, mdTypeSpec mdTypeSpec, GenericParamList genericContext)
        {
            throw new NotImplementedException();
        }

        public string GetType(CorElementType corElementType)
        {
            return corElementType switch
            {
                CorElementType.Boolean => "bool",
                CorElementType.Char => "char",
                CorElementType.I1 => "sbyte",
                CorElementType.I2 => "short",
                CorElementType.I4 => "int",
                CorElementType.I8 => "long",
                CorElementType.U1 => "byte",
                CorElementType.U2 => "ushort",
                CorElementType.U4 => "uint",
                CorElementType.U8 => "ulong",
                CorElementType.R4 => "float",
                CorElementType.R8 => "double",
                CorElementType.I => "IntPtr",
                CorElementType.U => "UIntPtr",
                CorElementType.String => "string",
                CorElementType.Object => "object",
                CorElementType.Void => "void"
            };
        }

        public string GetTypeDef(ModelHeap heap, mdTypeDef mdTypeDef) =>
            heap.TypeDefTable.FromToken(mdTypeDef).ToString();

        public string GetTypeRef(ModelHeap heap, mdTypeRef mdTypeRef) =>
            heap.TypeRefTable.FromToken(mdTypeRef).ToString();

        public string GetGenericInstantiation(string genericType, string[] typeArgs)
        {
            using var builder = new ValueStringBuilder();

            var tick = genericType.IndexOf('`');

            if (tick != -1)
                builder.Append(genericType.AsSpan(0, tick));
            else
                builder.Append(genericType);

            builder.Append("<");

            for (var i = 0; i < typeArgs.Length; i++)
            {
                builder.Append(typeArgs[i]);

                if (i < typeArgs.Length - 1)
                    builder.Append(", ");
            }

            builder.Append(">");

            return builder.ToString();
        }

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            throw new NotImplementedException();
        }

        public string GetByRefType(string elementType) => $"{elementType}&";
        public string GetPointerType(string elementType) => $"{elementType}*";

        public string GetSZArrayType(string elementType) => $"{elementType}[]";
    }
}
