using System;
using System.Collections.Generic;
using System.Text;
using ClrDebug;
using static ClrDebug.Extensions;

namespace PESpy.Ecma335
{
    public partial struct ModuleIndex
    {
        public static implicit operator mdToken(ModuleIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtModule);

        public static implicit operator mdModule(ModuleIndex value) => (mdModule) TokenFromRid(value.RowId, CorTokenType.mdtModule);
    }

    public partial struct TypeRefIndex
    {
        public static implicit operator mdToken(TypeRefIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtTypeRef);

        public static implicit operator mdTypeRef(TypeRefIndex value) => (mdTypeRef) TokenFromRid(value.RowId, CorTokenType.mdtTypeRef);
    }

    public partial struct TypeDefIndex
    {
        public static implicit operator mdToken(TypeDefIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtTypeDef);

        public static implicit operator mdTypeDef(TypeDefIndex value) => (mdTypeDef) TokenFromRid(value.RowId, CorTokenType.mdtTypeDef);
    }

    public partial struct FieldIndex
    {
        public static implicit operator mdToken(FieldIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtFieldDef);

        public static implicit operator mdFieldDef(FieldIndex value) => (mdFieldDef) TokenFromRid(value.RowId, CorTokenType.mdtFieldDef);
    }

    public partial struct MethodDefIndex
    {
        public static implicit operator mdToken(MethodDefIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtMethodDef);

        public static implicit operator mdMethodDef(MethodDefIndex value) => (mdMethodDef) TokenFromRid(value.RowId, CorTokenType.mdtMethodDef);
    }

    public partial struct ParamIndex
    {
        public static implicit operator mdToken(ParamIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtParamDef);

        public static implicit operator mdParamDef(ParamIndex value) => (mdParamDef) TokenFromRid(value.RowId, CorTokenType.mdtParamDef);
    }

    public partial struct InterfaceImplIndex
    {
        public static implicit operator mdToken(InterfaceImplIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtInterfaceImpl);

        public static implicit operator mdInterfaceImpl(InterfaceImplIndex value) => (mdInterfaceImpl) TokenFromRid(value.RowId, CorTokenType.mdtInterfaceImpl);
    }

    public partial struct MemberRefIndex
    {
        public static implicit operator mdToken(MemberRefIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtMemberRef);

        public static implicit operator mdMemberRef(MemberRefIndex value) => (mdMemberRef) TokenFromRid(value.RowId, CorTokenType.mdtMemberRef);
    }

    public partial struct CustomAttributeIndex
    {
        public static implicit operator mdToken(CustomAttributeIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtCustomAttribute);

        public static implicit operator mdCustomAttribute(CustomAttributeIndex value) => (mdCustomAttribute) TokenFromRid(value.RowId, CorTokenType.mdtCustomAttribute);
    }

    public partial struct DeclSecurityIndex
    {
        public static implicit operator mdToken(DeclSecurityIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtPermission);

        public static implicit operator mdPermission(DeclSecurityIndex value) => (mdPermission) TokenFromRid(value.RowId, CorTokenType.mdtPermission);
    }

    public partial struct StandAloneSigIndex
    {
        public static implicit operator mdToken(StandAloneSigIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtSignature);

        public static implicit operator mdSignature(StandAloneSigIndex value) => (mdSignature) TokenFromRid(value.RowId, CorTokenType.mdtSignature);
    }

    public partial struct EventIndex
    {
        public static implicit operator mdToken(EventIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtEvent);

        public static implicit operator mdEvent(EventIndex value) => (mdEvent) TokenFromRid(value.RowId, CorTokenType.mdtEvent);
    }

    public partial struct PropertyIndex
    {
        public static implicit operator mdToken(PropertyIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtProperty);

        public static implicit operator mdProperty(PropertyIndex value) => (mdProperty) TokenFromRid(value.RowId, CorTokenType.mdtProperty);
    }

    //mdtMethodImpl does not have a specific token typedef
    public partial struct MethodImplIndex
    {
        public static implicit operator mdToken(MethodImplIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtMethodImpl);
    }

    public partial struct ModuleRefIndex
    {
        public static implicit operator mdToken(ModuleRefIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtModuleRef);

        public static implicit operator mdModuleRef(ModuleRefIndex value) => (mdModuleRef) TokenFromRid(value.RowId, CorTokenType.mdtModuleRef);
    }

    public partial struct TypeSpecIndex
    {
        public static implicit operator mdToken(TypeSpecIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtTypeSpec);

        public static implicit operator mdTypeSpec(TypeSpecIndex value) => (mdTypeSpec) TokenFromRid(value.RowId, CorTokenType.mdtTypeSpec);
    }

    public partial struct AssemblyIndex
    {
        public static implicit operator mdToken(AssemblyIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtAssembly);

