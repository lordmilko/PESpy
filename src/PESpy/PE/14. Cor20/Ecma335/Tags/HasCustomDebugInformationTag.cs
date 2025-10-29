using System;

namespace PESpy.Ecma335
{
    internal static class HasCustomDebugInformationTag
    {
        //log2(27) = 4.7 = 5
        public const int LogN = 5; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        internal const int MethodDef = 0;
        internal const int Field = 1;
        internal const int TypeRef = 2;
        internal const int TypeDef = 3;
        internal const int Param = 4;
        internal const int InterfaceImpl = 5;
        internal const int MemberRef = 6;
        internal const int Module = 7;
        internal const int DeclSecurity = 8;
        internal const int Property = 9;
        internal const int Event = 10;
        internal const int StandAloneSig = 11;
        internal const int ModuleRef = 12;
        internal const int TypeSpec = 13;
        internal const int Assembly = 14;
        internal const int AssemblyRef = 15;
        internal const int File = 16;
        internal const int ExportedType = 17;
        internal const int ManifestResource = 18;
        internal const int GenericParam = 19;
        internal const int GenericParamConstraint = 20;
        internal const int MethodSpec = 21;

        internal const int Document = 22;
        internal const int LocalScope = 23;
        internal const int LocalVariable = 24;
        internal const int LocalConstant = 25;
        internal const int Import = 26;

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
            TableKind.MethodSpec,

            TableKind.Document,
            TableKind.LocalScope,
            TableKind.LocalVariable,
            TableKind.LocalConstant,
            TableKind.ImportScope,
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
            TableMask.MethodSpec |
            TableMask.Document |
            TableMask.LocalScope |
            TableMask.LocalVariable |
            TableMask.LocalConstant |
            TableMask.ImportScope;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.MethodDef => MethodDef,
                TableKind.Field => Field,
                TableKind.TypeRef => TypeRef,
                TableKind.TypeDef => TypeDef,
                TableKind.Param => Param,
                TableKind.InterfaceImpl => InterfaceImpl,
                TableKind.MemberRef => MemberRef,
                TableKind.Module => Module,
                TableKind.DeclSecurity => DeclSecurity,
                TableKind.Property => Property,
                TableKind.Event => Event,
                TableKind.StandAloneSig => StandAloneSig,
                TableKind.ModuleRef => ModuleRef,
                TableKind.TypeSpec => TypeSpec,
                TableKind.Assembly => Assembly,
                TableKind.AssemblyRef => AssemblyRef,
                TableKind.File => File,
                TableKind.ExportedType => ExportedType,
                TableKind.ManifestResource => ManifestResource,
                TableKind.GenericParam => GenericParam,
                TableKind.GenericParamConstraint => GenericParamConstraint,
                TableKind.MethodSpec => MethodSpec,

                TableKind.Document => Document,
                TableKind.LocalScope => LocalScope,
                TableKind.LocalVariable => LocalVariable,
                TableKind.LocalConstant => LocalConstant,
                TableKind.ImportScope => Import,
            });

            return new CodedIndex(value, CodedIndexType.HasCustomDebugInformation);
        }

        internal static TableKind GetTableKind(int value) => TagToTokenTypeArray[value & TagMask];
    }
}
