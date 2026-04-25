using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    internal class XRefVerifier
    {
        private string typeName;
        private string scenario;

        internal XRefVerifier(string typeName, string scenario)
        {
            this.typeName = typeName;
            this.scenario = scenario;
        }

        internal void Verify(string propertyName, int index, int fieldOffset, int targetOffset)
        {
            PEFile peFile = null;
            ProcessHolderStream processStream = null;

            PEFile GetSampleFile(string path)
            {
                peFile = PEFile.FromFile(path);

                return peFile;
            }

            PEFile GetStoreFile(SymStoreKey key)
            {
                peFile = PEFile.FromKey(key);

                return peFile;
            }

            PEFile GetAOTFile()
            {
                processStream = ProcessHolderStream.New(Sample.NativeAOT_EXE);

                peFile = PEFile.FromStream(processStream, true);

                return peFile;
            }

            try
            {

                object instance = typeName switch
                {
                    nameof(AppHostSignature) => propertyName switch
                    {
                        nameof(AppHostSignature.BundleHeaderOffset) => GetSampleFile(Sample.SingleFileApp_EXE).AppHostSignature
                    },

                    nameof(NativeAOT.DebugTypeEntry) => propertyName switch
                    {
                        nameof(NativeAOT.DebugTypeEntry.TypeName) => GetAOTFile().DotNetRuntimeDebugHeader.DebugTypeEntries.Value[0],
                        nameof(NativeAOT.DebugTypeEntry.FieldName) => GetAOTFile().DotNetRuntimeDebugHeader.DebugTypeEntries.Value[0]
                    },

                    nameof(NativeAOT.DotNetRuntimeDebugHeader) => propertyName switch
                    {
                        nameof(NativeAOT.DotNetRuntimeDebugHeader.DebugTypeEntries) => GetAOTFile().DotNetRuntimeDebugHeader,
                        nameof(NativeAOT.DotNetRuntimeDebugHeader.GlobalValueEntries) => GetAOTFile().DotNetRuntimeDebugHeader
                    },

                    "ScopeTable.ScopeRecord" => propertyName switch
                    {
                        nameof(ScopeTable.ScopeRecord.BeginAddress)   => ((ScopeTable) GetStoreFile(WellKnownTestModule.ntdll).ExceptionTable[3].UnwindData.Value.ExceptionData)[0],
                        nameof(ScopeTable.ScopeRecord.EndAddress)     => ((ScopeTable) GetStoreFile(WellKnownTestModule.ntdll).ExceptionTable[3].UnwindData.Value.ExceptionData)[0],
                        nameof(ScopeTable.ScopeRecord.HandlerAddress) => ((ScopeTable) GetStoreFile(WellKnownTestModule.ntdll).ExceptionTable[3].UnwindData.Value.ExceptionData)[0],
                        nameof(ScopeTable.ScopeRecord.JumpTarget)     => ((ScopeTable) GetStoreFile(WellKnownTestModule.ntdll).ExceptionTable[3].UnwindData.Value.ExceptionData)[0],
                    },

                    nameof(FuncInfo) => scenario switch
                    {
                        nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1) => propertyName switch
                        {
                            nameof(FuncInfo.dispUnwindMap) => ((RVA<FuncInfo>) GetStoreFile(WellKnownTestModule._7z).ExceptionTable[500].UnwindData.Value.ExceptionData).Value,
                            nameof(FuncInfo.dispTryBlockMap) => ((RVA<FuncInfo>) GetStoreFile(WellKnownTestModule._7z).ExceptionTable[500].UnwindData.Value.ExceptionData).Value,
                            nameof(FuncInfo.dispIPtoStateMap) => ((RVA<FuncInfo>) GetStoreFile(WellKnownTestModule._7z).ExceptionTable[500].UnwindData.Value.ExceptionData).Value
                        },

                        nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3) => propertyName switch
                        {
                            nameof(FuncInfo.dispUnwindMap) => ((RVA<FuncInfo>) GetStoreFile(WellKnownTestModule.AuthExt).ExceptionTable[74].UnwindData.Value.ExceptionData).Value,
                            nameof(FuncInfo.dispTryBlockMap) => ((RVA<FuncInfo>) GetStoreFile(WellKnownTestModule.AuthExt).ExceptionTable[74].UnwindData.Value.ExceptionData).Value,
                            nameof(FuncInfo.dispIPtoStateMap) => ((RVA<FuncInfo>) GetStoreFile(WellKnownTestModule.AuthExt).ExceptionTable[74].UnwindData.Value.ExceptionData).Value,
                        }
                    },

                    nameof(SepIPtoStateMapEntry4) => propertyName switch
                    {
                        nameof(SepIPtoStateMapEntry4.addrStartRVA) => ((RVA<FuncInfo4>) GetStoreFile(WellKnownTestModule.coreclr).ExceptionTable[5].UnwindData.Value.ExceptionData).Value.dispToSegMap.Value[0],
                        nameof(SepIPtoStateMapEntry4.dispOfIPMap) => ((RVA<FuncInfo4>) GetStoreFile(WellKnownTestModule.coreclr).ExceptionTable[5].UnwindData.Value.ExceptionData).Value.dispToSegMap.Value[0]
                    },

                    nameof(NativeAOT.GlobalValueEntry) => propertyName switch
                    {
                        nameof(NativeAOT.GlobalValueEntry.Name) => GetAOTFile().DotNetRuntimeDebugHeader.GlobalValueEntries.Value[0]
                    },

                    nameof(HandlerType) => propertyName switch
                    {
                        nameof(HandlerType.Type) => ((RVA<FuncInfoV1>) GetStoreFile(WellKnownTestModule._7z).ExceptionTable[500].UnwindData.Value.ExceptionData).Value.TryBlockMap.Value[0].HandlerArray.Value[0],
                    },

                    nameof(ImageBoundForwarderRef) => propertyName switch
                    {
                        nameof(ImageBoundForwarderRef.Name) => GetStoreFile(WellKnownTestModule.mfc40u).BoundImportTable[0].Refs[0]
                    },

                    nameof(ImageBoundImportDescriptor) => propertyName switch
                    {
                        nameof(ImageBoundImportDescriptor.Name) => GetStoreFile(WellKnownTestModule.mfc40u).BoundImportTable[0]
                    },

                    nameof(ImageCoffSymbolsHeader) => propertyName switch
                    {
                        nameof(ImageCoffSymbolsHeader.LvaToFirstSymbol) => GetSampleFile(Sample.VC60_Coff_EXE).DebugTable?.First(t => t.Type == IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_COFF).Data
                    },

                    nameof(ImageEnclaveConfig) => propertyName switch
                    {
                        nameof(ImageEnclaveConfig.ImportList) => GetStoreFile(WellKnownTestModule.AzureAttest).LoadConfigTable.EnclaveConfigurationPointer.Value
                    },

                    nameof(ImageDelayLoadDescriptor) => propertyName switch
                    {
                        nameof(ImageDelayLoadDescriptor.DllNameRVA) => GetStoreFile(WellKnownTestModule.AuthExt).DelayImportTable[2],
                        nameof(ImageDelayLoadDescriptor.ModuleHandleRVA) => GetStoreFile(WellKnownTestModule.AuthExt).DelayImportTable[2],
                        nameof(ImageDelayLoadDescriptor.ImportAddressTableRVA) => GetStoreFile(WellKnownTestModule.AuthExt).DelayImportTable[2],
                        nameof(ImageDelayLoadDescriptor.ImportNameTableRVA) => GetStoreFile(WellKnownTestModule.AuthExt).DelayImportTable[2],
                        nameof(ImageDelayLoadDescriptor.UnloadInformationTable) => null //I don't think this is present in on disk modules
                    },

                    nameof(ImageEnclaveImport) => propertyName switch
                    {
                        nameof(ImageEnclaveImport.ImportName) => GetStoreFile(WellKnownTestModule.AzureAttest).LoadConfigTable.EnclaveConfigurationPointer.Value.ImportList.Value[1]
                    },

                    nameof(ImageExportDirectory) => propertyName switch
                    {
                        nameof(ImageExportDirectory.Name) => GetStoreFile(WellKnownTestModule.kernel32).ExportTable,
                        nameof(ImageExportDirectory.AddressOfFunctions) => GetStoreFile(WellKnownTestModule.kernel32).ExportTable,
                        nameof(ImageExportDirectory.AddressOfNames) => GetStoreFile(WellKnownTestModule.kernel32).ExportTable,
                        nameof(ImageExportDirectory.AddressOfNameOrdinals) => GetStoreFile(WellKnownTestModule.kernel32).ExportTable
                    },

                    nameof(ImageFileHeader) => propertyName switch
                    {
                        nameof(ImageFileHeader.PointerToSymbolTable) => GetSampleFile(Sample.VC60_Coff_EXE).FileHeader
                    },

                    nameof(ImageImportDescriptor) => propertyName switch
                    {
                        nameof(ImageImportDescriptor.OriginalFirstThunk) => GetStoreFile(WellKnownTestModule.AzureAttest).ImportTable[0],
                        nameof(ImageImportDescriptor.Name) => GetStoreFile(WellKnownTestModule.AzureAttest).ImportTable[0],
                        nameof(ImageImportDescriptor.FirstThunk) => GetStoreFile(WellKnownTestModule.AzureAttest).ImportTable[0],
                        nameof(ImageImportDescriptor.ImportLookupTable) => GetStoreFile(WellKnownTestModule.AzureAttest).ImportTable[0],
                        nameof(ImageImportDescriptor.ImportAddressTable) => GetStoreFile(WellKnownTestModule.AzureAttest).ImportTable[0]
                    },

                    nameof(ImageLoadConfigDirectory) => propertyName switch
                    {
                        nameof(ImageLoadConfigDirectory.LockPrefixTable)                          => null,
                        nameof(ImageLoadConfigDirectory.SecurityCookie)                           => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.SEHandlerTable)                           => GetStoreFile(WellKnownTestModule.aadauthhelper).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardCFCheckFunctionPointer)              => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardCFDispatchFunctionPointer)           => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardCFFunctionTable)                     => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardAddressTakenIatEntryTable)           => GetSampleFile(Sample.SingleFileApp_EXE).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardLongJumpTargetTable)                 => GetStoreFile(WellKnownTestModule.ntoskrnl).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardRFFailureRoutineFunctionPointer)     => GetStoreFile(WellKnownTestModule.DbgEng).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.DynamicValueRelocTableOffset)             => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardRFVerifyStackPointerFunctionPointer) => GetStoreFile(WellKnownTestModule.DbgEng).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.EnclaveConfigurationPointer)              => GetStoreFile(WellKnownTestModule.AzureAttest).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardEHContinuationTable)                 => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardXFGCheckFunctionPointer)             => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardXFGDispatchFunctionPointer)          => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardXFGTableDispatchFunctionPointer)     => GetStoreFile(WellKnownTestModule.ntdll).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.GuardMemcpyFunctionPointer)               => GetStoreFile(WellKnownTestModule.coreclr).LoadConfigTable,
                        nameof(ImageLoadConfigDirectory.CastGuardOsDeterminedFailureMode)         => throw new System.NotImplementedException(),
                        nameof(ImageLoadConfigDirectory.UmaFunctionPointers)                      => throw new System.NotImplementedException()
                    },

                    nameof(ImageResourceDataEntry) => propertyName switch
                    {
                        nameof(ImageResourceDataEntry.OffsetToData) => throw new System.NotImplementedException()
                    },

                    nameof(ImageResourceDirectoryEntry) => propertyName switch
                    {
                        nameof(ImageResourceDirectoryEntry.OffsetToData) => throw new System.NotImplementedException(),
                        nameof(ImageResourceDirectoryEntry.OffsetToDirectory) => throw new System.NotImplementedException()
                    },

                    nameof(ImageSectionHeader) => propertyName switch
                    {
                        nameof(ImageSectionHeader.PointerToRelocations) => throw new System.NotImplementedException(), //GetStruct doesn't have either of these; find a sample that has line numbers
                        nameof(ImageSectionHeader.PointerToLineNumbers) => throw new System.NotImplementedException()
                    },

                    nameof(RuntimeFunction) => propertyName switch
                    {
                        nameof(RuntimeFunction.UnwindData) => GetStoreFile(WellKnownTestModule.ntdll).ExceptionTable[0]
                    },

                    nameof(TryBlockMapEntry) => propertyName switch
                    {
                        nameof(TryBlockMapEntry.HandlerArray) => ((RVA<FuncInfo>) GetStoreFile(WellKnownTestModule.AuthExt).ExceptionTable[74].UnwindData.Value.ExceptionData).Value.dispTryBlockMap.Value[0],
                    }
                };

                Assert.IsNotNull(instance);

                var viewWriter = new PEXRefViewWriter(peFile);

                ((IViewable) instance).WriteGlobals(viewWriter);

                var xrefs = viewWriter.XRefs;

                var xref = xrefs[index];

                Assert.AreEqual(fieldOffset, xref.FieldOffset, $"Xref fieldOffset was not correct. Also TargetOffset is {xref.TargetValue}");
                Assert.AreEqual(targetOffset, xref.TargetValue);
            }
            finally
            {
                peFile?.Dispose();
                processStream?.Dispose();
            }
        }
    }
}
