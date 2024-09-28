using System;

namespace PESpy.Ecma335
{
    //This is part of our internal API for parsing ECMA-335 metadata, hence not part of the enums folder

    //Name is made up
    [Flags]
    internal enum TableMask : ulong
    {
        Module = 1UL << TableKind.Module,
        TypeRef = 1UL << TableKind.TypeRef,
        TypeDef = 1UL << TableKind.TypeDef,
        FieldPtr = 1UL << TableKind.FieldPtr,
        Field = 1UL << TableKind.Field,
        MethodPtr = 1UL << TableKind.MethodPtr,
        MethodDef = 1UL << TableKind.MethodDef,
        ParamPtr = 1UL << TableKind.ParamPtr,
        Param = 1UL << TableKind.Param,
        InterfaceImpl = 1UL << TableKind.InterfaceImpl,
        MemberRef = 1UL << TableKind.MemberRef,
        Constant = 1UL << TableKind.Constant,
        CustomAttribute = 1UL << TableKind.CustomAttribute,
        FieldMarshal = 1UL << TableKind.FieldMarshal,
        DeclSecurity = 1UL << TableKind.DeclSecurity,
        ClassLayout = 1UL << TableKind.ClassLayout,
        FieldLayout = 1UL << TableKind.FieldLayout,
        StandAloneSig = 1UL << TableKind.StandAloneSig,
        EventMap = 1UL << TableKind.EventMap,
        EventPtr = 1UL << TableKind.EventPtr,
        Event = 1UL << TableKind.Event,
        PropertyMap = 1UL << TableKind.PropertyMap,
        PropertyPtr = 1UL << TableKind.PropertyPtr,
        Property = 1UL << TableKind.Property,
        MethodSemantics = 1UL << TableKind.MethodSemantics,
        MethodImpl = 1UL << TableKind.MethodImpl,
        ModuleRef = 1UL << TableKind.ModuleRef,
        TypeSpec = 1UL << TableKind.TypeSpec,
        ImplMap = 1UL << TableKind.ImplMap,
        FieldRva = 1UL << TableKind.FieldRva,
        EnCLog = 1UL << TableKind.EncLog,
        EnCMap = 1UL << TableKind.EncMap,
        Assembly = 1UL << TableKind.Assembly,
        AssemblyProcessor = 1UL << TableKind.AssemblyProcessor,
        AssemblyOS = 1UL << TableKind.AssemblyOS,
        AssemblyRef = 1UL << TableKind.AssemblyRef,
        AssemblyRefProcessor = 1UL << TableKind.AssemblyRefProcessor,
        AssemblyRefOS = 1UL << TableKind.AssemblyRefOS,
        File = 1UL << TableKind.File,
        ExportedType = 1UL << TableKind.ExportedType,
        ManifestResource = 1UL << TableKind.ManifestResource,
        NestedClass = 1UL << TableKind.NestedClass,
        GenericParam = 1UL << TableKind.GenericParam,
        MethodSpec = 1UL << TableKind.MethodSpec,
        GenericParamConstraint = 1UL << TableKind.GenericParamConstraint,

        Document = 1UL << TableKind.Document,
        MethodDebugInformation = 1UL << TableKind.MethodDebugInformation,
        LocalScope = 1UL << TableKind.LocalScope,
        LocalVariable = 1UL << TableKind.LocalVariable,
        LocalConstant = 1UL << TableKind.LocalConstant,
        ImportScope = 1UL << TableKind.ImportScope,
        StateMachineMethod = 1UL << TableKind.StateMachineMethod,
        CustomDebugInformation = 1UL << TableKind.CustomDebugInformation,
    }
}
