using System;
using System.Diagnostics;
using static PESpy.Tests.StringType;

namespace PESpy.Tests
{
    class FieldBuilder
    {
        internal static string GetDisplayName(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => "bool",
                TypeCode.Byte => "byte",
                TypeCode.Char => "char",
                TypeCode.Decimal => "decimal",
                TypeCode.Double => "double",
                TypeCode.Int16 => "short",
                TypeCode.Int32 => "int",
                TypeCode.Int64 => "long",
                TypeCode.SByte => "sbyte",
                TypeCode.Single => "float",
                TypeCode.String => "string",
                TypeCode.UInt16 => "ushort",
                TypeCode.UInt32 => "uint",
                TypeCode.UInt64 => "ulong",
                _ => type.Name
            };
        }

        public string Name { get; }

        public Type Type { get; }

        public string TypeName
        {
            get
            {
                if (Type.IsEnum)
                {
                    Debug.Assert(!IsArray);
                    return Type.Name;
                }

                if (Type == typeof(string))
                {
                    switch (StringType)
                    {
                        case AnsiNullTerminated:
                            return "AnsiString";

                        case Utf8NullTerminated:
                            return "Utf8String";

                        case Utf16NullTerminated:
                            return "Utf16String";

                        case NullPaddedUTF8:
                            return "Utf8String";

                        case UnicodeFixedLength:
                            if (IsArray)
                                throw new NotImplementedException();

                            return "ReadOnlySpan<char>";

                        default:
                            throw new NotImplementedException($"Don't know how to handle {nameof(PESpy.Tests.StringType)} '{StringType}'");
                    }
                }

                var str = GetDisplayName(Type);

                if (IsArray)
                    return $"Span<{str}>";

                return str;
            }
        }

        public string NumElems { get; }

        public bool IsArray => NumElems != null && StringType == null;

        public bool IsPointer { get; }

        public Type SerializationType { get; }

        public bool x86Only { get; }

        public bool Eager { get; }

        public string Modifier { get; }

        public string Size
        {
            get
            {
                if (IsArray)
                {
                    var elementSize = GetSize(Type);

                    if (int.TryParse(elementSize, out var elmSize))
                    {
                        if (int.TryParse(NumElems, out var numElems))
                            return (elmSize * numElems).ToString();
                    }

                    return $"{elementSize} * {NumElems}";
                }

                if (Type == typeof(string))
                {
                    switch (StringType)
                    {
                        case null:
                            throw new NotImplementedException("StringType must be specified when type is string");

                        case NullPaddedUTF8:
                            if (nullPaddedUTF8 != -1)
                                return nullPaddedUTF8.ToString();

                            throw new NotImplementedException("A null-padded length must be specified when StringType is NullPaddedUTF8");

                        case AnsiNullTerminated:
                        case Utf8NullTerminated:
                            return $"({Name}.Length + 1)";

                        case Utf16NullTerminated:
                            return $"(({Name}.Length * 2) + 2)";

                        case UnicodeFixedLength:
                            return NumElems;

                        default:
                            throw new NotImplementedException($"Don't know how to handle {nameof(PESpy.Tests.StringType)} '{StringType}'");
                    }
                }

                return GetSize(Type).ToString();
            }
        }

        private string GetSize(Type type)
        {
            if (type.IsEnum)
                return GetSize(SerializationType);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return "1";

                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return "2";

                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return "4";

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return "8";

                case TypeCode.String:
                    throw new NotImplementedException("Don't know how to get the size of a string");

                case TypeCode.Boolean:
                    if (SerializationType == null)
                        throw new NotImplementedException("Can't get the size of a bool when a serialization type is not specified");

                    return GetSize(SerializationType);

                default:
                    switch (type.Name)
                    {
                        case "Guid":
                            return "16";

                        default:
                            return ctx.GetStruct(type.Name).Size;
                    }
            }
        }

        private Type va;
        private Type rva;
        public StringType? StringType { get; }
        private int nullPaddedUTF8;

        private GenerationContext ctx;

        public FieldBuilder(
            string name,
            Type type,
            GenerationContext ctx,

            string numElems,
            Type va,
            Type rva,
            bool pointer,
            bool x86Only,
            StringType? stringType,
            int nullPaddedUTF8,
            Type serializationType,
            bool eager,
            string modifier)
        {
            Name = name;
            Type = type;
            this.ctx = ctx;

            if (type.IsEnum)
            {
                if (serializationType == null)
                    throw new ArgumentException($"{type.Name} requires an enumImpl");

                if (!serializationType.IsPrimitive)
                    throw new NotImplementedException("Don't know how to handle a serialization type that is not primitive");

                if (!serializationType.Name.StartsWith("U") && serializationType != typeof(byte))
                    throw new NotImplementedException("Serialization Type must be unsigned, as casting from a signed value may cause a leading 0xffff in the resulting value");
            }

            NumElems = numElems;
            SerializationType = serializationType;
            this.va = va;
            this.rva = rva;
            IsPointer = pointer;
            this.x86Only = x86Only;
            StringType = stringType;
            this.nullPaddedUTF8 = nullPaddedUTF8;
            Eager = eager;
            Modifier = modifier;
        }
    }
}
