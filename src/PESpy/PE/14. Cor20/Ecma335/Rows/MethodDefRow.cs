using System;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct MethodDefRow : IValue, IViewable
    {
        public MethodDefIndex RowIndex { get; }

        public int RVA => table.GetRVA(RowIndex);

        public CorMethodImpl ImplFlags => table.GetImplFlags(RowIndex);

        public CorMethodAttr Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public ParamIndex ParamList => table.GetParamList(RowIndex);

        public ParamList Parameters => new ParamList(RowIndex, table.ModelHeap);

        public GenericParamList GenericParameters => table.ModelHeap.GenericParamTable.FindGenericParameters(TypeOrMethodDefTag.CreateIndex(RowIndex.RowId, TableKind.MethodDef));

        //System.Reflection.Metadata's MethodImport type basically just contains
        //all of the properties of the ImplMapRow type minus the MemberForwarded member
        //(which would have been used to match against the row to begin with)
        public ImplMapRow? Import
        {
            get
            {
                var implMapTable = table.ModelHeap.ImplMapTable;

                if (implMapTable == null)
                    return default;

                var implIndex = implMapTable.FindImplForMethod(RowIndex);

                if (implIndex.RowId == 0)
                    return default;

                return table.ModelHeap.ImplMapTable[implIndex];
            }
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        public DeclSecurityAttributeList DeclSecurityAttributes => table.GetDeclSecurityAttributes(RowIndex);

        public ImageCorILMethod? ILMethod
        {
            get
            {
                var peFile = table.ModelHeap.File() as PEFile;

                if (peFile != null && peFile.TryGetILMethod(RowIndex, out var ilMethod))
                    return ilMethod;

                return null;
            }
        }

        public long Offset => table.GetRowOffset(RowIndex);

        private readonly MethodDefTable table;

        internal MethodDefRow(MethodDefIndex index, MethodDefTable table)
        {
            //II.22.26

            RowIndex = index;
            this.table = table;
        }

        public MethodSignature<TType> DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.ModelHeap);
            var reader = Signature.GetReader();
            return decoder.DecodeMethodSignature(ref reader);
        }

        public TypeDefRow? DeclaringType => table.ModelHeap.GetDeclaringType(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_MethodDefRow, table.RowSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(RVA), table.RVAOffset, RVA);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ImplFlags), table.ImplFlagsOffset, ImplFlags, sizeof(short));
                    break;

                case 2:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 3:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 4:
                    structWriter.WriteBlobHeapIndex(nameof(Signature), table.SignatureOffset, Signature);
                    break;

                case 5:
                    structWriter.WriteSimpleIndex(nameof(ParamList), table.ParamListOffset, (int) ParamList, TableKind.Param);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            var builder = new Utf8StringBuilder();

            try
            {
                ToString(ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public void ToString(ref Utf8StringBuilder builder)
        {
            var declaringType = DeclaringType;

            if (declaringType != null)
            {
                var ns = declaringType.Value.TypeNamespace.GetString();

                if (ns.Length > 0)
                {
                    builder.Append(declaringType.Value.TypeNamespace.GetString());
                    builder.Append('.');
                }

                builder.Append(declaringType.Value.TypeName.GetString());
                builder.Append('.');
            }

            builder.Append(Name.GetString());
        }
    }
}
