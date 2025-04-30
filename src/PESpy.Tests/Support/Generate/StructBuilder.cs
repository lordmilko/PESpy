using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PESpy.Tests
{
    enum StringType
    {
        None,
        AnsiNullTerminated,
        Utf8NullTerminated,
        Utf16NullTerminated,
        NullPaddedUTF8,
        UnicodeFixedLength
    }

    class StructBuilder
    {
        public string Name { get; }

        public string Size
        {
            get
            {
                int sum = 0;

                StringBuilder dynamicSize = null;

                foreach (var field in Fields)
                {
                    var s = field.Size;

                    if (int.TryParse(s, out var i))
                        sum += i;
                    else
                    {
                        if (dynamicSize == null)
                        {
                            dynamicSize = new StringBuilder();
                            dynamicSize.Append(s);
                        }
                        else
                        {
                            dynamicSize.Append(" + ").Append(s);
                        }
                    }
                }

                if (dynamicSize == null)
                    return sum.ToString();

                return sum + " + " + dynamicSize.ToString();
            }
        }

        public List<FieldBuilder> Fields { get; } = new List<FieldBuilder>();

        private GenerationContext ctx;

        public StructBuilder(string name, string help, GenerationContext ctx)
        {
            Name = name;
            this.ctx = ctx;
        }

        /// <summary>
        /// Adds a new field to the struct.
        /// </summary>
        /// <param name="name">The name of the field to add.</param>
        /// <param name="type">The managed type of the field. If the field is an enum, this should be the enum type and <paramref name="enumImpl"/> should be the underlying type.</param>
        /// <param name="numElems">The desired number of elements in an array.</param>
        /// <param name="va"></param>
        /// <param name="rva"></param>
        /// <param name="pointer">Whether this field is 4 bytes in x86 and 8 bytes in x64 (i.e. pointer length).</param>
        /// <param name="x86Only">Whether this field only exists in x86.</param>
        /// <param name="stringType">If this is a string, the type of string that it is.</param>
        /// <param name="nullPaddedUTF8">If this is a null-padded UTF 8 string, the number of characters (including trailing null pads) that encompass the string</param>
        /// <param name="serializationType">If <paramref name="type"/> is an enum or bool type, this is the underlying type that should actually be read.</param>
        /// <param name="help">XmlDocs that should be displayed on the field.</param>
        /// <param name="eager">Whether to eagerly initialize this field in the constructor and use readonly fields that can be referenced by ref.</param>
        /// <param name="lengthCondition">The field is only conditionally read if its offset is less than the specified length field</param>
        /// <returns>This <see cref="StructBuilder"/>.</returns>
        public StructBuilder Field(
            string name,
            Type type,

            string numElems = null,
            Type va = null,
            Type rva = null,
            bool pointer = false,
            bool x86Only = false,
            StringType? stringType = null,
            int nullPaddedUTF8 = -1,
            Type serializationType = null,
            string help = null,
            bool eager = false,
            string modifier = null,
            string lengthCondition = null)
        {
            var field = new FieldBuilder(
                name,
                type,
                ctx,
                numElems,
                va,
                rva,
                pointer,
                x86Only,
                stringType,
                nullPaddedUTF8,
                serializationType,
                eager,
                modifier,
                lengthCondition
            );

            Fields.Add(field);

            return this;
        }
    }
}
