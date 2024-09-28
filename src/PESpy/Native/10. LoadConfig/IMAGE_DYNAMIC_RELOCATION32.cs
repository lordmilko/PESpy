using System;

namespace PESpy.Native
{
    //ImageDynamicRelocation
    internal struct IMAGE_DYNAMIC_RELOCATION32
    {
        public int Symbol;
        public int BaseRelocSize;

        //Note that the IMAGE_BASE_RELOCATION records that follow may have a custom entry format (that depends on the Symbol version)
        //and that certain Symbol versions may have a structure entirely different from IMAGE_BASE_RELOCATION

        //IMAGE_BASE_RELOCATION BaseRelocations[0];
    }
}