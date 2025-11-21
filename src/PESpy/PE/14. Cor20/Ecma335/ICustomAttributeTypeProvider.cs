using ClrDebug;

namespace PESpy.Ecma335
{
    public interface ICustomAttributeTypeProvider<TType>
    {
        TType GetSystemType();

        bool IsSystemType(TType type);

        TType GetType(CorElementType corElementType);

        TType GetTypeFromSerializedName(string typeName);

        CorElementType GetUnderlyingEnumType(TType type);

        TType GetTypeDef(mdTypeDef typeDef);

        TType GetTypeRef(mdTypeRef typeRef);
    }
}
