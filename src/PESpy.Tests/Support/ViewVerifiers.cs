using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    static class ViewVerifiers
    {
        public static void VerifyStruct(this IView view, string name, int offset, int size, params Action<IView>[] verifyChildren)
        {
            Assert.IsInstanceOfType(view, typeof(StructView));

            var structView = (StructView) view;

            Assert.AreEqual(name, structView.Name, "Name was incorrect");
            Assert.AreEqual(offset, structView.Offset, $"Offset of {name} was incorrect. Also size is {structView.Size}");
            Assert.AreEqual(size, structView.Size, $"Size of {name} was incorrect");

            Assert.AreEqual(verifyChildren.Length, structView.Children.Length, "Number of StructView fields was incorrect");

            for (var i = 0; i < structView.Children.Length; i++)
                verifyChildren[i](structView.Children[i]);
        }

        public static void VerifyStructIgnoreChildren(this IView view, string name, int offset, int size)
        {
            Assert.IsInstanceOfType(view, typeof(StructView));

            var structView = (StructView) view;

            Assert.AreEqual(name, structView.Name, "Name was incorrect");
            Assert.AreEqual(offset, structView.Offset, $"Offset of {name} was incorrect. Also, size is {structView.Size}");
            Assert.AreEqual(size, structView.Size, $"Size of {name} was incorrect");
        }

        public static void VerifyField(this IView view, string name, object value)
        {
            Assert.IsInstanceOfType(view, typeof(IFieldView), $"{name} should not be a field");

            var fieldView = (IFieldView) view;

            Assert.AreEqual(name, fieldView.Name);

            var expectedType = value.GetType();
            var actualType = fieldView.Value.GetType();

            Assert.AreEqual(expectedType.IsArray, actualType.IsArray);

            if (expectedType.IsArray)
            {
                var expectedArray = (Array) value;
                var actualArray = (Array) fieldView.Value;

                Assert.AreEqual(expectedArray.Length, actualArray.Length);

                for (var i = 0; i < expectedArray.Length; i++)
                    Assert.AreEqual(expectedArray.GetValue(i), actualArray.GetValue(i));
            }
            else
            {
                var fieldValue = fieldView.Value;

                if (value is string s) //Could be PCSTR
                    fieldValue = fieldValue.ToString();

                Assert.AreEqual(value, fieldValue, $"Value of field {name} was incorrect");
            }
        }

        public static void VerifyFieldIgnoreValue(this IView view, string name)
        {
            Assert.IsInstanceOfType(view, typeof(IFieldView));

            var fieldView = (IFieldView) view;

            Assert.AreEqual(name, fieldView.Name);
        }

        public static void VerifyBitField(this IView view, string name, object value, int bits)
        {
            Assert.IsInstanceOfType(view, typeof(IBitFieldView));

            var bitFieldView = (IBitFieldView) view;

            Assert.AreEqual(name, bitFieldView.Name);
            Assert.AreEqual(value, bitFieldView.Value);
            Assert.AreEqual(bits, bitFieldView.Bits);
        }

        public static void VerifyValue(this IView view, int offset, object value)
        {
            Assert.IsInstanceOfType(view, typeof(IValueView));

            var valueView = (IValueView) view;

            Assert.AreEqual(offset, view.Offset);
            Assert.AreEqual(value, valueView.Value);
        }

        public static void VerifyByteBlob(this IView view, int offset, byte[] value)
        {
            Assert.IsInstanceOfType(view, typeof(ByteBlobView));

            var byteView = (ByteBlobView) view;

            Assert.AreEqual(offset, view.Offset);
            Assert.AreEqual(value.Length, byteView.Bytes.Length);

            for (var i = 0; i < value.Length; i++)
                Assert.AreEqual(value[i], byteView.Bytes[i]);
        }
    }
}
