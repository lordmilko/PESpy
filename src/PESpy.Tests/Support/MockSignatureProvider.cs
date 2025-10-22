using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ClrDebug;

namespace PESpy.Tests
{
    internal class MockSignatureProvider : ISignatureTypeProvider<object, object>
    {
        public object GetArrayType(object elementType, ArrayShape shape)
        {
            throw new NotImplementedException();
        }

        public object GetByReferenceType(object elementType)
        {
            throw new NotImplementedException();
        }

        public object GetFunctionPointerType(MethodSignature<object> signature)
        {
            throw new NotImplementedException();
        }

        public object GetGenericInstantiation(object genericType, ImmutableArray<object> typeArguments)
        {
            return (genericType, typeArguments);
        }

        public object GetGenericMethodParameter(object genericContext, int index)
        {
            throw new NotImplementedException();
        }

        public object GetGenericTypeParameter(object genericContext, int index)
        {
            throw new NotImplementedException();
        }

        public object GetModifiedType(object modifier, object unmodifiedType, bool isRequired)
        {
            throw new NotImplementedException();
        }

        public object GetPinnedType(object elementType)
        {
            throw new NotImplementedException();
        }

        public object GetPointerType(object elementType)
        {
            throw new NotImplementedException();
        }

        public object GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            throw new NotImplementedException();
        }

        public object GetSZArrayType(object elementType)
        {
            throw new NotImplementedException();
        }

        public object GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }

        public object GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            return (mdToken) MetadataTokens.GetToken(handle);
        }

        public object GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }
    }
}
