using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.Ecma335;
using PESpy.LIB;
using PESpy.PDB;
using PESpy.PowerShell;
using PESpy.View;
using static PESpy.IMAGE_DEBUG_TYPE;

namespace PESpy.Tests
{
    public abstract class BaseTest
    {
        #region TestStruct

        protected void TestStruct<T>(params Expression<Func<T, bool>>[] asserts) =>
            TestStruct<T, T>(asserts);

        protected void TestStruct<TSelector, TVerifier>(params Expression<Func<TVerifier, bool>>[] asserts)
        {
            Stream fs = null;
            object rawValue;

            rawValue = GetStruct<TSelector, TVerifier>(out fs);

            using var fs1 = fs;

            var results = new List<string>();

            var propertiesAndFieldsTouched = new List<MemberInfo>();

            bool IsEqual(object first, object second)
            {
                if (IgnoreValue.Equals(first))
                    return true;

                if (first == null)
                {
                    if (second == null || second.Equals(null))
                        return true;

                    return false;
                }
                else
                {
                    //First is not null

                    if (second == null)
                        return false;
                }

                if (first.Equals(second) || second.Equals(first)) //PCSTR -> string
                    return true;

                var firstType = first.GetType();
                var secondType = second.GetType();

                if (firstType.IsArray && secondType.IsArray)
                {
                    var firstArr = (Array) first;
                    var secondArr = (Array) second;

                    if (firstArr.Length != secondArr.Length)
                        return false;

                    for (var i = 0; i < firstArr.Length; i++)
                    {
                        var v1 = firstArr.GetValue(i);
                        var v2 = secondArr.GetValue(i);

                        if (v1 is ProdItem p1)
                        {
                            var p2 = (ProdItem) v2;

                            if (!(p1.BuildId == p2.BuildId && p1.Count == p2.Count && p1.ProdId == p2.ProdId && p1.ProductId == p2.ProductId && p1.VisualStudioVersion == p2.VisualStudioVersion))
                                return false;

                            continue;
                        }

                        if (!v1.Equals(v2))
                            return false;
                    }

                    return true;
                }

                return false;
            }

            foreach (var assert in asserts)
            {
                var local = rawValue;
                var body = (BinaryExpression) assert.Body;

                MemberInfo memberInfo;
                Type memberType;
                object actual;

                if (body.Left is MethodCallExpression c)
                {
                    if (c.Method.Name == "GetSpan")
                    {
                        var propertyName = ((ConstantExpression) c.Arguments[1]).Value.ToString();
                        memberInfo = rawValue.GetType().GetProperty(propertyName);
                        memberType = ((PropertyInfo) memberInfo).PropertyType;

                        var lambda = Expression.Lambda(c, assert.Parameters).Compile();

                        actual = lambda.DynamicInvoke(rawValue);
                    }
                    else if (c.Method.Name == "ToString")
                    {
                        memberInfo = GetPropertyInfo(c.Object, ref local);
                        memberType = ((PropertyInfo) memberInfo).PropertyType;

                        var lambda = Expression.Lambda(c, assert.Parameters).Compile();

                        actual = lambda.DynamicInvoke(rawValue);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    memberInfo = GetPropertyInfo(body.Left, ref local);

                    if (memberInfo is PropertyInfo p)
                    {
                        actual = p.GetValue(local);
                        memberType = p.PropertyType;
                    }
                    else
                    {
                        var f = (FieldInfo) memberInfo;

                        actual = f.GetValue(local);
                        memberType = f.FieldType;
                    }
                }

                //If a field is of type "object" but can store a value of type "enum", an expression that compares against an enum will be Convert Int32 -> Convert Enum -> object property == Int32.
                //We double unwrap the converts to get to the underlying property, but we need to convert the right hand side from Int32 to enum if the inner-most convert was an enum type

                if (memberType == typeof(object))
                {
                    if (body.Left is UnaryExpression { NodeType: ExpressionType.Convert } c1 && c1.Operand is UnaryExpression { NodeType: ExpressionType.Convert } c2 && c2.Type.IsEnum)
                        memberType = c2.Type;
                    else if (body.Left is UnaryExpression { NodeType: ExpressionType.Convert } cc1 && cc1.Operand is MemberExpression mm1 && mm1.Type.IsEnum)
                        memberType = mm1.Type;
                }

                var expectedValue = GetConstantValue(body.Right, memberType);

                if (!IsEqual(expectedValue, actual))
                {
                    static Expression Unwrap(Expression e)
                    {
                        while (e.NodeType == ExpressionType.Convert)
                            e = ((UnaryExpression) e).Operand;

                        return e;
                    }

                    results.Add($"[{Unwrap(body.Left).ToString().Substring(2)}] Expected: {expectedValue} ({expectedValue?.GetType().Name ?? "null"}), Actual: {actual} ({actual?.GetType().Name ?? "null"})");
                }

                propertiesAndFieldsTouched.Add(memberInfo);
            }

            if (results.Count > 0)
                Assert.Fail(string.Join(Environment.NewLine + Environment.NewLine, results));

            var actualPropertiesAndFields = typeof(TVerifier).GetProperties(BindingFlags.Instance | BindingFlags.Public).Cast<MemberInfo>().Concat(typeof(TVerifier).GetFields(BindingFlags.Instance | BindingFlags.Public));
            var missingPropertiesandFields = actualPropertiesAndFields.Except(propertiesAndFieldsTouched).ToArray();

            //todo: assert that we tested all properties
        }

        #endregion
        #region TestView

        protected void TestView<T>(params Action<IView>[] verify) => TestView<T>(verify, true);

        protected void TestView<TSelector, TVerifier>(params Action<IView>[] verify) =>
            TestView<TSelector, TVerifier>(verify, true);

        protected void TestView<T>(Action<IView>[] verify, bool assertChildCount) =>
            TestView<T, T>(verify, assertChildCount);

        protected unsafe void TestView<TSelector, TVerifier>(Action<IView>[] verify, bool assertChildCount)
        {
            if (verify == null)
                throw new ArgumentNullException(nameof(verify));

            Stream stream = null;

            try
            {
                var rawValue = GetStruct<TSelector, TVerifier>(out stream);

                stream.Seek(0, SeekOrigin.Begin);
                var peFile = PEFile.FromStream(stream, false);

                var viewWriter = new PEViewWriter(peFile);

                ((IViewable) rawValue).WriteGlobals(viewWriter);

                var result = ((IViewable) rawValue).WriteStruct(viewWriter);

                Assert.IsNotNull(result, "A StructView must be written");

                if (result is IContainerView c)
                {
                    //All globals should be written in WriteGlobals. If the ViewWriter current count was modified after a call
                    //to Children, this means a global was erroneously written in Children instead of WriteGlobals
                    var preWriteCount = viewWriter.Current.Count;

                    _ = c.Children;

                    var postWriteCount = viewWriter.Current.Count;

                    Assert.AreEqual(preWriteCount, postWriteCount, "Globals were erroneously written inside Children");
                }

                //Combine the struct + any globals into one big list
                var current = viewWriter.Current.Concat(new[] { result }).OrderBy(v => v.Offset).ToArray();

                if (assertChildCount)
                    Assert.AreEqual(verify.Length, current.Length, "Number of views was different from expected");
                else
                    Assert.IsFalse(verify.Length > current.Length, $"Must have at most {current.Length} verifiers");

                var visitor = new ViewAlignmentVerifier();

                for (var i = 0; i < verify.Length; i++)
                {
                    verify[i](current[i]);

                    //Are all of the children properly aligned?
                    current[i].Accept(visitor);
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        #endregion
        #region TestBytes

        protected void TestBytes(string fieldName, byte[] expected)
        {
            Stream fs;

#pragma warning disable CS8509
            ByteBlob rawValue = fieldName switch
#pragma warning restore CS8509
            {
                nameof(PEFile.DosStub) => GetFile(WellKnownTestModule.ntdll, out fs).DosStub
            };

            try
            {
                var expectedStr = string.Join(", ", expected.Select(v => "0x" + v.ToString("X2")));
                var actualStr = string.Join(", ", rawValue.Bytes.Select(v => "0x" + v.ToString("X2")));

                Assert.AreEqual(expectedStr, actualStr);
            }
            finally
            {
                fs.Dispose();
            }
        }

        #endregion
        #region TestXRefs

        //Assert that we have the same number of verifiers as we do properties that implement IRVA or IVA,
        //then assert that the number of xrefs actually written matches that count as well, and then finally
        //assert that the values of each XRef matches the expected source and destination passed in from the caller

        internal void TestXRefs<T>(
            params Action<XRefVerifier>[] actions)
        {
            var verifier = new XRefVerifier(typeof(T).Name);

            bool ShouldExclude(PropertyInfo propertyInfo)
            {
                if (propertyInfo.DeclaringType == typeof(ImageDelayLoadDescriptor))
                    return propertyInfo.Name == nameof(ImageDelayLoadDescriptor.UnloadInformationTable); //I don't think this is present in unloaded modules

                if (propertyInfo.DeclaringType == typeof(ImageLoadConfigDirectory))
                    return propertyInfo.Name == nameof(ImageLoadConfigDirectory.LockPrefixTable); //Haven't been able to find anything with LockPrefixTable

                return false;
            }

            ((IViewable) rawValue).WriteGlobals(viewWriter);

            Debug.Assert(xrefProperties.Length > 0);

                    if (value is IRVA r)
                        return r.IsValid;

                    return ((IVA) value).IsValid;
                })
                .ToArray();

            var xrefs = viewWriter.XRefs;

            Assert.AreEqual(xrefs.Count, xrefProperties.Length);
            Assert.AreEqual(verifiers.Length, xrefProperties.Length);
        }

        #endregion
        #region Setup

        //TSelector is almost always the same as TVerifier; an example of where it's not is ImageDllCharacteristicsEx,
        //which returns its whole entire ImageDebugDirectory to be verified
        protected TVerifier GetStruct<TSelector, TVerifier>(out Stream fs)
        {
            var t = typeof(TSelector);

            string name;

            if (t.IsGenericType)
                name = t.GetGenericArguments()[0].Name;
            else
            {
                name = typeof(TSelector).Name;

                if (t.DeclaringType != null)
                    name = t.DeclaringType.Name + "." + name;
            }

            object rawValue = name switch
            {
                #region DOS Header

                nameof(ImageDosHeader) => (object) GetFile(WellKnownTestModule.ntdll, out fs).DosHeader,

                #endregion
                #region Rich Header

                nameof(RichHeader)          => (object) GetFile(WellKnownTestModule.ntdll, out fs, out file).RichHeader,
                nameof(ProdItem)            => (object) GetFile(WellKnownTestModule.ntdll, out fs, out file).RichHeader.Items[1], //The first item is empty

                #endregion
                #region NT Headers

                nameof(ImageNtHeaders)      => GetFile(WellKnownTestModule.ntdll, out fs, out file).NtHeaders,
                nameof(ImageFileHeader)     => GetFile(WellKnownTestModule.ntdll, out fs, out file).NtHeaders.FileHeader,
                nameof(ImageOptionalHeader) => GetFile(WellKnownTestModule.ntdll, out fs, out file).OptionalHeader,
                nameof(ImageDataDirectory)  => GetFile(WellKnownTestModule.ntdll, out fs, out file).OptionalHeader.ExportTableDirectory,

                #endregion
                #region Section Headers

                nameof(ImageSectionHeader) => GetFile(WellKnownTestModule.ntdll, out fs, out file).SectionHeaders[0],

                #endregion
                #region Export Table (0)

                nameof(ImageExportDirectory) => GetFile(WellKnownTestModule.kernel32, out fs, out file).ExportTable,

                #endregion
                #region Import Table (1)

                nameof(ImageImportDescriptor) => GetFile(WellKnownTestModule.AzureAttest, out fs, out file).ImportTable[0],
                nameof(ImageImportByName)     => GetFile(WellKnownTestModule.AzureAttest, out fs, out file).ImportTable[1].OriginalFirstThunk.Value[0].Name.Value,

                #endregion
                #region Resource Directory (2)

                nameof(ImageResourceDirectory)       => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.Entries[0].OffsetToDirectory.Value,
                nameof(ImageResourceDirectoryEntry)  => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.Entries[0],
                nameof(ImageResourceDataEntry)       => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.Entries[2].OffsetToDirectory.Value.Entries[0].OffsetToDirectory.Value.Entries[0].OffsetToData.Value,
                nameof(ImageResourceDirStringU)      => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.Entries[0].NameOrId.NameOffset.Value,

                nameof(ClrDebugResource)             => GetFile(WellKnownTestModule.coreclr, out fs, out file).ResourceDirectory!.EnumerateResources<ClrDebugResource>().First(),

                nameof(VsVersionInfo)                => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First(),
                nameof(VsFixedFileInfo)              => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Value,
                "VsVersionInfo.StringFileInfo"       => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[0],
                "VsVersionInfo.StringTable"          => ((VsVersionInfo.StringFileInfo) GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[0]).Children[0],
                "VsVersionInfo.String"               => ((VsVersionInfo.StringFileInfo) GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[0]).Children[0].Children[0],
                "VsVersionInfo.VarFileInfo"          => GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[1],
                "VsVersionInfo.Var"                  => ((VsVersionInfo.VarFileInfo) GetFile(WellKnownTestModule.ntdll, out fs, out file).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[1]).Children[0],
                nameof(MessageResourceData)          => GetFile(WellKnownTestModule.DbgEng, out fs, out file).ResourceDirectory!.EnumerateResources<MessageResourceData>().First(),
                nameof(MessageResourceBlock)         => GetFile(WellKnownTestModule.DbgEng, out fs, out file).ResourceDirectory!.EnumerateResources<MessageResourceData>().First().Blocks[0],
                nameof(MessageResourceEntry)         => GetFile(WellKnownTestModule.DbgEng, out fs, out file).ResourceDirectory!.EnumerateResources<MessageResourceData>().First().Blocks[0].OffsetToEntries.Value[0],

                #endregion
                #region Exception Table (3)

                nameof(RuntimeFunction)              => GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[0],
                nameof(UnwindInfo)                   => GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[28].UnwindData.Value,
                nameof(ScopeTable)                   => GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[28].UnwindData.Value.ExceptionData,
                "ScopeTable.ScopeRecord"             => ((ScopeTable) GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[28].UnwindData.Value.ExceptionData).Records[0],

                nameof(FuncInfo)                     => ((RVA<FuncInfo>)  GetFile(WellKnownTestModule.AuthExt, out fs, out file).ExceptionTable[74].UnwindData.Value.ExceptionData).Value,
                nameof(TryBlockMapEntry)             => (((RVA<FuncInfo>) GetFile(WellKnownTestModule.AuthExt, out fs, out file).ExceptionTable[74].UnwindData.Value.ExceptionData).Value).TryBlockMap.Value[0],
                nameof(HandlerType)                  => (((RVA<FuncInfo>) GetFile(WellKnownTestModule.AuthExt, out fs, out file).ExceptionTable[74].UnwindData.Value.ExceptionData).Value).TryBlockMap.Value[0].HandlerArray.Value[0],
                nameof(TypeDescriptor)               => ((RVA<FuncInfo>) GetFile(WellKnownTestModule._7z, out fs, out file).ExceptionTable[500].UnwindData.Value.ExceptionData).Value.TryBlockMap.Value[0].HandlerArray.Value[0].Type.Value,
                nameof(UnwindMapEntry)               => (((RVA<FuncInfo>) GetFile(WellKnownTestModule.AuthExt, out fs, out file).ExceptionTable[74].UnwindData.Value.ExceptionData).Value).UnwindMap.Value[0],
                nameof(IptoStateMapEntry)            => (((RVA<FuncInfo>) GetFile(WellKnownTestModule.AuthExt, out fs, out file).ExceptionTable[74].UnwindData.Value.ExceptionData).Value).IPToStateMap.Value[0],

                nameof(FuncInfoV1)                   => ((RVA<FuncInfoV1>) GetFile(WellKnownTestModule._7z, out fs).ExceptionTable[96].UnwindData.Value.ExceptionData).Value,

                "UnwindCode.PushNonVolatile"         => (UnwindCode.PushNonVolatile)    GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[0].UnwindData.Value.UnwindCode[1],
                "UnwindCode.AllocLarge"              => (UnwindCode.AllocLarge)         GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[0].UnwindData.Value.UnwindCode[0],
                "UnwindCode.AllocSmall"              => (UnwindCode.AllocSmall)         GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[2].UnwindData.Value.UnwindCode[0],
                "UnwindCode.SetFpReg"                => (UnwindCode.SetFpReg)           GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[291].UnwindData.Value.UnwindCode[3],
                "UnwindCode.SaveNonVolatile"         => (UnwindCode.SaveNonVolatile)    GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[1].UnwindData.Value.UnwindCode[0],
                //"UnwindCode.SaveNonVolatileFar"      => (UnwindCode.SaveNonVolatileFar) GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[].UnwindData.Value.UnwindCode[],
                "UnwindCode.Epilog"                  => (UnwindCode.Epilog)             GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[1800].UnwindData.Value.UnwindCode[0],
                "UnwindCode.SaveXmm128"              => (UnwindCode.SaveXmm128)         GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[948].UnwindData.Value.UnwindCode[0],
                //"UnwindCode.SaveXmm128Far"           => (UnwindCode.SaveXmm128Far)      GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[].UnwindData.Value.UnwindCode[],
                "UnwindCode.PushMachFrame"           => (UnwindCode.PushMachFrame)      GetFile(WellKnownTestModule.ntdll, out fs, out file).ExceptionTable[2604].UnwindData.Value.UnwindCode[1],

                #endregion
                #region Security Table (4)

                nameof(WinCertificate)               => GetFile(WellKnownTestModule.ntdll, out fs, out file).SecurityTable[0],
                nameof(SignedData)                   => GetFile(WellKnownTestModule.ntdll, out fs, out file).SecurityTable[0].Certificate,

                #endregion
                #region Base Relocation Table (5)

                nameof(ImageBaseRelocation) => GetFile(WellKnownTestModule.ntdll, out fs, out file).BaseRelocationTable?[6],

                #endregion
                #region Debug Table (6)

                nameof(ImageDebugDirectory)       => GetFile(WellKnownTestModule.ntdll, out fs, out file).DebugTable?[0],
                nameof(ImageCoffSymbolsHeader)    => (ImageCoffSymbolsHeader)     GetSampleFile(Sample.VC60_Coff_EXE, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_COFF).Data,
                nameof(CoffSymbolTable)           => (CoffSymbolTable)            GetSampleFile(Sample.VC60_Coff_EXE, out fs, out file).FileHeader.PointerToSymbolTable.Value,
                nameof(ImageSymbol)               => (ImageSymbol)                GetSampleFile(Sample.VC60_Coff_EXE, out fs, out file).FileHeader.PointerToSymbolTable.Value.Symbols[0],
                nameof(ImageAuxSymbol)            => (ImageAuxSymbol)             GetSampleFile(Sample.VC60_Coff_EXE, out fs, out file).FileHeader.PointerToSymbolTable.Value.Symbols[1].AuxSymbols[0],
                nameof(RSDSI)                     => (RSDSI)                      GetFile(WellKnownTestModule.ntdll, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_CODEVIEW).Data,
                nameof(NB10I)                     => (NB10I)                      GetFile(WellKnownTestModule.crtdll, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_CODEVIEW).Data,
                nameof(FpoData)                   => ((FpoData[])                 GetFile(WellKnownTestModule.ctl3d32, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_FPO).Data)?[0],
                nameof(ImageDebugMisc)            => (ImageDebugMisc)             GetFile(WellKnownTestModule.mfc40, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_MISC).Data,
                nameof(VCFeature)                 => (VCFeature)                  GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_VC_FEATURE).Data,
                nameof(PogoData)                  => (PogoData)                   GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_POGO).Data,
                nameof(PogoItem)                  => (PogoItem)                   ((PogoData) GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_POGO).Data).Entries[0],
                nameof(Reproducible)              => (Reproducible?)              GetFile(WellKnownTestModule.ntdll, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_REPRO).Data,
                nameof(EmbeddedPortablePdb)       => (EmbeddedPortablePdb?)       GetSampleFile(Sample.MPDB_DLL, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB).Data, //todo: need a test module that has an embedded portable pdb
                nameof(PdbChecksum)               => (PdbChecksum?)               GetSampleFile(Sample.R2R_DLL, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_PDB_CHECKSUM).Data, //todo: need a test module that has a pdb checksum
                nameof(IMAGE_DLLCHARACTERISTICS_EX) => (GetFile(WellKnownTestModule.ntdll, out fs, out file).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS)),

