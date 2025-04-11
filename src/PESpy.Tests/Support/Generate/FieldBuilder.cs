using System;
using System.Diagnostics;

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

                var str = GetDisplayName(Type);

                if (IsArray)
                    return $"Span<{str}>";

                return str;
            }
        }

        public int NumElems { get; }

        public bool IsArray => NumElems != -1;

        public bool IsPointer { get; }

        public Type EnumImpl { get; }

        public bool x86Only { get; }

        public bool Eager { get; }

        public int Size
        {
            get
            {
                if (IsArray)
                    return GetSize(Type) * NumElems;

                if (Type == typeof(string))
                {
                    if (nullPaddedUTF8 != -1)
                        return nullPaddedUTF8;
                }

                return GetSize(Type);
            }
        }

        private int GetSize(Type type)
        {
            if (type.IsEnum)
                return GetSize(EnumImpl);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return 1;

                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return 2;

                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return 4;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return 8;

                default:
                    return ctx.GetStruct(type.Name).Size;
            }
        }

        private Type va;
        private bool rva;
        private int nullPaddedUTF8;

        private GenerationContext ctx;

        public FieldBuilder(
            string name,
            Type type,
            GenerationContext ctx,

            int numElems,
            Type va,
            bool rva,
            bool pointer,
            bool x86Only,
            int nullPaddedUTF8,
            Type enumImpl,
            bool eager)
        {
            Name = name;
            Type = type;
            this.ctx = ctx;

            if (type.IsEnum)
            {
                if (enumImpl == null)
                    throw new ArgumentException($"{type.Name} requires an enumImpl");

                if (!enumImpl.Name.StartsWith("U") && enumImpl != typeof(byte))
                    throw new NotImplementedException(); //enumimpl must be unsigned, as casting from a signed value may cause a leading 0xffff in the resulting value
            }

            NumElems = numElems;
            EnumImpl = enumImpl;
            this.va = va;
            this.rva = rva;
            IsPointer = pointer;
            this.x86Only = x86Only;
            this.nullPaddedUTF8 = nullPaddedUTF8;
            Eager = eager;
        }
    }
}
