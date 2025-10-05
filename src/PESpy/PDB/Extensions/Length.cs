using System;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public static partial class SymTypeExtensions
    {
        /// <summary>
        /// <inheritdoc cref="IDiaSymbol.get_length"/><para/>
        /// Corresponds to <see cref="IDiaSymbol.get_length"/>
        /// </summary>
        public static unsafe bool TryGetLength(in this SymType symType, out int length)
        {
            //I'm only seeing a "len" property on block sym types. I haven't checked what DIA does

            if (symType.IsBlockSym())
            {
                if (symType.rectyp < SYM_ENUM_e.S_TI16_MAX)
                    length = ((BlockSym16) symType).len;
                else
                    length = ((BlockSym32) symType).len;

                return true;
            }
            else
            {
                switch (symType.rectyp)
                {
                    case SYM_ENUM_e.S_PUB16:
                    case SYM_ENUM_e.S_PUB32_16t:
                        throw new NotImplementedException();

                    case SYM_ENUM_e.S_PUB32_ST:
                    case SYM_ENUM_e.S_PUB32:
                        //I believe you can just look at the length of the associated section contrib

                        var pubSym = (PubSym32) symType;

                        //todo: allow specifying an isymbolaccessor to trygetlength
                        if (SymType.TryPDBGetSectionContrib(symType, pubSym.seg, pubSym.off, out var sc))
                        {
                            length = sc.cb;
                            return true;
                        }
                        break;
                }
            }

            length = default;
            return false;
        }
    }
}