                #region NB05

                nameof(NB05Data)                  => (NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data,
                nameof(OMFDirHeader)              => ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirHeader,
                nameof(OMFDirEntry)               => ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[0],
                nameof(OMFModule)                 => (OMFModule) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[0].Data,
                nameof(OMFSegDesc)                => (OMFSegDesc) ((OMFModule) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[0].Data).SegInfo[0],
                nameof(OMFModuleSymbols)          => (OMFModuleSymbols) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[115].Data,
                nameof(OMFSourceModule)           => (OMFSourceModule) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[116].Data,
                nameof(OMFSourceFile)             => ((OMFSourceModule) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[116].Data).baseSrcFile[0],
                nameof(OMFSourceLine)             => ((OMFSourceModule) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[116].Data).baseSrcFile[0].baseSrcLn[0],
                nameof(OMFHashedSymbols)          => (OMFHashedSymbols) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[286].Data,
                nameof(OMFSymHash)                => (OMFSymHash) ((OMFHashedSymbols) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[286].Data).Hash,
                nameof(OMFGlobalTypes)            => (OMFGlobalTypes) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[290].Data,
                nameof(OMFFileIndex)              => (OMFFileIndex) ((NB05Data) GetSampleFile(Sample.VC50_EXE, out fs, out file).DebugTable[2].Data).DirEntries[292].Data,

                #endregion
                #endregion
                #region Copyright Table (7)
                #endregion
                #region Global Pointer Table (8)
                #endregion
                #region Thread Local Storage Table (9)

                nameof(ImageTlsDirectory) => GetFile(WellKnownTestModule.AzureAttest, out fs, out file).TlsDirectory,

                #endregion
                #region Load Config Table (10)

                nameof(ImageLoadConfigDirectory)       => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable,
                nameof(ImageLoadConfigCodeIntegrity)   => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable?.CodeIntegrity,

                nameof(ImageEnclaveConfig)             => GetFile(WellKnownTestModule.AzureAttest, out fs, out file).LoadConfigTable!.EnclaveConfigurationPointer.Value,
                nameof(ImageEnclaveImport)             => GetFile(WellKnownTestModule.AzureAttest, out fs, out file).LoadConfigTable!.EnclaveConfigurationPointer.Value.ImportList.Value[0],

                nameof(GuardAddressTakenIatEntryTable) => GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).LoadConfigTable?.GuardAddressTakenIatEntryTable.Value,
                "GuardAddressTakenIatEntryTable.Entry" => GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).LoadConfigTable?.GuardAddressTakenIatEntryTable.Value[0],
                nameof(GuardCFFunctionTable)           => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable?.GuardCFFunctionTable.Value,
                "GuardCFFunctionTable.Entry"           => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable?.GuardCFFunctionTable.Value[0],
                nameof(GuardEHContinuationTable)       => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable?.GuardEHContinuationTable.Value,
                "GuardEHContinuationTable.Entry"       => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable?.GuardEHContinuationTable.Value[0],
                nameof(GuardLongJumpTargetTable)       => GetFile(WellKnownTestModule.ntoskrnl, out fs, out file).LoadConfigTable?.GuardLongJumpTargetTable.Value,
                "GuardLongJumpTargetTable.Entry"       => GetFile(WellKnownTestModule.ntoskrnl, out fs, out file).LoadConfigTable?.GuardLongJumpTargetTable.Value[0],

