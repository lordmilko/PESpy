namespace PESpy.Ecma335
{
    internal readonly struct CodedIndexTag
    {
        public readonly int LogN;
        public readonly TableMask CandidateTables;
        public readonly int LargeRowThreshold;

        //log2(3) = 1.58 = 2
        public static readonly CodedIndexTag TypeDefOrRef = new CodedIndexTag(logN: 2, candidateTables: TableMask.TypeDef | TableMask.TypeRef | TableMask.TypeSpec);

        //log2(3) = 1.58 = 2
        public static readonly CodedIndexTag HasConstant = new CodedIndexTag(logN: 2, candidateTables: TableMask.Field | TableMask.Param | TableMask.Property);

        //log2(22) = 4.46 = 5
        public static readonly CodedIndexTag HasCustomAttribute = new CodedIndexTag(logN: 5, candidateTables:
            TableMask.MethodDef | TableMask.Field | TableMask.TypeRef | TableMask.TypeDef |
            TableMask.Param | TableMask.InterfaceImpl | TableMask.MemberRef | TableMask.Module |
            TableMask.DeclSecurity | TableMask.Property | TableMask.Event | TableMask.StandAloneSig |
            TableMask.ModuleRef | TableMask.TypeSpec | TableMask.Assembly | TableMask.AssemblyRef |
            TableMask.File | TableMask.ExportedType | TableMask.ManifestResource |
            TableMask.GenericParam | TableMask.GenericParamConstraint | TableMask.MethodSpec
        );

        //log2(1) = 1
        public static readonly CodedIndexTag HasFieldMarshal = new CodedIndexTag(logN: 1, candidateTables: TableMask.Field | TableMask.Param);

        //log2(3) = 1.58 = 2
        public static readonly CodedIndexTag HasDeclSecurity = new CodedIndexTag(logN: 2, candidateTables: TableMask.TypeDef | TableMask.MethodDef | TableMask.Assembly);

        //log2(5) = 2.3 = 3
        public static readonly CodedIndexTag MemberRefParent = new CodedIndexTag(logN: 3, candidateTables: TableMask.TypeDef | TableMask.TypeRef | TableMask.ModuleRef | TableMask.MethodDef | TableMask.TypeSpec);

        //log2(2) = 1
        public static readonly CodedIndexTag HasSemantics = new CodedIndexTag(logN: 1, candidateTables: TableMask.Event | TableMask.Property);

        //log2(2) = 1
        public static readonly CodedIndexTag MethodDefOrRef = new CodedIndexTag(logN: 1, candidateTables: TableMask.MethodDef | TableMask.MemberRef);

        //log2(2) = 1
        public static readonly CodedIndexTag MemberForwarded = new CodedIndexTag(logN: 1, candidateTables: TableMask.Field | TableMask.MethodDef);

        //log2(3) = 1.58 = 2
        public static readonly CodedIndexTag Implementation = new CodedIndexTag(logN: 2, candidateTables: TableMask.File | TableMask.AssemblyRef | TableMask.ExportedType);

        //log2(5) = 2.3 = 3. Only 2 of the 5 tags are used
        public static readonly CodedIndexTag CustomAttributeType = new CodedIndexTag(logN: 3, candidateTables: TableMask.MethodDef | TableMask.MemberRef);

        //log2(4) = 2
        public static readonly CodedIndexTag ResolutionScope = new CodedIndexTag(logN: 2, candidateTables: TableMask.Module | TableMask.ModuleRef | TableMask.AssemblyRef | TableMask.TypeRef);

        //log2(2) = 1
        public static readonly CodedIndexTag TypeOrMethodDef = new CodedIndexTag(logN: 1, candidateTables: TableMask.TypeDef | TableMask.MethodDef);

        //log2(27) = 4.7 = 5
        public static readonly CodedIndexTag HasCustomDebugInformation = new CodedIndexTag(logN: 5, candidateTables:
            TableMask.MethodDef | TableMask.Field | TableMask.TypeRef | TableMask.TypeDef | TableMask.Param | TableMask.InterfaceImpl |
            TableMask.MemberRef | TableMask.Module | TableMask.DeclSecurity | TableMask.Property | TableMask.Event | TableMask.StandAloneSig |
            TableMask.ModuleRef | TableMask.TypeSpec | TableMask.Assembly | TableMask.AssemblyRef | TableMask.File | TableMask.ExportedType |
            TableMask.ManifestResource | TableMask.GenericParam | TableMask.GenericParamConstraint | TableMask.MethodSpec | TableMask.Document |
            TableMask.LocalScope | TableMask.LocalVariable | TableMask.LocalConstant | TableMask.ImportScope);

        private CodedIndexTag(int logN, TableMask candidateTables)
        {
            LogN = logN;
            CandidateTables = candidateTables;
            LargeRowThreshold = 1 << (16 - logN);
        }
    }
}
