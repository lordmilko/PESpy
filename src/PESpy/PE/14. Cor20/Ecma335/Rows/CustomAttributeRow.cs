using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct CustomAttributeRow : IValue, IViewable
    {
        public CustomAttributeIndex RowIndex { get; }

        public CodedIndex Parent => table.GetParent(RowIndex);

        //While this property is called Type as per II.22.10,
        //it's really a MethodDef or MemberRef describing
        //the constructor of the attribute
        public CodedIndex Type => table.GetType(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object TypeRow => Type.GetRow(table.CompressedModelHeap);

        private readonly CustomAttributeTable table;

        internal CustomAttributeRow(CustomAttributeIndex index, CustomAttributeTable table)
        {
            //II.22.10

            RowIndex = index;
            this.table = table;
        }

        public bool TryGetName(out StringIndex namespaceIndex, out StringIndex nameIndex)
        {
            if (!TryGetTypeAndConstructor(out var customAttributeType, out _))
            {
                namespaceIndex = default;
                nameIndex = default;
                return false;
            }

            return TryGetTypeNamespaceAndName(customAttributeType, out namespaceIndex, out nameIndex);
        }

        private bool TryGetTypeNamespaceAndName(mdToken customAttributeType, out StringIndex namespaceIndex, out StringIndex nameIndex)
        {
            namespaceIndex = default;
            nameIndex = default;

            if (customAttributeType.Type == CorTokenType.mdtTypeDef)
            {
                var typeDefRow = table.CompressedModelHeap.TypeDefTable.FromToken(customAttributeType);

                nameIndex = typeDefRow.TypeName;
                namespaceIndex = typeDefRow.TypeNamespace;
                return true;
            }
            else if (customAttributeType.Type == CorTokenType.mdtTypeRef)
            {
                var typeRefRow = table.CompressedModelHeap.TypeRefTable.FromToken(customAttributeType);
                var resolutionScopeKind = typeRefRow.ResolutionScope.TableKind;

                //If it's a nested type, it's too complex for us to resolve just based on simple metadata
                if (resolutionScopeKind == TableKind.TypeRef || resolutionScopeKind == TableKind.TypeDef)
                    return false;

                nameIndex = typeRefRow.TypeName;
                namespaceIndex = typeRefRow.TypeNamespace;
                return true;
            }
            else if (customAttributeType.Type == CorTokenType.mdtTypeSpec)
            {
                retry:
                var typeSpecRow = table.CompressedModelHeap.TypeSpecTable.FromToken(customAttributeType);

                //We aren't exactly able to get the full name, but we can at least try and get the generic type definition

                var reader = typeSpecRow.Signature.GetReader();

                var corElementType = reader.ReadCorElementType();

                if (corElementType != CorElementType.GenericInst)
                    throw new BadImageFormatException();

                corElementType = reader.ReadCorElementType();

                if (corElementType != CorElementType.Class && corElementType != CorElementType.ValueType)
                    throw new BadImageFormatException();

                var token = reader.ReadToken();

                switch (token.Type)
                {
                    case CorTokenType.mdtTypeDef:
                        var typeDefRow = table.CompressedModelHeap.TypeDefTable.FromToken(token);

                        nameIndex = typeDefRow.TypeName;
                        namespaceIndex = typeDefRow.TypeNamespace;
                        break;

                    case CorTokenType.mdtTypeRef:
                        var typeRefRow = table.CompressedModelHeap.TypeRefTable.FromToken(token);

                        nameIndex = typeRefRow.TypeName;
                        namespaceIndex = typeRefRow.TypeNamespace;
                        break;

                    case CorTokenType.mdtTypeSpec:
                        typeSpecRow = table.CompressedModelHeap.TypeSpecTable.FromToken(token);
                        goto retry;

                    default:
                        throw new BadImageFormatException();
                }

                return true;
            }
            else
                throw new NotImplementedException($"Don't know how to handle a custom attribute type of kind '{customAttributeType.Type}'");
        }

        private bool TryGetTypeAndConstructor(out mdToken customAttributeType, out CodedIndex customAttributeCtor)
        {
            customAttributeCtor = Type;

            if (customAttributeCtor.TableKind == TableKind.MemberRef)
            {
                var type = table.CompressedModelHeap.MemberRefTable[customAttributeCtor].Class;

                customAttributeType = (mdToken) type;
                return true;
            }

            if (customAttributeCtor.TableKind == TableKind.MethodDef)
            {
                var declaringType = table.CompressedModelHeap.MethodDefTable[customAttributeCtor].DeclaringType;

                if (declaringType != null)
                {
                    customAttributeType = Extensions.TokenFromRid(declaringType.Value.RowIndex.RowId, CorTokenType.mdtTypeDef);
                    return true;
                }
            }

            customAttributeType = default;
            return false;
        }

        /// <summary>
        /// Tries to decode the custom attribute without the use of a <see cref="ICustomAttributeTypeProvider{TType}"/>
        /// that forwards to a complex type system. If any custom attribute arguments are found that reference boxed value
        /// enum or System.Type types, this method will return <see langword="false"/>.
        /// </summary>
        /// <param name="value">The arguments of the decoded custom attribute.</param>
        /// <returns></returns>
        public bool TryDecodeValue(out CustomAttributeValue<object> value)
        {
            var decoder = new CustomAttributeDecoder<object>(table.CompressedModelHeap, null);
            return decoder.TryDecodeValue(Type, Value, out value);
        }

        public CustomAttributeValue<object> DecodeValue()
        {
            if (!TryDecodeValue(out var value))
                throw new InvalidOperationException("Custom attribute is too complex to decode without specifying a ICustomAttributeTypeProvider.");

            return value;
        }

        public CustomAttributeValue<TType> DecodeValue<TType>(ICustomAttributeTypeProvider<TType> provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var decoder = new CustomAttributeDecoder<TType>(table.CompressedModelHeap, provider);
            var result = decoder.TryDecodeValue(Type, Value, out var value);
            Debug.Assert(result); //TryDecodeValue should not return false when a provider was provided

            return value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CustomAttributeRow, this, ViewKind.Metadata_CustomAttributeRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteHasCustomAttributeIndex(nameof(Parent), table.ParentOffset, Parent);
                    break;

                case 1:
                    structWriter.WriteCustomAttributeTypeIndex(nameof(Type), table.TypeOffset, Type);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(Value), table.ValueOffset, Value);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            TryGetTypeAndConstructor(out var customAttributeType, out _);

            if (customAttributeType.Type == CorTokenType.mdtTypeSpec)
                return table.CompressedModelHeap.TypeSpecTable.FromToken(customAttributeType).ToString();

            TryGetTypeNamespaceAndName(customAttributeType, out var namespaceHandle, out var nameHandle);

            var ns = namespaceHandle.GetString();
            var name = nameHandle.GetString();

            using var builder = new ValueStringBuilder();

            if (ns.Length == 0)
                builder.Append(name.AsSpan());
            else
            {
                builder.Append(ns.AsSpan());
                builder.Append('.');
                builder.Append(name.AsSpan());
            }

            if (TryDecodeValue(out var value))
            {
                if (value.FixedArgs.Length > 0 || value.NamedArgs.Length > 0)
                {
                    builder.Append('(');

                    if (value.FixedArgs.Length > 0)
                    {
                        for (var i = 0; i < value.FixedArgs.Length; i++)
                        {
                            var val = value.FixedArgs[i].Value;

                            if (val is bool b)
                                builder.Append(b ? "true" : "false");
                            else if (val is string s)
                            {
                                builder.Append('"');
                                builder.Append(s);
                                builder.Append('"');
                            }
                            else
                                builder.Append(val.ToString());

                            if (i < value.FixedArgs.Length - 1)
                                builder.Append(", ");
                        }

                        if (value.NamedArgs.Length > 0)
                            builder.Append(", ");
                    }

                    for (var i = 0; i < value.NamedArgs.Length; i++)
                    {
                        var arg = value.NamedArgs[i];
                        builder.Append(arg.Name);
                        builder.Append(" = ");

                        var val = arg.Value;

                        if (val is bool b)
                            builder.Append(b ? "true" : "false");
                        else if (val is string s)
                        {
                            builder.Append('"');
                            builder.Append(s);
                            builder.Append('"');
                        }
                        else
                            builder.Append(val.ToString());

                        if (i < value.NamedArgs.Length - 1)
                            builder.Append(", ");
                    }

                    builder.Append(')');
                }
            }
            else
            {
                builder.Append("(<complex>)");
            }

            return builder.ToString();
        }
    }
}