                nameof(ImageDynamicRelocationTable)    => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable?.DynamicValueRelocTableOffset.Value,
                nameof(ImageDynamicRelocation)         => GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable?.DynamicValueRelocTableOffset.Value.DynamicRelocations[0],
                //ImageDynamicRelocationV2

                #region Symbol 1

                //ImagePrologueDynamicRelocationHeader

                #endregion
                #region Symbol 2

                //ImageEpilogueDynamicRelocationHeader

                #endregion
                #region Symbol 3

                nameof(ImageImportControlTransferDynamicRelocation) => ((ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>[]) GetFile(WellKnownTestModule.cdd, out fs, out file).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data)[0].Entries[0],

                #endregion
                #region Symbol 4

                nameof(ImageIndirControlTransferDynamicRelocation) => ((ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>[]) GetFile(WellKnownTestModule.kdstub, out fs, out file).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data)[0].Entries[0],

                #endregion
                #region Symbol 5

                nameof(ImageSwitchTableBranchDynamicRelocation)    => (((ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>[]) GetFile(WellKnownTestModule.kd_02_15b3, out fs, out file).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[1].Data)[0]).Entries[0],

                #endregion
                #region Symbol 7

                nameof(ImageFunctionOverrideHeader)            => (ImageFunctionOverrideHeader)  GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data,
                nameof(ImageFunctionOverrideDynamicRelocation) => ((ImageFunctionOverrideHeader) GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data).FuncOverrides[0],
                nameof(ImageBDDInfo)                           => ((ImageFunctionOverrideHeader) GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data).BDDInfo,
                nameof(ImageBDDDynamicRelocation)              => ((ImageFunctionOverrideHeader) GetFile(WellKnownTestModule.ntdll, out fs, out file).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data).BDDInfo.BDDNodes[0],

