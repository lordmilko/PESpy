using ClrDebug;

namespace PESpy.Ecma335
{
    public interface ISimpleTypeProvider<TType>
    {
        TType GetType(CorElementType corElementType);

        //Passing in the heap allows you to potentially have a static type provider instance
        TType GetTypeDef(CompressedModelHeap heap, mdTypeDef mdTypeDef);

        TType GetTypeRef(CompressedModelHeap heap, mdTypeRef mdTypeRef);
    }

    public interface ISZArrayTypeProvider<TType>
    {
        TType GetSZArrayType(TType elementType);
    }

    public interface ICustomAttributeTypeProvider<TType> : ISimpleTypeProvider<TType>, ISZArrayTypeProvider<TType>
    {
        TType GetSystemType();

        bool IsSystemType(TType type);

        TType GetTypeFromSerializedName(string typeName);

        CorElementType GetUnderlyingEnumType(TType type);
    }

    public interface IConstructedTypeProvider<TType> : ISZArrayTypeProvider<TType>
    {
        TType GetGenericInstantiation(TType genericType, TType[] typeArgs);

        TType GetArrayType(TType elementType, ArrayShape shape);

        TType GetByRefType(TType elementType);

        TType GetPointerType(TType elementType);
    }

    public interface ISignatureTypeProvider<TType, TGenericContext> : ISimpleTypeProvider<TType>, IConstructedTypeProvider<TType>
    {
        TType GetFunctionPointerType(MethodSignature<TType> signature);

        TType GetGenericMethodParameter(TGenericContext genericContext, int index);

        TType GetGenericTypeParameter(TGenericContext genericContext, int index);

        TType GetModifiedType(TType modifier, TType unmodifiedType, bool isRequired);

        TType GetPinnedType(TType elementType);

        TType GetTypeSpec(CompressedModelHeap heap, mdTypeSpec mdTypeSpec, TGenericContext genericContext);
    }
}
