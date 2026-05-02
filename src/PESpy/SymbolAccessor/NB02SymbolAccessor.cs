using PESpy.View;

namespace PESpy
{
    internal class NB02SymbolAccessor : ISymbolAccessor
    {
        private NB02Data data;

        internal NB02SymbolAccessor(NB02Data data)
        {
            this.data = data;
        }

        #region ISymbolAccessor

        public SymbolAccessorKind Kind => SymbolAccessorKind.CodeView;

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetLengthFromAddress(int targetAddress, ISectionDataAccessor sectionDataAccessor, out int length)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        public void Dispose()
        {
            //These symbols are part of the parent file; we have nothing to do
        }
    }
}