                #endregion
                #endregion
                #region Bound Import Table (11)

                nameof(ImageBoundImportDescriptor) => GetFile(WellKnownTestModule.mfc40u, out fs, out file).BoundImportTable?[0],
                nameof(ImageBoundForwarderRef)     => GetFile(WellKnownTestModule.mfc40u, out fs, out file).BoundImportTable?[0].Refs[0],

                #endregion
                #region Import Address Table (12)
                #endregion
                #region Delay Import Table (13)

                nameof(ImageDelayLoadDescriptor) => GetFile(WellKnownTestModule.coreclr, out fs, out file).DelayImportTable[0],

                #endregion
                #region Cor Header (14)

                nameof(ImageCor20Header)         => GetFile(WellKnownTestModule.mscorlib, out fs, out file).Cor20Header,
                nameof(StorageSignature)         => GetFile(WellKnownTestModule.mscorlib, out fs, out file).EcmaMetadata.Signature,
                nameof(StorageHeader)            => GetFile(WellKnownTestModule.mscorlib, out fs, out file).EcmaMetadata.Header,
                nameof(ImageCorILMethod)         => GetFile(WellKnownTestModule.mscorlib, out fs, out file).ILMethods.First(),
                nameof(ImageCorILMethodSectEH)   => GetFile(WellKnownTestModule.mscorlib, out fs, out file).ILMethods[22].EHSections[0],
                nameof(ImageCorILMethodSect)     => GetFile(WellKnownTestModule.mscorlib, out fs, out file).ILMethods[22].EHSections[0].Sect,
                nameof(ImageCorILMethodSectEHClause) => GetFile(WellKnownTestModule.mscorlib, out fs, out file).ILMethods[22].EHSections[0].Clauses[0],
                nameof(StorageStream)            => GetFile(WellKnownTestModule.mscorlib, out fs, out file).EcmaMetadata.Header.StreamHeaders[0],

