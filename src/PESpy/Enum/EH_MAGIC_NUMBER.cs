namespace PESpy
{
    public enum EH_MAGIC_NUMBER
    {
        //The original
        EH_MAGIC_NUMBER1 = 0x19930520,

        //Indicates that FuncInfo::psESTypeList is valid
        EH_MAGIC_NUMBER2 = 0x19930521,

        //Indicates that FuncInfo::EHFlags is valid
        EH_MAGIC_NUMBER3 = 0x19930522,

        //Note that FuncInfo4 does not include a magic number, as it was deemed
        //to take up too much space https://devblogs.microsoft.com/cppblog/making-cpp-exception-handling-smaller-x64/
    }
}
