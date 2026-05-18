using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.Ecma335;

namespace PESpy.Tests
{
    struct MarshalStruct
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Name;
    }

    //For testing marshalling
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibraryW(
            [MarshalAs(UnmanagedType.LPWStr), ComAliasName("foo")] string lpLibFileName);
    }

    [TestClass]
    public class EcmaTests
    {
        /* The following entities allow for custom attributes per ECMA-335, however did not actually have
         * any in any loaded modules in a stress test, so we won't have tests for these items
         * - AssemblyRef
         * - ExportedType
         * - File
         * - ManifestResource
         * - MemberRef
         * - MethodSpec
         * - ModuleRef
         * - StandAloneSig
         * - TypeSpec
         * 
         * TypeDef also has DeclSecurityAttributes but I couldn't find any types with these
         */

        #region AssemblyRow

        [TestMethod]
        public void Ecma335_AssemblyRow_CustomAttributes()
        {
            Test(heap =>
            {
                var row = heap.AssemblyTable[0];

                var customAttribs = row.CustomAttributes;

                var attrib = customAttribs["System.Reflection.AssemblyTitleAttribute"].DecodeValue();

                Assert.AreEqual(attrib.FixedArgs[0].Value, "PESpy.Tests");
            });
        }

        [TestMethod]
        public void Ecma335_AssemblyRow_DeclSecurityAttributes()
        {
            Test(heap =>
            {
                var row = heap.AssemblyTable[0];

                var attribs = row.DeclSecurityAttributes;

                Assert.AreEqual(1, attribs.Count);
            });
        }

        #endregion
        #region EventRow

        [TestMethod]
        public void Ecma335_EventRow_DeclaringType()
        {
            Test(heap =>
            {
                var declaringType = heap.EventTable[0].DeclaringType;

                Assert.AreEqual("PESpy.Tests.Dummy`1", declaringType.ToString());
            });
        }

        [TestMethod]
        public void Ecma335_EventRow_Accessors()
        {
            Test(heap =>
            {
                var accessors = heap.EventTable[0].Accessors;

                var add = heap.MethodDefTable[accessors.Adder];
                var remove = heap.MethodDefTable[accessors.Remover];

                Assert.AreEqual("PESpy.Tests.Dummy`1.add_Handler", add.ToString());
                Assert.AreEqual("PESpy.Tests.Dummy`1.remove_Handler", remove.ToString());
            });
        }

        [TestMethod]
        public void Ecma335_EventRow_CustomAttributes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var @event = type.Events[0];
                Assert.AreEqual(1, @event.CustomAttributes.Count);
            });
        }

        #endregion
        #region FieldRow

        [TestMethod]
        public void Ecma335_FieldRow_DecodeSignature()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var field = type.Fields[0];
                var sig = field.DecodeSignature(StringSignatureTypeProvider.Instance, default);

                Assert.AreEqual("System.EventHandler", sig);
            });
        }

        [TestMethod]
        public void Ecma335_FieldRow_DeclaringType()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var field = type.Fields[0];
                Assert.AreEqual(type.RowIndex, field.DeclaringType.Value.RowIndex);
            });
        }

        [TestMethod]
        public void Ecma335_FieldRow_DefaultValue()
        {
            Test(heap =>
            {
                //Enum members will be listed here
                var row = heap.FieldTable.First(v => v.Name.GetString() == "ByteBlob");
                Assert.AreEqual(3, (int) row.DefaultValueRow.Value.ClrValue);
            });
        }

        //We're getting a bunch of fields in <PrivateImplementationDetails> but I don't know how to generate these myself
        //Ecma335_FieldRow_RelativeVirtualAddress

        [TestMethod]
        public void Ecma335_FieldRow_MarshallingDescriptor()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.MarshalStruct"];

                var field = type.Fields["Name"];
                Assert.IsFalse(field.MarshallingDescriptor.IsNil);
            });
        }

        [TestMethod]
        public void Ecma335_FieldRow_CustomAttributes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var field = type.Fields["<Property>k__BackingField"];

                Assert.AreEqual(2, field.CustomAttributes.Count);
            });
        }

        #endregion
        #region GenericParamRow

        [TestMethod]
        public void Ecma335_GenericParamRow_Constraints()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];

                var genericParam = type.GenericParameters["T"];
                Assert.AreEqual(1, genericParam.Constraints.Count);
            });
        }

        [TestMethod]
        public void Ecma335_GenericParamRow_CustomAttributes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];

                var genericParam = type.GenericParameters["T"];
                Assert.AreEqual(1, genericParam.CustomAttributes.Count);
            });
        }

        [TestMethod]
        public void Ecma335_GenericParamConstraintRow_CustomAttributes()
        {
            //NullableAttribute can apply itself to your constraint

            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.FindKindViewWriter"];
                var method = type.Methods["NewStruct"];

                var genericParam = method.GenericParameters[0];
                var constraint = genericParam.Constraints[0];
                Assert.AreEqual(1, constraint.CustomAttributes.Count);
            });
        }

        #endregion
        #region InterfaceImplRow

        //You can get nullable attributes on your interface implementation when there's a NullableAttribute
        //somehow, whcih doesn't really make sense to me but that's what my stress test showed. I'm now sure
        //how to get one outside of mscorlib
        //Ecma335_InterfaceImplRow_CustomAttributes

        #endregion
        #region MemberRefRow

        [TestMethod]
        public void Ecma335_MemberRefRow_DecodeFieldSignature()
        {
            Test(heap =>
            {
                var memberRef = heap.MemberRefTable["System.String.Empty"];

                var sig = memberRef.DecodeFieldSignature(StringSignatureTypeProvider.Instance, default);

                Assert.AreEqual("string", sig);
            });
        }

        [TestMethod]
        public void Ecma335_MemberRefRow_DecodeMethodSignature()
        {
            Test(heap =>
            {
                var method = heap.MemberRefTable["System.Diagnostics.DebuggableAttribute..ctor"];

                var sig = method.DecodeMethodSignature(StringSignatureTypeProvider.Instance, default);

                Assert.AreEqual("void M(DebuggingModes)", sig.ToString());
            });
        }

        #endregion
        #region MethodDefRow

        [TestMethod]
        public void Ecma335_MethodDefRow_Parameters()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var method = type.Methods["Foo"];
                Assert.AreEqual(1, method.Parameters.Count);
            });
        }

        [TestMethod]
        public void Ecma335_MethodDefRow_GenericParameters()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var method = type.Methods["Foo"];
                Assert.AreEqual(1, method.GenericParameters.Count);
            });
        }

        [TestMethod]
        public void Ecma335_MethodDefRow_Import()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.NativeMethods"];

                var method = type.Methods["LoadLibraryW"];
                var import = method.Import.Value;

                Assert.AreEqual("kernel32.dll!LoadLibraryW", import.ToString());
            });
        }

        [TestMethod]
        public void Ecma335_MethodDefRow_CustomAttributes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var method = type.Methods["add_Handler"];
                Assert.AreEqual(1, method.CustomAttributes.Count);
            });
        }

        //Couldn't find any methods with decl security attributes for test Ecma335_MethodDefRow_DeclSecurityAttributes

        #endregion
        #region MethodSpecRow

        [TestMethod]
        public void Ecma335_MethodSpecRow_DecodeSignature()
        {
            Test(heap =>
            {
                var methodSpec = heap.MethodSpecTable.First(v => v.MethodRow.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException");

                var sig = methodSpec.DecodeSignature(StringSignatureTypeProvider.Instance, default);

                Assert.AreEqual(1, sig.Length);
                Assert.AreEqual("System.NotImplementedException", sig[0]);
            });
        }

        #endregion
        #region ModuleRow

        [TestMethod]
        public void Ecma335_ModuleRow_CustomAttributes()
        {
            Test(heap =>
            {
                var module = heap.ModuleTable[0];
                Assert.AreEqual(2, module.CustomAttributes.Count);
            });
        }

        #endregion
        #region ParamRow

        [TestMethod]
        public void Ecma335_ParamRow_DefaultValue()
        {
            Test(heap =>
            {
                var row = heap.ParamTable.First(v => v.Name.GetString() == "projectName");
                Assert.AreEqual("PESpy", row.DefaultValueRow.Value.ClrValue.ToString());
            });
        }

        [TestMethod]
        public void Ecma335_ParamRow_MarshallingDescriptor()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.NativeMethods"];

                var method = type.Methods["LoadLibraryW"];
                var param = method.Parameters[0];

                Assert.IsFalse(param.MarshallingDescriptor.IsNil);
            });
        }

        [TestMethod]
        public void Ecma335_ParamRow_CustomAttributes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.NativeMethods"];

                var method = type.Methods["LoadLibraryW"];
                var param = method.Parameters[0];

                //Certain attributes like MarshalAs, In and Out cause additional Flags to be applied and don't
                //actually exist as custom attributes

                Assert.AreEqual(1, param.CustomAttributes.Count);
            });
        }

        #endregion
        #region PropertyRow

        [TestMethod]
        public void Ecma335_PropertyRow_DecodeSignature()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var property = type.Properties[0];
                var sig = property.DecodeSignature(StringSignatureTypeProvider.Instance, default);
                Assert.AreEqual("int M()", sig.ToString());
            });
        }

        //Not sure how to generate Ecma335_PropertyRow_DefaultValue

        [TestMethod]
        public void Ecma335_PropertyRow_CustomAttributes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var property = type.Properties[0];
                Assert.AreEqual(1, property.CustomAttributes.Count);
            });
        }

        [TestMethod]
        public void Ecma335_PropertyRow_DeclaringType()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var property = type.Properties[0];
                Assert.AreEqual(property.DeclaringType.Value.RowIndex, type.RowIndex);
            });
        }

        [TestMethod]
        public void Ecma335_PropertyRow_Accessors()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var property = type.Properties[0];

                var accessors = property.Accessors;
                Assert.AreEqual(0, accessors.Setter.RowId);
                Assert.AreNotEqual(0, accessors.Getter.RowId);

                var getter = heap.MethodDefTable[accessors.Getter];
                Assert.AreEqual("PESpy.Tests.Dummy`1.get_Property", getter.ToString());
            });
        }

        #endregion
        #region StandAloneSigRow

        //This exists in mscorlib but I'm not sure how to repro + test on a specific method
        //Ecma335_StandAloneSigRow_DecodeMethodSignature

        [TestMethod]
        public void Ecma335_StandAloneSigRow_DecodeLocalSignature()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var method = type.Methods["ToString"];
                var sig = method.ILMethod.Value.Sig.Value;
                var types = sig.DecodeLocalSignature(StringSignatureTypeProvider.Instance, default);
                Assert.AreEqual(1, types.Length);
                Assert.AreEqual("string", types[0]);
            });
        }

        #endregion
        #region TypeDefRow

        [TestMethod]
        public void Ecma335_TypeDefRow_CustomAttributes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(7, type.CustomAttributes.Count);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_Layout()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.IsNull(type.Layout);

                var layout = heap.ClassLayoutTable[0];
                type = heap.TypeDefTable[layout.Parent];
                Assert.IsNotNull(layout);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_DeclaringType()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var nestedType = heap.TypeDefTable[type.NestedTypes[0]];
                Assert.AreEqual(type.RowIndex, nestedType.DeclaringType.Value.RowIndex);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_GenericParameters()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.GenericParameters.Count);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_Methods()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(7, type.Methods.Count);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_Fields()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(2, type.Fields.Count);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_Properties()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.Properties.Count);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_Events()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.Events.Count);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_NestedTypes()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.NestedTypes.Length);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_MethodImplementations()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.MethodImplementations.Count);
            });
        }

        [TestMethod]
        public void Ecma335_TypeDefRow_InterfaceImplementations()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.InterfaceImplementations.Count);
            });
        }

        #endregion
        #region TypeSpecRow

        [TestMethod]
        public void Ecma335_TypeSpecRow_DecodeSignature()
        {
            Test(heap =>
            {
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];

                var attrib = type.CustomAttributes[4];
                Assert.AreEqual("PESpy.Tests.GenericArray<int>", attrib.ToString()); //This is a TypeSpec
            });
        }

        #endregion
        #region Lists

        /* System.Reflection.Metadata plays games with its list indices, always returning a start row of 1 and then doing endRow - startRow + 1
         * to calculate the actual number of items in the list. In the event a list is empty, start = 1, end = 0. 0-1 = -1 + 1 = 0. This is all well
         * and good, except if you have a "default" list this will have start = 0 and end = 0 which gives a false count of 0. This is no good, so we need
         * to change System.Reflection.Metadata's behavior to correctly report a length of 0 when a list is empty
         * 
         * For each list, we need to test 3 things
         * 1. A normal list with items in it
         * 2. An empty list given to us by PESpy
         * 3. A "default" list created from initializing a variable to empty
         */

        [TestMethod]
        public void Ecma335_List_CustomAttribute()
        {
            Test(heap =>
            {
                //Full List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(7, type.CustomAttributes.Count);
                var items = type.CustomAttributes.ToArray();
                Assert.AreEqual(7, items.Length);

                //Empty List
                var method = type.Methods["ToString"];
                Assert.AreEqual(0, method.CustomAttributes.Count);
                items = method.CustomAttributes.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                CustomAttributeList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        [TestMethod]
        public void Ecma335_List_DeclSecurityAttribute()
        {
            Test(heap =>
            {
                //Full List
                var assemblyTable = heap.AssemblyTable[0];
                Assert.AreEqual(1, assemblyTable.DeclSecurityAttributes.Count);
                var items = assemblyTable.DeclSecurityAttributes.ToArray();
                Assert.AreEqual(1, items.Length);

                //Empty List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var method = type.Methods[0];
                Assert.AreEqual(0, method.DeclSecurityAttributes.Count);
                items = method.DeclSecurityAttributes.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                DeclSecurityAttributeList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        [TestMethod]
        public void Ecma335_List_Event()
        {
            Test(heap =>
            {
                Test(heap =>
                {
                    //Full List
                    var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                    Assert.AreEqual(1, type.Events.Count);
                    var items = type.Events.ToArray();
                    Assert.AreEqual(1, items.Length);

                    //Empty List
                    type = heap.TypeDefTable[0];
                    Assert.AreEqual(0, type.Events.Count);
                    items = type.Events.ToArray();
                    Assert.AreEqual(0, items.Length);

                    //Default List
                    EventList defaultList = default;
                    Assert.AreEqual(0, defaultList.Count);
                    items = defaultList.ToArray();
                    Assert.AreEqual(0, items.Length);
                });
            });
        }

        [TestMethod]
        public void Ecma335_List_FieldDef()
        {
            Test(heap =>
            {
                //Full List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(2, type.Fields.Count);
                var items = type.Fields.ToArray();
                Assert.AreEqual(2, items.Length);

                //Empty List
                type = heap.TypeDefTable[0];
                Assert.AreEqual(0, type.Fields.Count);
                items = type.Fields.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                FieldDefList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        [TestMethod]
        public void Ecma335_List_GenericParamConstraint()
        {
            Test(heap =>
            {
                Test(heap =>
                {
                    //Full List
                    var type = heap.TypeDefTable["PESpy.Tests.BaseTest"];
                    var method = type.Methods.First(m => m.Name.GetString() == "GetSampleFile" && m.GenericParameters.Count > 0);
                    var genericParameter = method.GenericParameters[0];
                    Assert.AreEqual(1, genericParameter.Constraints.Count);
                    var items = genericParameter.Constraints.ToArray();
                    Assert.AreEqual(1, items.Length);

                    //Empty List
                    genericParameter = heap.GenericParamTable[0];
                    Assert.AreEqual(0, genericParameter.Constraints.Count);
                    items = genericParameter.Constraints.ToArray();
                    Assert.AreEqual(0, items.Length);

                    //Default List
                    GenericParamConstraintList defaultList = default;
                    Assert.AreEqual(0, defaultList.Count);
                    items = defaultList.ToArray();
                    Assert.AreEqual(0, items.Length);
                });
            });
        }

        [TestMethod]
        public void Ecma335_List_GenericParam()
        {
            Test(heap =>
            {
                Test(heap =>
                {
                    //Full List
                    var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                    Assert.AreEqual(1, type.GenericParameters.Count);
                    var items = type.GenericParameters.ToArray();
                    Assert.AreEqual(1, items.Length);

                    //Empty List
                    type = heap.TypeDefTable[0];
                    Assert.AreEqual(0, type.GenericParameters.Count);
                    items = type.GenericParameters.ToArray();
                    Assert.AreEqual(0, items.Length);

                    //Default List
                    GenericParamList defaultList = default;
                    Assert.AreEqual(0, defaultList.Count);
                    items = defaultList.ToArray();
                    Assert.AreEqual(0, items.Length);
                });
            });
        }

        [TestMethod]
        public void Ecma335_List_InterfaceImpl()
        {
            Test(heap =>
            {
                //Full List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.InterfaceImplementations.Count);
                var items = type.InterfaceImplementations.ToArray();
                Assert.AreEqual(1, items.Length);

                //Empty List
                type = heap.TypeDefTable[0];
                Assert.AreEqual(0, type.InterfaceImplementations.Count);
                items = type.InterfaceImplementations.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                InterfaceImplList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        [TestMethod]
        public void Ecma335_List_MethodDef()
        {
            Test(heap =>
            {
                //Full List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(7, type.Methods.Count);
                var items = type.Methods.ToArray();
                Assert.AreEqual(7, items.Length);

                //Empty List
                type = heap.TypeDefTable[0];
                Assert.AreEqual(0, type.Methods.Count);
                items = type.Methods.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                MethodDefList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        [TestMethod]
        public void Ecma335_List_MethodImpl()
        {
            //MethodImpl rows seem to be added when an interface method is explicitly implemented. Compiler generated classes seem to explicitly implement
            //all of their interfaces, and so are commonly seen in the MethodImplTable

            Test(heap =>
            {
                //Full List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.MethodImplementations.Count);
                var items = type.MethodImplementations.ToArray();
                Assert.AreEqual(1, items.Length);

                //Empty List
                type = heap.TypeDefTable[0];
                Assert.AreEqual(0, type.MethodImplementations.Count);
                items = type.MethodImplementations.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                MethodImplList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        [TestMethod]
        public void Ecma335_List_Param()
        {
            Test(heap =>
            {
                //Full List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                var method = type.Methods[0];
                Assert.AreEqual(1, method.Parameters.Count);
                var items = method.Parameters.ToArray();
                Assert.AreEqual(1, items.Length);

                //Empty List
                method = type.Methods["ToString"];
                Assert.AreEqual(0, method.Parameters.Count);
                items = method.Parameters.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                ParamList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        [TestMethod]
        public void Ecma335_List_Property()
        {
            Test(heap =>
            {
                //Full List
                var type = heap.TypeDefTable["PESpy.Tests.Dummy`1"];
                Assert.AreEqual(1, type.Properties.Count);
                var items = type.Properties.ToArray();
                Assert.AreEqual(1, items.Length);

                //Empty List
                type = heap.TypeDefTable[0];
                Assert.AreEqual(0, type.Properties.Count);
                items = type.Properties.ToArray();
                Assert.AreEqual(0, items.Length);

                //Default List
                PropertyList defaultList = default;
                Assert.AreEqual(0, defaultList.Count);
                items = defaultList.ToArray();
                Assert.AreEqual(0, items.Length);
            });
        }

        #endregion

        private void Test(Action<ModelHeap> action)
        {
            using var peFile = PEFile.FromFile(GetType().Assembly.Location);

            var heap = peFile.EcmaMetadata.ModelHeap;

            action(heap);
        }

        private unsafe void TestMicrosoft(Action<MetadataReader> action)
        {
            using var peFile = PEFile.FromFile(GetType().Assembly.Location);

            peFile.TryGetRawMetadata(out var metadata, out var length);

            var metadataReader = new MetadataReader(metadata, length);
            action(metadataReader);
        }

        private void StressTest(Action<ModelHeap> action)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (assembly.Location == string.Empty)
                    continue;

                using var peFile = PEFile.FromFile(assembly.Location);

                var heap = peFile.EcmaMetadata.ModelHeap;

                action(heap);
            }
        }
    }
}