                nameof(EcmaMetadata)             => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata,
                nameof(CompressedModelHeap)      => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata.CompressedModelHeap,
                nameof(CompressedModelHeader)    => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata.CompressedModelHeap.Header,
                nameof(StringHeap)               => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata.StringHeap,
                nameof(BlobHeap)                 => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata.BlobHeap,
                nameof(GuidHeap)                 => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata.GuidHeap,
                nameof(UserStringHeap)           => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata.UserStringHeap,

                nameof(BlobEntry)                => GetSampleFile(Sample.Framework_EXE, out fs, out file).EcmaMetadata.BlobHeap.GetBlob(1),
                nameof(UserString)               => GetFile(WellKnownTestModule.mscorlib, out fs, out file).EcmaMetadata.UserStringHeap.GetString(1),

                nameof(R2R.ReadyToRunHeader)         => GetSampleFile(Sample.R2R_DLL, out fs, out file).ReadyToRunHeader,
                nameof(R2R.ReadyToRunCoreHeader)     => GetSampleFile(Sample.R2R_DLL, out fs, out file).ReadyToRunHeader.CoreHeader,
                nameof(R2R.ReadyToRunSection)        => GetSampleFile(Sample.R2R_DLL, out fs, out file).ReadyToRunHeader.CoreHeader.Sections[0],

