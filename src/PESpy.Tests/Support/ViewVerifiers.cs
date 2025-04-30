using System;
using ClrDebug.PDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.PDB;
using PESpy.View;

namespace PESpy.Tests
{
    static class ViewVerifiers
    {
        public static void VerifyHeader(this IView view, int size, params Action<IView>[] verifyChildren)
        {
            Assert.IsInstanceOfType(view, typeof(HeaderView));

            var headerView = (HeaderView) view;

            Assert.AreEqual(size, headerView.Size, $"Size of Header was incorrect");

            Assert.AreEqual(verifyChildren.Length, headerView.Children.Length, "Number of HeaderView children was incorrect");

            for (var i = 0; i < headerView.Children.Length; i++)
                verifyChildren[i](headerView.Children[i]);
        }

        public static void VerifySection(this IView view, string name, int offset, int size, params Action<IView>[] verifyChildren)
        {
            Assert.IsInstanceOfType(view, typeof(SectionView));

            var sectionView = (SectionView) view;

            Assert.AreEqual(name, sectionView.Header.Name.ToString());
            Assert.AreEqual(offset, sectionView.Offset, $"Offset of {name} was incorrect. Also size is {sectionView.Size}");
            Assert.AreEqual(size, sectionView.Size, $"Size of {name} was incorrect");

            Assert.AreEqual(verifyChildren.Length, sectionView.Children.Length, "Number of SectionView children was incorrect");

            for (var i = 0; i < sectionView.Children.Length; i++)
                verifyChildren[i](sectionView.Children[i]);
        }

        public static void VerifySectionIgnoreChildren(this IView view, string name, int offset, int size)
        {
            Assert.IsInstanceOfType(view, typeof(SectionView));

            var sectionView = (SectionView) view;

            Assert.AreEqual(name, sectionView.Header.Name.ToString());
            Assert.AreEqual(offset, sectionView.Offset, $"Offset of {name} was incorrect. Also size is {sectionView.Size}");
            Assert.AreEqual(size, sectionView.Size, $"Size of {name} was incorrect");
        }

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

        public static void VerifyStructField(this IView view, string name, string type, int offset, int size, params Action<IView>[] verifyChildren)
        {
            Assert.IsInstanceOfType(view, typeof(IFieldView), $"{name} should not be a field");

            var fieldView = (IFieldView) view;

            VerifyStruct((IView) fieldView.Value, type, offset, size, verifyChildren);
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

            var actualValue = valueView.Value;

            if (actualValue is AnsiString s)
                actualValue = s.ToString();
            else if (actualValue is Utf8String u)
                actualValue = u.ToString();
            else if (actualValue is FixedUtf8String fu)
                actualValue = fu.ToString();

            Assert.AreEqual(offset, view.Offset);
            Assert.AreEqual(value, actualValue);
        }

        public static void VerifyValueIgnoreValue(this IView view, int offset)
        {
            Assert.IsInstanceOfType(view, typeof(IValueView));

            Assert.AreEqual(offset, view.Offset);
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

        public static void VerifyLogicalRegion(this IView view, string name, int offset, int size, params Action<IView>[] verifyChildren)
        {
            Assert.IsInstanceOfType(view, typeof(LogicalRegionView));

            var logicalRegion = (LogicalRegionView) view;

            Assert.AreEqual(name, logicalRegion.Name, "Name was incorrect");
            Assert.AreEqual(offset, logicalRegion.Offset, $"Offset of {name} was incorrect. Also size is {logicalRegion.Size}");
            Assert.AreEqual(size, logicalRegion.Size, $"Size of {name} was incorrect");

            Assert.AreEqual(verifyChildren.Length, logicalRegion.Children.Length, "Number of LogicalRegionView children was incorrect");

            for (var i = 0; i < logicalRegion.Children.Length; i++)
                verifyChildren[i](logicalRegion.Children[i]);
        }

        public static void VerifyLogicalRegionIgnoreChildren(this IView view, string name, int offset, int size)
        {
            Assert.IsInstanceOfType(view, typeof(LogicalRegionView));

            var logicalRegion = (LogicalRegionView) view;

            Assert.AreEqual(name, logicalRegion.Name, "Name was incorrect");
            Assert.AreEqual(offset, logicalRegion.Offset, $"Offset of {name} was incorrect. Also size is {logicalRegion.Size}");
            Assert.AreEqual(size, logicalRegion.Size, $"Size of {name} was incorrect");
        }

        public static void VerifySymType(this IView view, int offset, int size, SYM_ENUM_e type)
        {
            Assert.IsInstanceOfType(view, typeof(ValueView<SymType>));

            var valueView = (ValueView<SymType>) view;

            Assert.AreEqual(offset, valueView.Offset, $"Offset of {valueView.Value} was incorrect. Also size is {view.Size}");
            Assert.AreEqual(size, valueView.Size, $"Size of {valueView.Value} was incorrect");
            Assert.AreEqual(type, valueView.Value.rectyp);
        }

        public static void VerifyTypType(this IView view, int offset, int size, LEAF_ENUM_e type)
        {
            Assert.IsInstanceOfType(view, typeof(ValueView<TypType>));

            var valueView = (ValueView<TypType>) view;

            Assert.AreEqual(offset, valueView.Offset, $"Offset of {valueView.Value} was incorrect. Also size is {view.Size}");
            Assert.AreEqual(size, valueView.Size, $"Size of {valueView.Value} was incorrect");
            Assert.AreEqual(type, valueView.Value.leaf);
        }
    }
}
