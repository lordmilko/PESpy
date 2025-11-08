using ClrDebug;

namespace PESpy
{
    public enum SectionFormat
    {
        // Important: update ibcmerge.cs if you change these
        ScenarioInfo = 0,
        MethodBlockCounts = 1, // Basic-block counts. Cold blocks will be placed in the cold-code section
        BlobStream = 2, // metadata access, inst-type-spec and inst-method-spec blobs

        FirstTokenFlagSection = 3,

        ModuleProfilingData                 = FirstTokenFlagSection + (CorTokenType.mdtModule >> 24),
        TypeRefProfilingData                = FirstTokenFlagSection + (CorTokenType.mdtTypeRef >> 24),
        TypeProfilingData                   = FirstTokenFlagSection + (CorTokenType.mdtTypeDef >> 24),
        FieldDefProfilingData               = FirstTokenFlagSection + (CorTokenType.mdtFieldDef >> 24),
        MethodProfilingData                 = FirstTokenFlagSection + (CorTokenType.mdtMethodDef >> 24),
        ParamDefProfilingData               = FirstTokenFlagSection + (CorTokenType.mdtParamDef >> 24),
        InterfaceImplProfilingData          = FirstTokenFlagSection + (CorTokenType.mdtInterfaceImpl >> 24),
        MemberRefProfilingData              = FirstTokenFlagSection + (CorTokenType.mdtMemberRef >> 24),
        CustomAttributeProfilingData        = FirstTokenFlagSection + (CorTokenType.mdtCustomAttribute >> 24),
        PermissionProfilingData             = FirstTokenFlagSection + (CorTokenType.mdtPermission >> 24),
        SignatureProfilingData              = FirstTokenFlagSection + (CorTokenType.mdtSignature >> 24),
        EventProfilingData                  = FirstTokenFlagSection + (CorTokenType.mdtEvent >> 24),
        PropertyProfilingData               = FirstTokenFlagSection + (CorTokenType.mdtProperty >> 24),
        ModuleRefProfilingData              = FirstTokenFlagSection + (CorTokenType.mdtModuleRef >> 24),
        TypeSpecProfilingData               = FirstTokenFlagSection + (CorTokenType.mdtTypeSpec >> 24),
        AssemblyProfilingData               = FirstTokenFlagSection + (CorTokenType.mdtAssembly >> 24),
        AssemblyRefProfilingData            = FirstTokenFlagSection + (CorTokenType.mdtAssemblyRef >> 24),
        FileProfilingData                   = FirstTokenFlagSection + (CorTokenType.mdtFile >> 24),
        ExportedTypeProfilingData           = FirstTokenFlagSection + (CorTokenType.mdtExportedType >> 24),
        ManifestResourceProfilingData       = FirstTokenFlagSection + (CorTokenType.mdtManifestResource >> 24),
        GenericParamProfilingData           = FirstTokenFlagSection + (CorTokenType.mdtGenericParam >> 24),
        MethodSpecProfilingData             = FirstTokenFlagSection + (CorTokenType.mdtMethodSpec >> 24),
        GenericParamConstraintProfilingData = FirstTokenFlagSection + (CorTokenType.mdtGenericParamConstraint >> 24),

        StringPoolProfilingData,
        GuidPoolProfilingData,
        BlobPoolProfilingData,
        UserStringPoolProfilingData,

        FirstMetadataPoolSection = StringPoolProfilingData,
        LastMetadataPoolSection = UserStringPoolProfilingData,
        LastTokenFlagSection = LastMetadataPoolSection,

        IbcTypeSpecSection,
        IbcMethodSpecSection,

        GenericTypeProfilingData = 63, // Deprecated with V2 IBC data
        SectionFormatCount = 64, // 0x40

        SectionFormatInvalid = -1
    }
}
