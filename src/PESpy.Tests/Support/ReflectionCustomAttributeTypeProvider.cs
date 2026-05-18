using System;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Tests
{
    class ReflectionCustomAttributeTypeProvider : ICustomAttributeTypeProvider<Type>
    {
        public Type GetSystemType()
        {
            throw new NotImplementedException();
        }

        public bool IsSystemType(Type type) => type == typeof(Type);

        public Type GetType(CorElementType corElementType)
        {
            return corElementType switch
            {
                CorElementType.Boolean => typeof(bool),
                CorElementType.Char => typeof(char),
                CorElementType.I1 => typeof(sbyte),
                CorElementType.I2 => typeof(short),
                CorElementType.I4 => typeof(int),
                CorElementType.I8 => typeof(long),
                CorElementType.U1 => typeof(byte),
                CorElementType.U2 => typeof(ushort),
                CorElementType.U4 => typeof(uint),
                CorElementType.U8 => typeof(ulong),
                CorElementType.R4 => typeof(float),
                CorElementType.R8 => typeof(double),
                CorElementType.I => typeof(IntPtr),
                CorElementType.U => typeof(UIntPtr),
                CorElementType.String => typeof(string),
                CorElementType.Object => typeof(object),
            };
        }

        public Type GetTypeFromSerializedName(string typeName)
        {
            return Type.GetType(typeName, throwOnError: true);
        }

        public CorElementType GetUnderlyingEnumType(Type type)
        {
            var underlyingType = Enum.GetUnderlyingType(type);

            var typeCode = Type.GetTypeCode(underlyingType);

            return typeCode switch
            {
                TypeCode.SByte => CorElementType.I1,
                TypeCode.Int16 => CorElementType.I2,
                TypeCode.Int32 => CorElementType.I4,
                TypeCode.Int64 => CorElementType.I8,
                TypeCode.Byte => CorElementType.U1,
                TypeCode.UInt16 => CorElementType.U2,
                TypeCode.UInt32 => CorElementType.U4,
                TypeCode.UInt64 => CorElementType.U8,
            };
        }

        public Type GetTypeDef(ModelHeap heap, mdTypeDef typeDef)
        {
            throw new NotImplementedException();
        }

        public Type GetTypeRef(ModelHeap heap, mdTypeRef mdTypeRef)
        {
            var typeRef = heap.TypeRefTable.FromToken(mdTypeRef);

            switch (typeRef.ResolutionScope.TableKind)
            {
                case TableKind.TypeRef:
                    var outerType = GetTypeRef(heap, (mdTypeRef) (mdToken) typeRef.ResolutionScope);

                    return outerType.GetNestedType(typeRef.TypeName.GetString().ToString());

                case TableKind.AssemblyRef:
                    var assemblyInfo = heap.AssemblyRefTable[typeRef.ResolutionScope];
                    var expectedName = assemblyInfo.Name.GetString().ToString();

                    var assembly = AppDomain.CurrentDomain.GetAssemblies().First(f => f.GetName().Name == expectedName);

                    return assembly.GetType($"{typeRef.TypeNamespace.GetString()}.{typeRef.TypeName.GetString()}");

                default:
                    throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        public Type GetSZArrayType(Type elementType) => elementType.MakeArrayType();
    }
}