        public static implicit operator mdAssembly(AssemblyIndex value) => (mdAssembly) TokenFromRid(value.RowId, CorTokenType.mdtAssembly);
    }

    public partial struct AssemblyRefIndex
    {
        public static implicit operator mdToken(AssemblyRefIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtAssemblyRef);

        public static implicit operator mdAssemblyRef(AssemblyRefIndex value) => (mdAssemblyRef) TokenFromRid(value.RowId, CorTokenType.mdtAssemblyRef);
    }

    public partial struct FileIndex
    {
        public static implicit operator mdToken(FileIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtFile);

        public static implicit operator mdFile(FileIndex value) => (mdFile) TokenFromRid(value.RowId, CorTokenType.mdtFile);
    }

    public partial struct ExportedTypeIndex
    {
        public static implicit operator mdToken(ExportedTypeIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtExportedType);

        public static implicit operator mdExportedType(ExportedTypeIndex value) => (mdExportedType) TokenFromRid(value.RowId, CorTokenType.mdtExportedType);
    }

    public partial struct ManifestResourceIndex
    {
        public static implicit operator mdToken(ManifestResourceIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtManifestResource);

        public static implicit operator mdManifestResource(ManifestResourceIndex value) => (mdManifestResource) TokenFromRid(value.RowId, CorTokenType.mdtManifestResource);
    }

    public partial struct GenericParamIndex
    {
        public static implicit operator mdToken(GenericParamIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtGenericParam);

        public static implicit operator mdGenericParam(GenericParamIndex value) => (mdGenericParam) TokenFromRid(value.RowId, CorTokenType.mdtGenericParam);
    }

    public partial struct MethodSpecIndex
    {
        public static implicit operator mdToken(MethodSpecIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtMethodSpec);

        public static implicit operator mdMethodSpec(MethodSpecIndex value) => (mdMethodSpec) TokenFromRid(value.RowId, CorTokenType.mdtMethodSpec);
    }

    public partial struct GenericParamConstraintIndex
    {
        public static implicit operator mdToken(GenericParamConstraintIndex value) => TokenFromRid(value.RowId, CorTokenType.mdtGenericParamConstraint);

        public static implicit operator mdGenericParamConstraint(GenericParamConstraintIndex value) => (mdGenericParamConstraint) TokenFromRid(value.RowId, CorTokenType.mdtGenericParamConstraint);
    }

    /*
                     CorTokenType.mdtModule                 => ModuleTable.FromToken(token),
                CorTokenType.mdtTypeRef                => TypeRefTable.FromToken(token),
                CorTokenType.mdtTypeDef                => TypeDefTable.FromToken(token),
                CorTokenType.mdtFieldDef               => FieldTable.FromToken(token),
                CorTokenType.mdtMethodDef              => MethodDefTable.FromToken(token),
                CorTokenType.mdtParamDef               => ParamTable.FromToken(token),
                CorTokenType.mdtInterfaceImpl          => InterfaceImplTable.FromToken(token),
                CorTokenType.mdtMemberRef              => MemberRefTable.FromToken(token),
                CorTokenType.mdtCustomAttribute        => CustomAttributeTable.FromToken(token),
                CorTokenType.mdtPermission             => DeclSecurityTable.FromToken(token),
                CorTokenType.mdtSignature              => StandAloneSigTable.FromToken(token),
                CorTokenType.mdtEvent                  => EventTable.FromToken(token),
                CorTokenType.mdtProperty               => PropertyTable.FromToken(token),
                CorTokenType.mdtMethodImpl             => MethodImplTable.FromToken(token),
                CorTokenType.mdtModuleRef              => ModuleRefTable.FromToken(token),
                CorTokenType.mdtTypeSpec               => TypeSpecTable.FromToken(token),
                CorTokenType.mdtAssembly               => AssemblyTable.FromToken(token),
                CorTokenType.mdtAssemblyRef            => AssemblyRefTable.FromToken(token),
                CorTokenType.mdtFile                   => FileTable.FromToken(token),
                CorTokenType.mdtExportedType           => ExportedTypeTable.FromToken(token),
                CorTokenType.mdtManifestResource       => ManifestResourceTable.FromToken(token),
                CorTokenType.mdtGenericParam           => GenericParamTable.FromToken(token),
                CorTokenType.mdtMethodSpec             => MethodSpecTable.FromToken(token),
                CorTokenType.mdtGenericParamConstraint => GenericParamConstraintTable.FromToken(token),
                CorTokenType.mdtString                 => throw new NotImplementedException(),
                CorTokenType.mdtName                   => throw new NotImplementedException(),
                CorTokenType.mdtBaseType               => throw new NotImplementedException(),
     */
}
