using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    #region Test Types

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class GenericAttribute<T> : Attribute
    {
        public T B { get; set; }

        public GenericAttribute(T a)
        {
        }
    }

    public class GenericAttribute<T1, T2> : Attribute
    {
        public GenericAttribute(T1 a, T2 b)
        {
        }
    }

    public class ArrayAttribute : Attribute
    {
        public ArrayAttribute(params int[] a)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class GenericArray<T> : Attribute
    {
        public GenericArray(params T[] a)
        {
        }
    }

    [TypeConverter(typeof(Dummy<>))] //Should get whatever T we are
    [Generic<object>(1, B = 2)]
    [Generic<string>("test")]
    [Array(1, 2, 3, 4)]
    [GenericArray<int>(1, 2, 3, 4)]
    [GenericArray<string>("a", "b", "c", "d")]
    [Generic<int, object>(1, 2)]
    public class Dummy<[System.ComponentModel.Description("Test")] T> : IDisposable where T : struct
    {
        [System.ComponentModel.Description("Test")]
        public event EventHandler Handler;

        [System.ComponentModel.Description("Test")]
        public int Property { get; } = 1;

        public void Foo<T1>(T1 a)
        {
        }

        //Implementing an interface normally does not generate a MethodImpl, but explicitly implementing one does
        void IDisposable.Dispose()
        {
        }

        public override string ToString()
        {
            return "test";
        }

        public struct NestedType
        {
            public int Field;
        }
    }

    #endregion

    [TestClass]
    public class CustomAttributeTests
    {
        [TestMethod]
        public void Ecma335_CustomAttributes_StressTest()
        {
            //Get all custom attributes defined in PESpy.Tests (which will include all attributes on the types defined above)
            //and check we can parse them

            using var peFile = PEFile.FromFile(GetType().Assembly.Location);

            var heap = peFile.EcmaMetadata.CompressedModelHeap;

            var typeDef = heap.TypeDefTable.First(v => v.TypeName.GetString() == "Dummy`1");

            var provider = new ReflectionCustomAttributeTypeProvider();

            foreach (var row in typeDef.CustomAttributes)
            {
                var value = row.DecodeValue(provider);
                _ = row.ToString();
            }
        }
    }
}
