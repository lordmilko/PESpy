using static ClrDebug.PDB.LEAF_ENUM_e;

namespace PESpy.PDB
{
    public static partial class TypTypeExtensions
    {
        public static bool TryGetCount(in this LfEasy lfEasy, out int count)
        {
            int elementSize;

            switch (lfEasy.leaf)
            {
                case LF_ARRAY:
                case LF_ARRAY_ST: //Not supported by DIA
                    var lfArray = (LfArray) lfEasy;

                    //I thought maybe we could say if elementSize is 0 get the fieldlist, but
                    //that was null as well!
                    if (lfArray.elemtype.TryGetLength(out elementSize))
                    {
                        count = elementSize == 0 ? 0 : lfArray.length / elementSize;
                        return true;
                    }

                    count = default;
                    return false;

                case LF_ARRAY_16t: //Not supported by DIA
                    var lfArray16t = (LfArray16t) lfEasy;

                    if (lfArray16t.elemtype.TryGetLength(out elementSize))
                    {
                        count = elementSize == 0 ? 0 : lfArray16t.length / elementSize;
                        return true;
                    }

                    count = default;
                    return false;

                default:
                    count = default;
                    return false;
            }
        }
    }
}