                //0: CompilerIdentifier
                nameof(R2R.ReadyToRunImportSection)  => ((R2R.ReadyToRunImportSection[]) GetSampleFile(Sample.R2R_DLL, out fs, out file).ReadyToRunHeader.CoreHeader.Sections[1].Data)[1],
                //2: RuntimeFunctions
                //3: MethodDefEntryPoints
                //4: DebugInfo
                //5: DelayLoadMethodCallThunks
                //6: AvailableTypes
                //7: InstanceMethodEntryPoints
                //8: ManifestMetadata
                //9: ManifestAssemblyMvids
                //10: CrossModuleInlineInfo

                nameof(RuntimeInfo)              => GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).DotNetRuntimeInfo,

                nameof(AppHostSignature)         => GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature,
                "Bundle.Manifest"                => (GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature).BundleHeaderOffset.Value,
                "Bundle.HeaderFixed"             => (GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature).BundleHeaderOffset.Value.Header,
                "Bundle.HeaderFixedV2"           => (GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature).BundleHeaderOffset.Value.AdditionalContext,
                "Bundle.Location"                => (GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature).BundleHeaderOffset.Value.AdditionalContext.DepsJsonLocation,
                "Bundle.FileEntry"               => (GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature).BundleHeaderOffset.Value.Files[0],
                "Bundle.FileEntryFixed"          => (GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature).BundleHeaderOffset.Value.Files[0].Header,
                nameof(BundleEncodedString)      => (GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).AppHostSignature).BundleHeaderOffset.Value.BundleID,

                nameof(ClrEngineMetrics)         => GetSampleFile(Sample.SingleFileApp_EXE, out fs, out file).ClrEngineMetrics,

                nameof(NativeAOT.DotNetRuntimeDebugHeader) => GetNativeAOTProcessStream(out fs, out file).DotNetRuntimeDebugHeader,
                nameof(NativeAOT.DebugTypeEntry)           => GetNativeAOTProcessStream(out fs, out file).DotNetRuntimeDebugHeader.DebugTypeEntries.Value[0],
                nameof(NativeAOT.GlobalValueEntry)         => GetNativeAOTProcessStream(out fs, out file).DotNetRuntimeDebugHeader.GlobalValueEntries.Value[0],

                nameof(ImageCorVTableFixup)      => GetSampleFile(Sample.Interop_Core_DLL, out fs, out file).Cor20VTableFixups[0],

                #endregion
                #region RTTI

                //TypeDescriptor is RttiTypeDescriptor and is handled above
                nameof(RTTIBaseClassDescriptor)      => GetSymbol(WellKnownTestModule.coreclr, "??_R1", c => new RTTIBaseClassDescriptor(c), out fs, out file),
                //nameof(RTTIBaseClassArray)           => GetSymbol(WellKnownTestModule.coreclr, "??_R2", c => new RTTIBaseClassArray(c), out fs),
                nameof(RTTIBaseClassArray) => GetSymbol(WellKnownTestModule.coreclr, "??_R3", c => new RTTIClassHierarchyDescriptor(c).pBaseClassArray.Value, out fs, out file),
                nameof(RTTIClassHierarchyDescriptor) => GetSymbol(WellKnownTestModule.coreclr, "??_R3", c => new RTTIClassHierarchyDescriptor(c), out fs, out file),
                nameof(RTTICompleteObjectLocator)    => GetSymbol(WellKnownTestModule.coreclr, "??_R4", c => new RTTICompleteObjectLocator(c), out fs, out file),

                #endregion
                #region PDB

                nameof(NMT) => GetSampleFile<PDBFile>(Sample.VS22_PDB, out fs, out file).NameMap,
                nameof(NMTNI) => GetSampleFile<PDBFile>(Sample.VS22_PDB, out fs, out file).PDB.StreamNameTable,

                nameof(DBIHdr)                   => (DBIHdr) GetSampleFile<PDBFile>(Sample.VC40_PDB, out fs, out file).DBI.DbiHdr, //Don't have a PDB for VC50; 40 is the latest we have that is DBIHdr
                nameof(NewDBIHdr)                => (NewDBIHdr) GetSampleFile<PDBFile>(Sample.VC60_PDB, out fs, out file).DBI.DbiHdr,

                nameof(Modi)   => (Modi) GetSampleFile<PDBFile>(Sample.VC40_PDB, out fs, out file).DBI.Modules[0],
                nameof(Modi20) => (Modi20) GetSampleFile<PDBFile>(Sample.VC20_PDB, out fs, out file).DBI.Modules[0],
                nameof(Modi50) => (Modi50) GetSampleFile<PDBFile>(Sample.VC50_PDB, out fs, out file).DBI.Modules[0],
                nameof(Modi60) => (Modi60) GetSampleFile<PDBFile>(Sample.VC60_PDB, out fs, out file).DBI.Modules[0],

                nameof(DbgDataHdr) => GetSampleFile<PDBFile>(Sample.VS22_PDB, out fs, out file).DBI.DbgHdr,

                nameof(CvDebugSSubsectionHeader) => GetSampleFile<PDBFile>(Sample.VS22_PDB, out fs, out file).DBI.Modules[1].C13Lines[0],

                nameof(HDR) => GetSampleFile<PDBFile>(Sample.VC60_PDB, out fs, out file).TPI.Hdr,
                nameof(HDR_16t) => GetSampleFile<PDBFile>(Sample.VC40_PDB, out fs, out file).TPI.Hdr,

                "MsfHdr.StreamTable"    => (MsfHdr.StreamTable) GetSampleFile<PDBFile>(Sample.VC60_PDB, out fs, out file).StreamTable,
                "BigMsfHdr.StreamTable" => (BigMsfHdr.StreamTable) GetSampleFile<PDBFile>(Sample.VS22_PDB, out fs, out file).StreamTable,

                #endregion
                #region LIB

                nameof(ImageArchiveMemberHeader) => GetSampleFile<LIBFile>(Sample.VS22_LIB, out fs, out file).FirstLinkerMember.ArchiveHeader,
                nameof(FirstLinkerMember)        => GetSampleFile<LIBFile>(Sample.VS22_LIB, out fs, out file).FirstLinkerMember,
                nameof(SecondLinkerMember)       => GetSampleFile<LIBFile>(Sample.VS22_LIB, out fs, out file).SecondLinkerMember,
                nameof(ShortImportLibraryMember) => GetSampleFile<LIBFile>(Sample.VS22_LIB, out fs, out file).ImportLibrary[3],
                nameof(LongImportLibraryMember)  => GetSampleFile<LIBFile>(Sample.VS22_LIB, out fs, out file).ImportLibrary[0],

                #endregion
                //_ => throw new NotImplementedException($"Don't know how to handle type '{typeof(T).Name}'")
                _ => throw new AssertInconclusiveException($"Don't know how to handle type '{typeof(TSelector).Name}'")
            };

            if (rawValue == null)
                throw new NotImplementedException();

            return (TVerifier) rawValue;
        }

        protected static PEFile GetFile(SymStoreKey key, out Stream fs, out IFile file)
        {
            var path = Locator.Locate(key);

            fs = File.OpenRead(path);

            var peFile = PEFile.FromStream(fs, false);
            file = peFile;

            return peFile;
        }

        private static PEFile GetSampleFile(string path, out Stream fs, out IFile file)
        {
            fs = File.OpenRead(path);

            var peFile = PEFile.FromStream(fs, false);
            file = peFile;

            return peFile;
        }

        protected static T GetSampleFile<T>(string path, out Stream fs, out IFile file) where T : IFile
        {
            fs = File.OpenRead(path);

            file = Detector.OpenFile(path); ;
            return (T) file;
        }

        protected static T GetSampleFile<T>(string path, out IFile file, out Stream fs) where T : IFile
        {
            fs = File.OpenRead(path);

            file = Detector.OpenFile(path);

            return (T) file;
        }

        private static PEFile GetNativeAOTProcessStream(out Stream stream, out IFile file)
        {
            stream = ProcessHolderStream.New(Sample.NativeAOT_EXE);

            var peFile = PEFile.FromStream(stream, true);
            file = peFile;

            return peFile;
        }

        #endregion
        #region Expression Helpers

        private MemberInfo GetPropertyInfo(Expression expression, ref object rawValue)
        {
            while (expression is UnaryExpression e)
                expression = e.Operand;

            if (expression is not MemberExpression m)
            {
                throw new NotImplementedException();
            }

            var exprs = new List<MemberExpression>();

            var mm = m.Expression;

            while (mm is MemberExpression m2)
            {
                exprs.Add(m2);
                mm = m2.Expression;
            }

            exprs.Reverse();

            foreach (var expr in exprs)
                rawValue = ((PropertyInfo) expr.Member).GetValue(rawValue);

            if (mm is UnaryExpression)
            {
                var unary = GetPropertyInfo(mm, ref rawValue);

                rawValue = ((PropertyInfo) unary).GetValue(rawValue);
            }

            return m.Member;
        }

        private object GetConstantValue(Expression expression, Type type)
        {
            object value;

            if (expression is ConstantExpression c)
                value = c.Value;
            else
            {
                //Compile it
                value = Expression.Lambda(expression).Compile().DynamicInvoke();
            }

            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null)
            {
                //If the expression is null, use null
                if (value == null)
                    return null;

                type = underlying;
            }

            var typeCode = Type.GetTypeCode(type);

            value = typeCode switch
            {
                TypeCode.SByte => Convert.ToSByte(value),
                TypeCode.Byte => Convert.ToByte(value),
                TypeCode.Int16 => Convert.ToInt16(value),
                TypeCode.UInt16 => Convert.ToUInt16(value),
                _ => value
            };

            if (type.IsEnum && value != null)
                value = Enum.Parse(type, value.ToString());

            if (type == typeof(Timestamp) && value is uint u)
                value = (Timestamp) u;

            if (type == typeof(ClrDebug.PDB.CV_typ_t))
                value = (ClrDebug.PDB.CV_typ_t) (int) value;

            if (type == typeof(ClrDebug.PDB.CV_off32_t))
                value = (ClrDebug.PDB.CV_off32_t) (int) value;

            if (type == typeof(DbiHdrVersion))
                value = (DbiHdrVersion) (int) value;

            return value;
        }

        #endregion
        #region Ignore

        protected static readonly object IgnoreValue = new object();

        protected static Action<IView>[] WithIgnores(Action<IView> action, int after) => WithIgnores(0, action, after);

        protected static Action<IView>[] WithIgnores(Action<IView>[] action, int after) =>
            WithIgnores(0, action, after);

        protected static Action<IView>[] WithIgnores(int before, Action<IView> action, int after = 0) =>
            WithIgnores(before, new[] { action }, after);

        protected static Action<IView>[] WithIgnores(int before, Action<IView>[] action, int after = 0)
        {
            var list = new List<Action<IView>>();

            for (var i = 0; i < before; i++)
                list.Add(v => { });

            list.AddRange(action);

            for (var i = 0; i < after; i++)
                list.Add(v => { });

            return list.ToArray();
        }

        protected static Action<IView>[] IgnoreValues(int count)
        {
            var result = new Action<IView>[count];

            for (var i = 0; i < count; i++)
                result[i] = v => { };

            return result;
        }

        #endregion
        #region Generate

        protected string GenerateTest<T>() where T : IValue
        {
            Stream fs = null;

            try
            {
                var rawValue = GetStruct<T, T>(out fs, out var file);

                Debug.Assert(rawValue != null);

                var builder = new StringBuilder();
                builder.Append("TestStruct<").Append(typeof(T).Name).AppendLine(">(");

                var properties = typeof(T).GetProperties().Where(p => p.Name != "Offset" && p.Name != "StructSize" && p.GetIndexParameters().Length == 0).ToArray();

                for (var i = 0; i < properties.Length; i++)
                {
                    var value = GetAssertValue(rawValue, properties[i], false);

                    builder.Append("    v => v.").Append(properties[i].Name).Append(" == ").Append(value);

                    if (i < properties.Length - 1)
                        builder.Append(",");

                    builder.AppendLine();
                }

                builder.AppendLine(");");
                builder.AppendLine();

                string CalculateName(string name)
                {
                    var chunks = new List<string>();

                    var chars = name.ToCharArray();

                    var chunkStart = 0;

                    for (var i = 0; i < chars.Length; i++)
                    {
                        chunkStart = i;

                        //Find the first lowercase letter

                        int j = i + 1;

                        for (; j < chars.Length; j++)
                        {
                            if (char.IsLower(chars[j]))
                                break;
                        }

                        //Read characters until we either reach the end, or reach another uppercase letter

                        for (; j < chars.Length; j++)
                        {
                            if (char.IsUpper(chars[j]))
                                break;
                        }

                        var str = name.Substring(chunkStart, j - chunkStart);

                        chunks.Add(str.ToUpper());

                        i = j - 1;
                    }

                    return string.Join("_", chunks);
                }

                builder.Append("TestView<").Append(typeof(T).Name).AppendLine(">(");

                builder.AppendLine("    v => v.VerifyStruct(");
                builder.AppendLine($"        name: \"{CalculateName(typeof(T).Name)}\", offset: {rawValue.Offset}, size: 0,");

                for (var i = 0; i < properties.Length; i++)
                {
                    var value = GetAssertValue(rawValue, properties[i], true);

                    builder.Append($"        c => c.VerifyField(name: \"{properties[i].Name}\", value: {value})");

                    if (i < properties.Length - 1)
                        builder.Append(",");

                    builder.AppendLine();
                }

                builder.AppendLine("    )");
                builder.Append(");");

                return builder.ToString();
            }
            finally
            {
                fs?.Dispose();
            }
        }

        internal static string GetAssertValue(object rawValue, PropertyInfo propertyInfo, bool cast)
        {
            var value = propertyInfo.GetValue(rawValue);

            if (value == null)
                return "null";

            if (value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(RawValue<>))
                value = value.GetType().GetProperty("Value").GetGetMethod().Invoke(value, null);

            if (value is IRVA r)
                value = r.ListedOffset;

            if (value is IVA v)
                value = v.ListedAddress;

            if (value is bool b)
            {
                if (cast)
                    return "(byte) " + (b ? 1 : 0);

                return b ? "true" : "false";
            }

            if (propertyInfo.PropertyType.IsEnum)
            {
                var items = value.ToString().Split(", ").Select(v => $"{propertyInfo.PropertyType.Name}.{v}").ToArray();

                if (items.Length == 1)
                    return items[0];

                return "(" + string.Join(" | ", items) + ")";
            }

            var typeCode = Type.GetTypeCode(propertyInfo.PropertyType);

            string GetTypeName(TypeCode typeCode)
            {
#pragma warning disable CS8509
                return typeCode switch
#pragma warning restore CS8509
                {
                    TypeCode.SByte => "sbyte",
                    TypeCode.Byte => "byte",
                    TypeCode.Int16 => "short",
                    TypeCode.UInt16 => "ushort",
                    TypeCode.Int32 => "int",
                    TypeCode.UInt32 => "uint",
                    TypeCode.Int64 => "long",
                    TypeCode.UInt64 => "ulong",
                };
            }

            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    if (cast && typeCode != TypeCode.Int32)
                        return "(" + GetTypeName(typeCode) + ") " + value;

                    return value.ToString();
            }

            if (value is string s)
                return $"\"{s}\"";

            if (value.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IString<,>)))
                return $"\"{value}\"";

            if (value is Timestamp)
                return $"\"{value}\"";

            if (value is Guid g)
                return $"new Guid(\"{g}\")";

            if (value is TypOrEnumType t)
                value = (ClrDebug.PDB.CV_typ_t) t;

            if (propertyInfo.PropertyType.IsArray)
            {
                var arr = (Array) value;

                var elementType = propertyInfo.PropertyType.GetElementType();

                var tc = Type.GetTypeCode(elementType);

                if (tc == TypeCode.Object)
                    return "null";

                var typeName = GetTypeName(tc);

                var arrayBuilder = new StringBuilder();
                arrayBuilder.Append("new ").Append(typeName).Append("[]{");

                for (var i = 0; i < arr.Length; i++)
                {
                    var item = arr.GetValue(i);

                    arrayBuilder.Append(item);

                    if (i < arr.Length - 1)
                        arrayBuilder.Append(", ");
                }

                arrayBuilder.Append("}");

                return arrayBuilder.ToString();
            }
        #endregion
    }
}
