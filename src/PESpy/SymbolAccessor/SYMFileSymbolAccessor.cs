using PESpy.View;

namespace PESpy
{
    internal class SYMFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.SYM;

        internal readonly SYMFile SYMFile;

        internal SYMFileSymbolAccessor(SYMFile symFile)
        {
            SYMFile = symFile;
        }

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetLengthFromAddress(int rva, ISectionDataAccessor sectionDataAccessor, out int length)
        {
            //We don't know the bounds of the section, so we need to ask the PEFile

           if (sectionDataAccessor.TryGetOffSeg(rva, out var off, out var seg))
            {
                var section = SYMFile.Segments[seg - 1];

                var symbols = section.Symbols;

                if (symbols.Symbols32 != null)
                {
                    for (var i = 0; i < symbols.Symbols32.Length; i++)
                    {
                        ref var symbol = ref symbols.Symbols32[i];

                        //When we've gone one too far, that means the previous symbol is us
                        if (symbol.sd_lval > off)
                        {
                            if (i == 0)
                            {
                                length = default;
                                return false;
                            }

                            ref var previousSymbol = ref symbols.Symbols32[i - 1];

                            length = symbol.sd_lval - previousSymbol.sd_lval;

                            return true;
                        }
                    }

                    //We can't resolve the last symbol in a section because we don't know how big it is.
                    //DbgHelp seems to have the same issue
                    length = default;
                    return false;
                }
                else
                {
                    for (var i = 0; i < symbols.Symbols16.Length; i++)
                    {
                        ref var symbol = ref symbols.Symbols16[i];

                        //When we've gone one too far, that means the previous symbol is us
                        if (symbol.sd16_val > off)
                        {
                            if (i == 0)
                            {
                                length = default;
                                return false;
                            }

                            ref var previousSymbol = ref symbols.Symbols32[i - 1];

                            length = symbol.sd16_val - previousSymbol.sd_lval;

                            return true;
                        }
                    }

                    //We can't resolve the last symbol in a section because we don't know how big it is.
                    //DbgHelp seems to have the same issue
                    length = default;
                    return false;
                }
            }

            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
