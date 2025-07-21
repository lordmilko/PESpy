namespace PESpy
{
    public static partial class Demangler
    {
        enum SpecialIntrinsicKind
        {
            None,
            Vftable,
            Vbtable,
            Typeof,
            VcallThunk,
            LocalStaticGuard,
            StringLiteralSymbol,
            UdtReturning,
            Unknown,
            DynamicInitializer,
            DynamicAtexitDestructor,
            RttiTypeDescriptor,
            RttiBaseClassDescriptor,
            RttiBaseClassArray,
            RttiClassHierarchyDescriptor,
            RttiCompleteObjLocator,
            LocalVftable,
            LocalStaticThreadGuard,
        }
    }
}
