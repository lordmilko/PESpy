using System;

namespace PESpy.Ecma335
{

    internal static class HasCustomAttributeTag
    {
        //log2(22) = 4.46 = 5
        public const int LogN = 5; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int MethodDef = 0;
        private const int Field = 1;
        private const int TypeRef = 2;
        private const int TypeDef = 3;
        private const int Param = 4;
        private const int InterfaceImpl = 5;
        private const int MemberRef = 6;
        private const int Module = 7;
        private const int DeclSecurity = 8;
        private const int Property = 9;
        private const int Event = 10;
        private const int StandAloneSig = 11;
        private const int ModuleRef = 12;
        private const int TypeSpec = 13;
        private const int Assembly = 14;
        private const int AssemblyRef = 15;
        private const int File = 16;
        private const int ExportedType = 17;
        private const int ManifestResource = 18;
        private const int GenericParam = 19;
        private const int GenericParamConstraint = 20;
        private const int MethodSpec = 21;

        private const int TagMask = (1 << LogN) - 1;

        internal static ReadOnlySpan<TableKind> TagToTokenTypeArray =>
        [
            TableKind.MethodDef,
            TableKind.Field,
            TableKind.TypeRef,
            TableKind.TypeDef,
            TableKind.Param,
            TableKind.InterfaceImpl,
            TableKind.MemberRef,
            TableKind.Module,
            TableKind.DeclSecurity,
            TableKind.Property,
            TableKind.Event,
            TableKind.StandAloneSig,
            TableKind.ModuleRef,
            TableKind.TypeSpec,
            TableKind.Assembly,
            TableKind.AssemblyRef,
            TableKind.File,
            TableKind.ExportedType,
            TableKind.ManifestResource,
            TableKind.GenericParam,
            TableKind.GenericParamConstraint,
            TableKind.MethodSpec
        ];

        public const TableMask CandidateTables =
            TableMask.MethodDef |
            TableMask.Field |
            TableMask.TypeRef |
            TableMask.TypeDef |
            TableMask.Param |
            TableMask.InterfaceImpl |
            TableMask.MemberRef |
            TableMask.Module |
            TableMask.DeclSecurity |
            TableMask.Property |
            TableMask.Event |
            TableMask.StandAloneSig |
            TableMask.ModuleRef |
            TableMask.TypeSpec |
            TableMask.Assembly |
            TableMask.AssemblyRef |
            TableMask.File |
            TableMask.ExportedType |
            TableMask.ManifestResource |
            TableMask.GenericParam |
            TableMask.GenericParamConstraint |
            TableMask.MethodSpec;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.MethodDef              => MethodDef,
                TableKind.Field                  => Field,
                TableKind.TypeRef                => TypeRef,
                TableKind.TypeDef                => TypeDef,
                TableKind.Param                  => Param,
                TableKind.InterfaceImpl          => InterfaceImpl,
                TableKind.MemberRef              => MemberRef,
                TableKind.Module                 => Module,
                TableKind.DeclSecurity           => DeclSecurity,
                TableKind.Property               => Property,
                TableKind.Event                  => Event,
                TableKind.StandAloneSig          => StandAloneSig,
                TableKind.ModuleRef              => ModuleRef,
                TableKind.TypeSpec               => TypeSpec,
                TableKind.Assembly               => Assembly,
                TableKind.AssemblyRef            => AssemblyRef,
                TableKind.File                   => File,
                TableKind.ExportedType           => ExportedType,
                TableKind.ManifestResource       => ManifestResource,
                TableKind.GenericParam           => GenericParam,
                TableKind.GenericParamConstraint => GenericParamConstraint,
                TableKind.MethodSpec             => MethodSpec
            });

            return new CodedIndex(value, CodedIndexType.HasCustomAttribute);
        }

        internal static TableKind GetTableKind(int value) => TagToTokenTypeArray[value & TagMask];
    }
}
